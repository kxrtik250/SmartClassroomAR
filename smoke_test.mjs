import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';

const chromePath = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const profileDir = path.resolve('tmp_cdp_smoke');

if (!fs.existsSync(profileDir)) {
  fs.mkdirSync(profileDir, { recursive: true });
}

console.log('Launching headless Chrome for Visual Smoke Test...');
const chrome = spawn(chromePath, [
  '--remote-debugging-port=9223',
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

  let targetRes;
  for (let i = 0; i < 10; i++) {
    try {
      const vRes = await fetch('http://127.0.0.1:9223/json/version');
      if (vRes.ok) {
        const res = await fetch('http://127.0.0.1:9223/json/new?http://localhost:8080/index.html', { method: 'PUT' });
        targetRes = await res.json();
        break;
      }
    } catch (e) {
      await sleep(1000);
    }
  }

  if (!targetRes || !targetRes.webSocketDebuggerUrl) {
    console.error('Failed to connect to Chrome debugger:', targetRes);
    chrome.kill();
    process.exit(1);
  }

  const ws = new WebSocket(targetRes.webSocketDebuggerUrl);
  let msgId = 1;
  const pending = new Map();

  ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
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

  async function clickAt(x, y) {
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
    const ss = await sendCommand('Page.captureScreenshot', { format: 'png' });
    if (ss && ss.result && ss.result.data) {
      fs.writeFileSync(filename, Buffer.from(ss.result.data, 'base64'));
      console.log(`[SCREENSHOT] Saved ${filename}`);
    }
  }

  await new Promise(resolve => ws.onopen = resolve);
  await sendCommand('Page.enable');
  await sendCommand('Runtime.enable');

  console.log('Waiting for Unity WebGL to initialize (15 seconds)...');
  await sleep(15000);
  await takeScreenshot('smoke_exterior.png');

  console.log('Selecting Room 101...');
  await clickAt(200, 810);
  await sleep(4000);
  await takeScreenshot('smoke_interior.png');

  console.log('Done smoke test!');
  ws.close();
  chrome.kill();
  process.exit(0);
}

main().catch(err => {
  console.error(err);
  chrome.kill();
  process.exit(1);
});
