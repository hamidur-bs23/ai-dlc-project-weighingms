import { expect, test, type Page } from '@playwright/test';
import { resolve } from 'node:path';
import { SIMULATORS_PORT } from '../stack.mjs';
import {
  B24187, Simulators, confirmButton, key, openBatch, prepare, select, signIn, statusLine, weigh, weighedNow,
} from '../tests/support/station';
import { json, render, shot, sheet, staging } from './sheet';

/**
 * Workbook STEPs 20, 21, 22, 24, 25 and 26 on B-24187, on a fresh isolated stack. Every screenshot is
 * taken only after the assertions above it hold, so a passing run means each pictured state was real.
 */
const [BLUE, RED, YELLOW, SODIUM, SODA] = B24187 as [typeof B24187[0], typeof B24187[0], typeof B24187[0], typeof B24187[0], typeof B24187[0]];
const sim = `http://localhost:${SIMULATORS_PORT}`;
const png = (n: string): string => resolve(staging, `${n}.png`);
const badge = (page: Page) => page.getByTestId('scale-badge');

/** Pours exactly `value` g (the simulator lands on the target after its pour duration) and waits for it. */
async function pourTo(page: Page, s: Simulators, value: number): Promise<void> {
  await s.pour(YELLOW, 'rising', value);
  await expect(weighedNow(page)).toHaveText(`${value.toFixed(1)} g`, { timeout: 20_000 });
  await page.waitForTimeout(1500);
  await expect(page.getByText('UNSTABLE', { exact: true })).toBeHidden();
  await expect(weighedNow(page)).toHaveText(`${value.toFixed(1)} g`);
}

