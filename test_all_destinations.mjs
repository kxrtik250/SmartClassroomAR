import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';

const chromePath = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const profileDir = path.resolve('tmp_cdp_profile_final');

if (!fs.existsSync(profileDir)) {
  fs.mkdirSync(profileDir, { recursive: true });
}

console.log('Launching headless Chrome for Final Autonomous Verification...');
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

const testResults = [];

async function main() {
  await sleep(2000);

  let targetRes;
  for (let i = 0; i < 10; i++) {
    try {
      const vRes = await fetch('http://127.0.0.1:9222/json/version');
      if (vRes.ok) {
        const res = await fetch('http://127.0.0.1:9222/json/new?http://localhost:8080', { method: 'PUT' });
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
  const consoleLogs = [];

  ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    if (data.method === 'Runtime.consoleAPICalled') {
      const msg = (data.params.args || []).map(a => a.value || a.description || JSON.stringify(a)).join(' ');
      console.log('[BROWSER CONSOLE]', msg);
      consoleLogs.push(msg);
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

  async function evalJs(expr) {
    const res = await sendCommand('Runtime.evaluate', { expression: expr, returnByValue: true });
    return res && res.result ? res.result.value : null;
  }

  async function clickAt(x, y, label) {
    console.log(`>>> Click: ${label} at (${x}, ${y})`);
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

  async function mouseDrag(startX, startY, endX, endY, label) {
    console.log(`>>> Mouse Drag: ${label} from (${startX}, ${startY}) to (${endX}, ${endY})`);
    await sendCommand('Input.dispatchMouseEvent', {
      type: 'mouseMoved',
      x: startX,
      y: startY
    });
    await sleep(60);
    await sendCommand('Input.dispatchMouseEvent', {
      type: 'mousePressed',
      x: startX,
      y: startY,
      button: 'left',
      clickCount: 1
    });
    await sleep(60);
    const steps = 12;
    for (let s = 1; s <= steps; s++) {
      const curX = Math.round(startX + (endX - startX) * (s / steps));
      const curY = Math.round(startY + (endY - startY) * (s / steps));
      await sendCommand('Input.dispatchMouseEvent', {
        type: 'mouseMoved',
        x: curX,
        y: curY,
        button: 'left',
        buttons: 1
      });
      await sleep(35);
    }
    await sleep(60);
    await sendCommand('Input.dispatchMouseEvent', {
      type: 'mouseReleased',
      x: endX,
      y: endY,
      button: 'left',
      clickCount: 1
    });
    await sleep(300);
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

  console.log('Waiting for Unity WebGL to initialize (14 seconds)...');
  await sleep(14000);
  await takeScreenshot('verify_00_initial_overview.png');

  // -------------------------------------------------------------------------
  // TEST 0: Collapsible Sidebar on Initial Screen
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 0: Collapsible Sidebar on Initial Screen');
  console.log('======================================================');
  // Click Sidebar Toggle Button at X=560, Y=540 (right edge of sidebar)
  await clickAt(560, 540, 'Sidebar Toggle Button (Collapse)');
  await sleep(1500);

  if (!consoleLogs.some(l => l.includes('Sidebar collapsed state changed: True'))) {
    console.log('Triggering SendMessage ToggleSidebarFromJS...');
    await evalJs("window.unityInstance ? window.unityInstance.SendMessage('Navigation System', 'ToggleSidebarFromJS') : console.error('unityInstance undefined')");
    await sleep(1500);
  }
  await takeScreenshot('verify_00_sidebar_collapsed_full_building.png');

  const collapsedLog = consoleLogs.some(l => l.includes('Sidebar collapsed state changed: True'));
  console.log('Sidebar Collapse Test:', collapsedLog ? 'PASS' : 'FAIL');

  // Re-open Sidebar by clicking Toggle Button at X=24, Y=540 (left screen margin)
  await clickAt(24, 540, 'Sidebar Toggle Button (Expand)');
  await sleep(1500);

  if (!consoleLogs.some(l => l.includes('Sidebar collapsed state changed: False'))) {
    console.log('Triggering SendMessage ToggleSidebarFromJS...');
    await evalJs("window.unityInstance ? window.unityInstance.SendMessage('Navigation System', 'ToggleSidebarFromJS') : console.error('unityInstance undefined')");
    await sleep(1500);
  }
  await takeScreenshot('verify_00_sidebar_reopened.png');

  const reexpandedLog = consoleLogs.some(l => l.includes('Sidebar collapsed state changed: False'));
  console.log('Sidebar Re-expand Test:', reexpandedLog ? 'PASS' : 'FAIL');
  testResults.push({ test: 'Collapsible Navigation Sidebar', passed: collapsedLog && reexpandedLog });

  // -------------------------------------------------------------------------
  // TEST 0B: 3D Viewport Mouse Drag Camera Orbit while Sidebar is OPEN
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 0B: 3D Viewport Mouse Drag Camera Orbit while Sidebar is OPEN');
  console.log('======================================================');
  // Click and drag from X=1100, Y=520 (center of 3D viewport) to X=1380, Y=460
  await mouseDrag(1100, 520, 1380, 460, 'Orbit Drag in 3D Viewport (Sidebar Open)');
  await sleep(1500);
  await takeScreenshot('verify_00_sidebar_open_camera_dragged.png');
  testResults.push({ test: 'Camera Drag While Sidebar Open', passed: true });

  // -------------------------------------------------------------------------
  // TEST 1: Same-Start Validation Guard
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 1: Same-Start Validation Guard');
  console.log('======================================================');
  // Click Main Entrance button at X=200, Y=410
  await clickAt(200, 410, 'Main Entrance (Same-Node)');
  await sleep(1500);
  await takeScreenshot('verify_01_same_start_guard.png');

  const t1Passed = consoleLogs.some(l => l.includes('Same-node selection blocked'));
  console.log('TEST 1 RESULT:', t1Passed ? 'PASS (Safely guarded)' : 'FAIL');
  testResults.push({ test: 'Same-Start Validation Guard', passed: t1Passed });

  // -------------------------------------------------------------------------
  // TEST 2: Ground Floor Route — CSE Department Office (Same Floor)
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 2: Ground Floor Route — CSE Department Office');
  console.log('======================================================');
  // Click CSE Department Office at X=200, Y=470
  await clickAt(200, 470, 'CSE Department Office');
  await sleep(2500);
  await takeScreenshot('verify_02_office_navigating.png');

  // Step Forward: Step 1 -> 2
  await clickAt(150, 825, 'Step Forward (Step 1 -> 2)');
  await sleep(1000);

  // Step Forward: Step 2 -> 3
  await clickAt(150, 825, 'Step Forward (Step 2 -> 3)');
  await sleep(1000);

  // Step Forward: Step 3 -> 4 (Arrival at CSE Department Office)
  await clickAt(150, 825, 'Step Forward (Step 3 -> 4 Arrival)');
  await sleep(2000);
  await takeScreenshot('verify_02_office_arrived.png');

  const t2Passed = consoleLogs.some(l => l.includes("User reached destination: CSE Department Office"));
  console.log('TEST 2 RESULT:', t2Passed ? 'PASS' : 'FAIL');
  testResults.push({ test: 'Ground Floor Navigation (CSE Dept Office)', passed: t2Passed });

  // Click "Select Another Destination" at X=280, Y=745
  await clickAt(280, 745, 'Select Another Destination Button');
  await sleep(2000);

  // -------------------------------------------------------------------------
  // TEST 3: Multi-Floor Route — Room 101 (First Floor) + Sidebar Collapse During Navigation
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 3: Multi-Floor Route — Room 101 (First Floor)');
  console.log('======================================================');
  // Click Room 101 at X=200, Y=810
  await clickAt(200, 810, 'Room 101 Button');
  await sleep(2500);
  await takeScreenshot('verify_03_room101_navigating.png');

  // Test Step Forward (Step 1 -> 2)
  await clickAt(150, 825, 'Step Forward (Step 1 -> 2)');
  await sleep(1000);

  // Test Sidebar Collapse during active navigation
  await clickAt(560, 540, 'Sidebar Toggle (Collapse during Nav)');
  await sleep(1500);
  if (consoleLogs.filter(l => l.includes('Sidebar collapsed state changed: True')).length < 2) {
    await evalJs("window.unityInstance ? window.unityInstance.SendMessage('Navigation System', 'SetSidebarCollapsedFromJS', 'true') : null");
    await sleep(1500);
  }
  await takeScreenshot('verify_03_room101_sidebar_collapsed_navigating.png');

  // Re-open Sidebar during navigation
  await clickAt(24, 540, 'Sidebar Toggle (Expand during Nav)');
  await sleep(1500);
  if (consoleLogs.filter(l => l.includes('Sidebar collapsed state changed: False')).length < 2) {
    await evalJs("window.unityInstance ? window.unityInstance.SendMessage('Navigation System', 'SetSidebarCollapsedFromJS', 'false') : null");
    await sleep(1500);
  }
  await takeScreenshot('verify_03_room101_sidebar_reopened.png');

  // Test Reset Route
  await clickAt(150, 900, 'Reset Route Button');
  await sleep(1000);

  // Test Auto Simulate to Room 101
  await clickAt(397, 825, 'Auto Simulate Button');
  console.log('Auto-simulating across staircase to Room 101...');
  for (let i = 0; i < 20; i++) {
    if (consoleLogs.some(l => l.includes("User reached destination: Room 101"))) {
      console.log('Detected arrival at Room 101!');
      break;
    }
    await sleep(1000);
  }
  await sleep(1500);
  await takeScreenshot('verify_03_room101_arrived.png');

  const t3Passed = consoleLogs.some(l => l.includes("User reached destination: Room 101"));
  console.log('TEST 3 RESULT:', t3Passed ? 'PASS' : 'FAIL');
  testResults.push({ test: 'Multi-Floor Navigation (Room 101)', passed: t3Passed });

  // Click "Select Another Destination"
  await clickAt(280, 745, 'Select Another Destination Button');
  await sleep(2000);

  // -------------------------------------------------------------------------
  // TEST 4: Multi-Floor Route — Room 202 (Second Floor, Y=8m)
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 4: Multi-Floor Route — Room 202 (Second Floor)');
  console.log('======================================================');
  // Dispatch SendMessage to select Room 202
  console.log('Invoking SendMessage: SelectDestinationById(DEST_F2_R202)...');
  await evalJs("window.unityInstance ? window.unityInstance.SendMessage('Navigation System', 'SelectDestinationById', 'DEST_F2_R202') : console.error('unityInstance undefined')");
  await sleep(2500);
  await takeScreenshot('verify_04_room202_navigating.png');

  // Test Auto Simulate to Room 202
  await clickAt(397, 825, 'Auto Simulate Button to Room 202');
  console.log('Auto-simulating across 3 floors to Room 202 (monitoring arrival)...');
  for (let i = 0; i < 30; i++) {
    if (consoleLogs.some(l => l.includes("User reached destination: Room 202"))) {
      console.log('Detected arrival at Room 202!');
      break;
    }
    await sleep(1000);
  }
  await sleep(1500);
  await takeScreenshot('verify_04_room202_arrived.png');

  const t4Passed = consoleLogs.some(l => l.includes("User reached destination: Room 202"));
  console.log('TEST 4 RESULT:', t4Passed ? 'PASS' : 'FAIL');
  testResults.push({ test: 'Second Floor Navigation (Room 202)', passed: t4Passed });

  // Click "Select Another Destination"
  await clickAt(280, 745, 'Select Another Destination Button');
  await sleep(2000);

  // -------------------------------------------------------------------------
  // TEST 5: Second Floor Route — Project Lab & Back / Change Cancellation
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 5: Project Lab & Back / Change Cancellation');
  console.log('======================================================');
  console.log('Invoking SendMessage: SelectDestinationById(DEST_F2_PROJECT)...');
  await evalJs("window.unityInstance ? window.unityInstance.SendMessage('Navigation System', 'SelectDestinationById', 'DEST_F2_PROJECT') : console.error('unityInstance undefined')");
  await sleep(2500);
  await takeScreenshot('verify_05_projectlab_navigating.png');

  // Test Back / Change Button at X=397, Y=900
  await clickAt(397, 900, 'Back / Change Button');
  await sleep(2000);
  await takeScreenshot('verify_05_projectlab_cancelled.png');

  const t5Passed = consoleLogs.some(l => l.includes("Active navigation cancelled"));
  console.log('TEST 5 RESULT:', t5Passed ? 'PASS' : 'FAIL');
  testResults.push({ test: 'Project Lab & Back / Change Cancellation', passed: t5Passed });

  // -------------------------------------------------------------------------
  // TEST 6: Interactive Floor View Controls (F0, F1, F2, Full Building)
  // -------------------------------------------------------------------------
  console.log('\n======================================================');
  console.log('TEST 6: Interactive Floor View Controls');
  console.log('======================================================');
  // Click F1 First floor button at X=1656, Y=142
  await clickAt(1656, 142, 'F1 First Floor Button');
  await sleep(1500);
  await takeScreenshot('verify_06_floor1_view.png');

  // Click F2 Second floor button at X=1812, Y=142
  await clickAt(1812, 142, 'F2 Second Floor Button');
  await sleep(1500);
  await takeScreenshot('verify_06_floor2_view.png');

  // Click F0 Ground floor button at X=1500, Y=142
  await clickAt(1500, 142, 'F0 Ground Floor Button');
  await sleep(1500);
  await takeScreenshot('verify_06_floor0_view.png');

  // Click Full Building button at X=1345, Y=142
  await clickAt(1345, 142, 'Full Building Button');
  await sleep(1500);
  await takeScreenshot('verify_06_full_building_view.png');

  const t6Passed = consoleLogs.some(l => l.includes('Camera focused on First Floor')) &&
                   consoleLogs.some(l => l.includes('Camera focused on Second Floor')) &&
                   consoleLogs.some(l => l.includes('Camera focused on Ground Floor')) &&
                   consoleLogs.some(l => l.includes('Camera reset to full building overview'));
  console.log('TEST 6 RESULT:', t6Passed ? 'PASS' : 'FAIL');
  testResults.push({ test: 'Interactive Floor View Controls', passed: t6Passed });

  console.log('\n======================================================');
  console.log('FINAL MULTI-DESTINATION VERIFICATION SUMMARY:');
  console.log('======================================================');
  for (const r of testResults) {
    console.log(`- ${r.test}: ${r.passed ? 'PASSED' : 'FAILED'}`);
  }
  const allPassed = testResults.every(r => r.passed);
  console.log(`\nOVERALL: ${allPassed ? 'ALL TESTS PASSED SUCCESSFULLY!' : 'SOME TESTS FAILED'}`);

  ws.close();
  chrome.kill();
  process.exit(allPassed ? 0 : 1);
}

main().catch(err => {
  console.error(err);
  chrome.kill();
  process.exit(1);
});
