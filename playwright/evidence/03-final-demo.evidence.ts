import { expect, test, type Page } from '@playwright/test';
import { pathToFileURL } from 'node:url';
import { resolve } from 'node:path';
import { SIMULATORS_PORT } from '../stack.mjs';
import {
  B24187, Simulators, confirmButton, key, openBatch, prepare, select, signIn, statusLine, weigh, weighedNow,
} from '../tests/support/station';
import { keepVideo, root } from './sheet';

/**
 * Workbook STEP 35, Parts 1–8, as one narrated recording (a backup of the live demonstration). The
 * caption bar is display only: it is drawn over the page and changes nothing in the app.
 */
const sim = `http://localhost:${SIMULATORS_PORT}`;
const [BLUE, RED, YELLOW, SODIUM, SODA] = B24187 as [typeof B24187[0], typeof B24187[0], typeof B24187[0], typeof B24187[0], typeof B24187[0]];

async function say(page: Page, text: string, hold = 2500): Promise<void> {
  await page.evaluate((t) => {
    window.name = `demo:${t}`;
    (window as unknown as { __demo?: () => void }).__demo?.();
  }, text);
  await page.waitForTimeout(hold);
}

/** Ticks Chrome's built-in JSON viewer "Pretty-print" box, if this Chrome shows one. */
async function prettyPrint(page: Page): Promise<void> {
  const box = page.getByRole('checkbox', { name: /pretty/i });
  if (await box.isVisible().catch(() => false)) await box.check();
}

test('STEP 35 · final demonstration, Parts 1–8', async ({ page }) => {
  await page.addInitScript(() => {
    const draw = (): void => {
      const text = window.name.startsWith('demo:') ? window.name.slice(5) : '';
      let bar = document.getElementById('__demo');
      if (!bar) {
        bar = document.createElement('div');
        bar.id = '__demo';
        bar.setAttribute('aria-hidden', 'true');
        bar.style.cssText = 'position:fixed;left:0;right:0;bottom:0;z-index:2147483647;padding:10px 16px;background:rgba(15,20,25,.92);color:#fff;font:600 16px system-ui;pointer-events:none';
        document.documentElement.appendChild(bar);
      }
      bar.textContent = text;
      bar.style.display = text ? 'block' : 'none';
    };
    (window as unknown as { __demo: () => void }).__demo = draw;
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', draw);
    else draw();
  });
  const s = await Simulators.connect();
  await s.reset();

  // Part 1 · Batch
  await signIn(page);
  await say(page, 'Part 1 · Batch — the queue comes from the dummy OrgaTEX (GET /api/batches)', 3500);
  await openBatch(page, 'B-24187');
  await say(page, 'Part 1 · Batch — B-24187 selected · SO-9931 · JET-04 · Teal 4C');

  // Part 2 · Recipe
  await say(page, 'Part 2 · Recipe — five components, each with its own unit and scale', 3500);

  // Parts 3–4 · Scale and tolerance
  await weigh(page, s, BLUE, 'LOT-1');
  await weigh(page, s, RED, 'LOT-2');
  await prepare(page, YELLOW, 'LOT-3', s);
  await say(page, 'Part 3 · Scale — Yellow RGB (94.0 g) is weighed on the 3 kg precision scale');
  await s.pour(YELLOW, 'rising', 93.4);
  await expect(weighedNow(page)).toHaveText('93.4 g', { timeout: 20_000 });
  await expect(confirmButton(page)).toBeDisabled();
  await say(page, 'Part 4 · Tolerance — 93.4 g is below 93.5 g: confirmation is blocked', 3500);
  await s.pour(YELLOW, 'rising', 94.0);
  await expect(statusLine(page)).toContainText('TARGET ACHIEVED', { timeout: 20_000 });
  await expect(confirmButton(page)).toBeEnabled();
  await say(page, 'Part 4 · Tolerance — 94.0 g is in range: OK · Confirm is enabled', 3500);
  await key(page, 'F1');
  await expect(page.getByText('3 of 5 components weighed')).toBeVisible();
  await select(page, SODIUM);
  await expect(page.getByTestId('scale-badge')).toContainText('32 kg floor');
  await say(page, 'Part 3 · Scale — Sodium Sulphate (24 kg) switches to the 32 kg floor scale', 3500);

  // Part 5 · Complete
  await say(page, 'Part 5 · Complete — weighing the last two components on the 32 kg floor scale', 1500);
  await weigh(page, s, SODIUM, 'LOT-4');
  await weigh(page, s, SODA, 'LOT-5');
  await expect(page.getByRole('heading', { name: 'Batch B-24187 weighed' })).toBeVisible();
  await expect(page.getByTestId('orgatex-state')).toContainText('CONFIRMED', { timeout: 30_000 });
  await say(page, 'Part 5 · Complete — 5 of 5 weighed; consumption written to OrgaTEX: CONFIRMED', 4000);
  const summary = page.url();

  // Part 6 · Integration
  await say(page, 'Part 6 · Integration — the dummy OrgaTEX contract: POST /api/batches/{batchId}/consumption (Idempotency-Key) → 200 OK', 0); // caption first, so the new page shows it from its first frame
  await page.goto(`${sim}/scalar/#tag/batches/POST/api/batches/{batchId}/consumption`);
  await expect(page.getByText('Idempotency-Key').first()).toBeVisible();
  await expect(page.getByRole('heading', { name: '/api/batches/{batchId}/consumption' }).first()).toBeVisible();
  await say(page, 'Part 6 · Integration — the dummy OrgaTEX contract: POST /api/batches/{batchId}/consumption (Idempotency-Key) → 200 OK', 5000);
  await say(page, 'Part 6 · Integration — the stored transaction received by the dummy OrgaTEX', 0); // caption first, so the new page shows it from its first frame
  await page.goto(`${sim}/_sim/orgatex/consumption`);
  await expect(page.locator('body')).toContainText('B-24187');
  await prettyPrint(page);
  await say(page, 'Part 6 · Integration — the stored transaction received by the dummy OrgaTEX', 4000);

  // Part 7 · Ticket
  await say(page, 'Part 7 · Ticket — the 80 mm ticket preview (print is manual: F1)', 0); // caption first, so the new page shows it from its first frame
  await page.goto(summary);
  await expect(page.getByTestId('ticket-preview')).toBeVisible();
  await say(page, 'Part 7 · Ticket — the 80 mm ticket preview (print is manual: F1)');
  await key(page, 'F1');
  await expect(page.getByTestId('ticket-state')).toHaveText('Printed (1)');
  await say(page, 'Part 7 · Ticket — printed once: Printed (1)');
  await say(page, 'Part 7 · Ticket — what the dummy printer received', 0); // caption first, so the new page shows it from its first frame
  await page.goto(`${sim}/_sim/printer/tickets`);
  await expect(page.locator('body')).toContainText('B-24187');
  await prettyPrint(page);
  await say(page, 'Part 7 · Ticket — what the dummy printer received', 3500);

  // Part 8 · Automated test
  await say(page, 'Part 8 · Automated test — the Playwright E2E report (STEP 29)', 0); // caption first, so the new page shows it from its first frame
  await page.goto(pathToFileURL(resolve(root, 'evidence/STEP-29-playwright-report/index.html')).href);
  await page.waitForTimeout(1500);
  await say(page, 'Part 8 · Automated test — the Playwright E2E report (STEP 29)', 5000);

  await s.dispose();
  await keepVideo(page, 'STEP-35-final-demo');
});