test('STEPs 20–26 stills on B-24187', async ({ page, browser }) => {
  const s = await Simulators.connect();
  await s.reset();

  // STEP 20 · the Weigh screen mid-pour on the first component.
  await signIn(page);
  await openBatch(page, 'B-24187');
  await expect(page.getByTestId('batch-crumb')).toContainText('B-24187');
  await expect(page.getByTestId('batch-crumb')).toContainText('SO-9931');
  await expect(page.getByTestId('batch-crumb')).toContainText('JET-04');
  await prepare(page, BLUE, 'LOT-1', s);
  await s.pour(BLUE, 'rising');
  await page.waitForTimeout(3500);
  await expect(statusLine(page)).toContainText(/DISPENSE|NEAR TARGET/);
  await expect(confirmButton(page)).toBeDisabled();
  await expect(badge(page)).toContainText('3 kg precision');
  await shot(page, 'STEP-20-weigh-screen');
  await expect(statusLine(page)).toContainText('TARGET ACHIEVED', { timeout: 20_000 });
  await expect(confirmButton(page)).toBeEnabled();
  await key(page, 'F1');
  await expect(page.getByText('1 of 5 components weighed')).toBeVisible();

  await weigh(page, s, RED, 'LOT-2');

  // STEP 22 · Yellow RGB 94.0 g ± 0.5 g: the five workbook points.
  await prepare(page, YELLOW, 'LOT-3', s);
  const points: { g: number; status: RegExp; ok: boolean }[] = [
    { g: 93.4, status: /DISPENSE|NEAR TARGET/, ok: false },
    { g: 93.5, status: /WITHIN TOLERANCE/, ok: true },
    { g: 94.0, status: /TARGET ACHIEVED/, ok: true },
    { g: 94.5, status: /WITHIN TOLERANCE/, ok: true },
    { g: 94.6, status: /OVER TOLERANCE — SURPLUS \+0\.6 g \(\+0\.6%\)/, ok: false }, // surplus is measured from the target
  ];
  const panels: { png: string; caption: string }[] = [];
  for (const p of points) {
    await pourTo(page, s, p.g);
    await expect(statusLine(page)).toContainText(p.status);
    if (p.ok) await expect(confirmButton(page)).toBeEnabled();
    else await expect(confirmButton(page)).toBeDisabled();
    const name = `_s22-${p.g.toFixed(1)}`;
    await shot(page, name);
    const text = (await statusLine(page).innerText()).replace(/\s+/g, ' ').trim();
    panels.push({ png: png(name), caption: `${p.g.toFixed(1)} g → ${p.ok ? 'can confirm (OK enabled)' : 'cannot confirm (OK disabled)'} · "${text}"` });
  }

  // STEP 24 · the overshoot (94.6 g is past the 94.5 g upper limit): PD-05 option 1, no recalculation.
  await expect(page.getByTestId('surplus')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Supervisor override' })).toBeVisible();
  await expect(page.getByRole('button', { name: /^Quit/ })).toBeVisible();
  await shot(page, 'STEP-24-overshoot-ui');
  await sheet(browser, panels, 'STEP-22-tolerance-ui', 3, 'STEP 22 · Remazol Yellow RGB, target 94.0 g ± 0.5 g (range 93.5 – 94.5 g, inclusive)');

  // Recovery: quit and re-weigh, then STEP 25 · confirm in tolerance.
  await key(page, 'F4');
  const discard = page.getByRole('button', { name: /Discard attempt\?/ });
  await expect(discard).toBeVisible();
  await discard.click();
  await expect(page.getByText('Select a pending component to weigh.')).toBeVisible();
  await expect(page.getByText('2 of 5 components weighed')).toBeVisible();
  await prepare(page, YELLOW, 'LOT-3', s);
  await pourTo(page, s, 94.0);
  await expect(statusLine(page)).toContainText('TARGET ACHIEVED');
  await expect(confirmButton(page)).toBeEnabled();
  await expect(badge(page)).toContainText('3 kg precision');
  await shot(page, '_s21-yellow');
  const confirmation = page.waitForResponse((r) => r.url().endsWith('/api/batches/B-24187/session/confirmation') && r.request().method() === 'POST');
  await key(page, 'F1');
  const response = await confirmation;
  expect(response.status()).toBe(201);
  const created = await response.json() as { record: Record<string, unknown> };
  expect(created.record['chemicalName']).toBe('Remazol Yellow RGB');
  expect(created.record['lot']).toBe('LOT-3');
  await expect(page.getByText('3 of 5 components weighed')).toBeVisible();
  const yellowRow = page.getByRole('complementary', { name: 'Recipe' }).getByRole('button', { name: /Remazol Yellow RGB/ });
  await expect(yellowRow).toContainText('✓ 94.0 g');
  await shot(page, '_s25-after');
  await render(browser, {
    title: 'POST /api/batches/B-24187/session/confirmation → 201 Created',
    source: `WeighingMS Api, the response to the F1 press (server re-read the scale and re-checked the tolerance) · request body ${response.request().postData() ?? ''}`,
    note: 'Body field "record" shown; the "session" field (the updated batch state) is left out for space.',
    text: `HTTP/1.1 201 Created\n\n"record": ${json(created.record)}`,
  }, '_s25-record');
  await sheet(browser, [
    { png: png('_s21-yellow'), caption: '1 · Before: 94.0 g, TARGET ACHIEVED → OK · Confirm enabled' },
    { png: png('_s25-after'), caption: '2 · After F1: Yellow RGB ✓ 94.0 g, 3 of 5 weighed; the station moves to the next component' },
    { png: png('_s25-record'), caption: '3 · The weighing record created (actual, operator, component, lot, container, tare, scale, timestamp)' },
  ], 'STEP-25-confirmation', 3, 'STEP 25 · Confirmation of Remazol Yellow RGB (94.0 g ± 0.5 g) on B-24187');

  // STEP 21 · the scale switches with the component.
  await select(page, SODIUM);
  await expect(badge(page)).toContainText('32 kg floor');
  await shot(page, '_s21-sodium');
  await sheet(browser, [
    { png: png('_s21-yellow'), caption: 'Remazol Yellow RGB · 94.0 g ± 0.5 g (≤ 3 kg) → 3 kg precision scale' },
    { png: png('_s21-sodium'), caption: 'Sodium Sulphate · 24 kg ± 0.12 kg (> 3 kg) → 32 kg floor scale' },
  ], 'STEP-21-scale-switch', 2, 'STEP 21 · Automatic scale selection (threshold 3 kg, configurable in Weighing:ScaleSelection)');
  await weigh(page, s, SODIUM, 'LOT-4');
  await weigh(page, s, SODA, 'LOT-5');

  // STEP 26 · completion and the OrgaTEX write-back.
  await expect(page.getByRole('heading', { name: 'Batch B-24187 weighed' })).toBeVisible();
  await expect(page.getByTestId('summary-weighed')).toHaveText('5 of 5');
  await expect(page.getByTestId('orgatex-state')).toContainText('CONFIRMED', { timeout: 30_000 });

  const writeBack = await (await page.request.get('/api/batches/B-24187/session/write-back')).json();
  const all = await (await page.request.get(`${sim}/_sim/orgatex/consumption`)).json() as { batchId: string; idempotencyKey: string; lines: { lot: string }[] }[];
  const stored = all.filter((c) => c.batchId === 'B-24187');
  expect(stored).toHaveLength(1);
  const record = stored[0]!;
  expect(record.lines.map((l) => l.lot)).toEqual(['LOT-1', 'LOT-2', 'LOT-3', 'LOT-4', 'LOT-5']);

  const url = `/api/batches/B-24187/consumption`;
  const body = { lines: (record as unknown as { lines: unknown[] }).lines };
  const headers = { 'Content-Type': 'application/json', 'Idempotency-Key': record.idempotencyKey };
  const replay = await page.request.post(`${sim}${url}`, { data: body, headers });
  expect(replay.status()).toBe(200);
  const receipt = await replay.json();
  const after = (await (await page.request.get(`${sim}/_sim/orgatex/consumption`)).json() as { batchId: string }[]).filter((c) => c.batchId === 'B-24187');
  expect(after).toHaveLength(1);

  const note = 'The Api\'s background dispatcher sent this POST server-side (its own HTTP exchange is not logged). Shown here: the same request replayed with the stored idempotency key; a repeated key returns the first receipt, and the dummy OrgaTEX still holds exactly one record.';
  await render(browser, { title: `STEP 26 · Request · POST ${url} (dummy OrgaTEX, training contract)`, source: `dummy OrgaTEX (WeighingMS.Simulators.Host) · POST ${url} · key and lines from the stored consumption`, note, text: `POST ${url} HTTP/1.1\nContent-Type: application/json\nIdempotency-Key: ${record.idempotencyKey}\n\n${json(body)}` }, 'STEP-26-request');
  await render(browser, { title: 'STEP 26 · Response · 200 OK', source: `response to the replay above, plus GET /api/batches/B-24187/session/write-back (Api)`, note, text: `HTTP/1.1 ${replay.status()} ${replay.statusText()}\nContent-Type: ${replay.headers()['content-type'] ?? ''}\n\n${json(receipt)}\n\n# Api write-back state for B-24187 (GET /api/batches/B-24187/session/write-back)\n${json(writeBack)}` }, 'STEP-26-response');
  await render(browser, { title: 'STEP 26 · Stored data · consumption received by the dummy OrgaTEX', source: `GET /_sim/orgatex/consumption (simulator inspection endpoint), filtered to B-24187 · ${after.length} record`, text: json(after) }, 'STEP-26-data');

  await s.dispose();
});
