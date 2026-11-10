// STEP 28 evidence: the mockup and the working app, screen by screen, side by side.
// Run against a running E2E stack after the suite (which leaves B-24187 completed and B-24190
// part-weighed): node playwright/evidence/capture-step28.mjs
import { chromium } from '@playwright/test';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { API_PORT, SIMULATORS_PORT } from '../stack.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const out = resolve(root, 'evidence/STEP-28-ui-validation');
const app = `http://localhost:${API_PORT}`;
const sim = `http://localhost:${SIMULATORS_PORT}`;
const viewport = { width: 1280, height: 860 };

const browser = await chromium.launch();
const shots = {};

// The mockup's three views.
const mock = await browser.newPage({ viewport });
await mock.goto(pathToFileURL(resolve(root, 'docs/customize-weighing-station.html')).href);
for (const [n, view] of [[1, 'queue'], [2, 'weigh'], [3, 'done']]) {
  await mock.locator(`button[data-view="${view}"]`).click();
  await mock.waitForTimeout(300);
  shots[`mock-${n}`] = await mock.screenshot();
}

// The app: queue, a part-poured Yellow RGB on B-24190, and B-24187's Confirm & print.
const page = await browser.newPage({ viewport });
const post = (path, data) => page.request.post(`${sim}${path}`, data === undefined ? {} : { data });
await post('/_sim/reset');
await page.goto(`${app}/`);
await page.getByLabel('Badge number').fill('OP-1001');
await page.getByRole('button', { name: /^OK/ }).click();
await page.getByText('Scan a batch card to begin').waitFor();
shots['app-1'] = await page.screenshot();

await page.getByRole('row', { name: 'Open batch B-24190' }).click();
await page.getByRole('complementary', { name: 'Recipe' }).getByRole('button', { name: /Remazol Yellow RGB/ }).click();
await page.getByRole('heading', { level: 1, name: 'Remazol Yellow RGB' }).waitFor();
await page.getByLabel('Lot (scan or type)').fill('2026-0387');
await page.getByLabel('Lot (scan or type)').press('Enter');
await page.getByLabel('Container').press('Enter');
await post('/_sim/scales/scale-3kg/container');
await page.getByRole('button', { name: /^Tare/ }).click();
await page.getByRole('button', { name: /^Re-tare/ }).waitFor();
await post('/_sim/scales/scale-3kg/scenario', { scenario: 'rising', target: { value: 64, unit: 'g' }, tolerance: 0.4 });
await page.waitForTimeout(2500);
await post('/_sim/scales/scale-3kg/scenario', { scenario: 'stable' });
await page.waitForTimeout(800);
shots['app-2'] = await page.screenshot();
await page.getByRole('button', { name: 'Back to queue' }).click();

await page.getByRole('row', { name: 'Open batch B-24187' }).click();
await page.getByRole('heading', { name: 'Batch B-24187 weighed' }).waitFor();
await page.getByTestId('ticket-preview').waitFor();
shots['app-3'] = await page.screenshot();

// Side by side, mockup on the left.
const sheet = await browser.newPage({ viewport: { width: 2 * viewport.width + 48, height: viewport.height + 72 } });
const uri = (png) => `data:image/png;base64,${png.toString('base64')}`;
const titles = { 1: '1 · Select batch', 2: '2 · Weigh', 3: '3 · Confirm & print' };
for (const n of [1, 2, 3]) {
  await sheet.setContent(`<!doctype html><body style="margin:0;padding:16px;background:#fff;font:600 15px system-ui">
    <div style="display:flex;gap:16px">
      <figure style="margin:0"><figcaption style="margin-bottom:8px">Mockup · ${titles[n]}</figcaption><img src="${uri(shots[`mock-${n}`])}" width="${viewport.width}"></figure>
      <figure style="margin:0"><figcaption style="margin-bottom:8px">WeighingMS (Compose full) · ${titles[n]}</figcaption><img src="${uri(shots[`app-${n}`])}" width="${viewport.width}"></figure>
    </div></body>`);
  await sheet.screenshot({ path: `${out}/screen-${n}-comparison.png`, fullPage: true });
}

await browser.close();
console.log(`wrote ${out}/screen-{1,2,3}-comparison.png`);
