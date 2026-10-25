import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';

const chromePath = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const profileDir = path.resolve('tmp_cdp_profile');

if (!fs.existsSync(profileDir)) {
  fs.mkdirSync(profileDir, { recursive: true });
}

console.log('Launching Chrome with CDP on port 9222...');
const chrome = spawn(chromePath, [
  '--remote-debugging-port=9222',
  '--remote-allow-origins=*',
  `--user-data-dir=${profileDir}`,
  '--headless=new',
  '--disable-gpu=false',
  '--use-gl=angle',
  '--enable-webgl',
  '--ignore-gpu-blocklist',
  '--window-size=1920,1080',
  'about:blank'
]);

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function main() {
  await sleep(2000);

  console.log('Connecting to browser on port 9222...');
  let targetRes;
  for (let i = 0; i < 10; i++) {
    try {
      const vRes = await fetch('http://127.0.0.1:9222/json/version');
      if (vRes.ok) {
        console.log('Chrome version API connected!');
        const res = await fetch('http://127.0.0.1:9222/json/new?http://localhost:8080', { method: 'PUT' });
        targetRes = await res.json();
        break;
      }
    } catch (e) {
      console.log('Waiting for Chrome...', e.message);
      await sleep(1000);
    }
  }

  if (!targetRes || !targetRes.webSocketDebuggerUrl) {
    console.error('Failed to get WebSocket debugger URL:', targetRes);
    chrome.kill();
    process.exit(1);
  }

  console.log('Connecting to WebSocket:', targetRes.webSocketDebuggerUrl);
  const ws = new WebSocket(targetRes.webSocketDebuggerUrl);

  let msgId = 1;
  const pending = new Map();

  ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    if (data.method === 'Runtime.consoleAPICalled') {
      const msg = (data.params.args || []).map(a => a.value || a.description || JSON.stringify(a)).join(' ');
      console.log('[BROWSER CONSOLE]', msg);
    }
    if (data.id && pending.has(data.id)) {
      pending.get(data.id)(data);
      pending.delete(data.id);
    }
  };

  function sendCommand(method, params = {}) {
    return new Promise((resolve) => {
      const id = msgId++;
      pending.set(id, resolve);
      ws.send(JSON.stringify({ id, method, params }));
    });
  }

  async function clickAt(x, y, label) {
    console.log(`Clicking ${label} at (${x}, ${y})...`);
    await sendCommand('Input.dispatchMouseEvent', {
      type: 'mousePressed',
      x,
      y,
      button: 'left',
      clickCount: 1
    });
    await sleep(120);
    await sendCommand('Input.dispatchMouseEvent', {
      type: 'mouseReleased',
      x,
      y,
      button: 'left',
      clickCount: 1
    });
  }

  async function takeScreenshot(filename) {
    console.log(`Capturing ${filename}...`);
    const ss = await sendCommand('Page.captureScreenshot', { format: 'png' });
    if (ss && ss.result && ss.result.data) {
      fs.writeFileSync(filename, Buffer.from(ss.result.data, 'base64'));
      console.log(`Saved ${filename}`);
    }
  }

  await new Promise(resolve => ws.onopen = resolve);
  console.log('CDP WebSocket connected!');

  await sendCommand('Page.enable');
  await sendCommand('Runtime.enable');

  console.log('Waiting for Unity WebGL to initialize (12 seconds)...');
  await sleep(12000);

  // 1. Initial screenshot
  await takeScreenshot('01_initial_selection.png');

  // 2. Select Room 102 (X = 260, Y = 858)
  await clickAt(260, 858, 'Room 102 Button');
  await sleep(2500);
  await takeScreenshot('02_navigating_panel.png');

  // 3. Step Forward button (X = 150, Y = 825)
  await clickAt(150, 825, 'Step Forward Button (Step 1 -> 2)');
  await sleep(1500);
  await takeScreenshot('03_step_forward_1.png');

  // 4. Step Forward button again (Step 2 -> 3)
  await clickAt(150, 825, 'Step Forward Button (Step 2 -> 3)');
  await sleep(1500);
  await takeScreenshot('04_step_forward_2.png');

  // 5. Reset Route button (X = 150, Y = 900)
  await clickAt(150, 900, 'Reset Route Button');
  await sleep(1500);
  await takeScreenshot('05_reset_route.png');

  // 6. Auto Simulate button (X = 397, Y = 825) to auto-walk to destination
  await clickAt(397, 825, 'Auto Simulate Button');
  console.log('Waiting for auto simulation across 9 nodes to reach destination (18 seconds)...');
  await sleep(18000);
  await takeScreenshot('06_destination_reached.png');

  // 7. Click "Select Another Destination" on DestinationReachedPanel at (X = 280, Y = 745)
  await clickAt(280, 745, 'Select Another Destination Button');
  await sleep(2500);
  await takeScreenshot('07_back_to_selection.png');

  ws.close();
  chrome.kill();
  console.log('All WebGL browser tests completed successfully!');
}

main().catch(err => {
  console.error(err);
  chrome.kill();
  process.exit(1);
});
