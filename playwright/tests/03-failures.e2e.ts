import { expect, test, type Page } from '@playwright/test';
import {
  B24188,
  B24191,
  BLACK,
  NAVY,
  Simulators,
  confirmButton,
  key,
  openBatch,
  prepare,
  select,
  signIn,
  statusLine,
  tareButton,
  weigh,
  weighAll,
  weighedNow,
  xsrfHeader,
} from './support/station';

/**
 * STEP 30 (stories 011, 013): each failure scenario's defined behaviour on the packaged app. The
 * weighing scenarios use B-24190, which no other test completes; every one ends by quitting the
 * attempt, so nothing is recorded until "incomplete batch", which runs last.
 */
test.describe.configure({ mode: 'serial' });

let sim: Simulators;

test.beforeAll(async () => {
  sim = await Simulators.connect();
});

test.beforeEach(async () => {
  await sim.reset();
});

test.afterAll(async () => {
  await sim.clearFaults();
  await sim.dispose();
});

/** Quits the attempt (F4, then F4 again to discard what was captured) and checks nothing was recorded. */
async function quitAttempt(page: Page): Promise<void> {
  await key(page, 'F4');
  const discard = page.getByRole('button', { name: /Discard attempt\?/ });
  if (await discard.isVisible()) {
    await key(page, 'F4');
  }
  await expect(page.getByText('0 of 7 components weighed')).toBeVisible();
}

async function openB24190(page: Page): Promise<void> {
  await signIn(page);
  await openBatch(page, 'B-24190');
}

test.describe('scale unavailable', () => {
  test('should show no weight and block Confirm and Tare until the scale returns (safe failure)', async ({ page }) => {
    await openB24190(page);
    await prepare(page, NAVY, 'L-SCALE');
    await sim.pour(NAVY, 'rising');
    await expect(weighedNow(page)).not.toHaveText('—');

    await sim.fault('scale:scale-3kg');

    await expect(statusLine(page)).toContainText('SCALE OFFLINE');
    await expect(weighedNow(page)).toHaveText('—');
    await expect(confirmButton(page)).toBeDisabled();
    await expect(tareButton(page)).toBeDisabled();
    await expect(page.getByRole('alert')).toContainText('The scale is not responding.');
    await expect(page.locator('[data-device="scale-3kg"]')).toContainText('OFFLINE');

    await sim.clearFaults();

    await expect(statusLine(page)).not.toContainText('SCALE OFFLINE');
    await expect(weighedNow(page)).not.toHaveText('—');
    await expect(tareButton(page)).toBeEnabled();
    await quitAttempt(page);
  });
});

test.describe('weights that cannot be confirmed', () => {
  test('under tolerance: the weight stays below the range and cannot be confirmed', async ({ page }) => {
    await openB24190(page);
    await prepare(page, NAVY, 'L-UNDER');
    await sim.pour(NAVY, 'belowRange');

    await expect(statusLine(page)).toContainText('DISPENSE', { timeout: 20_000 });
    await expect(weighedNow(page)).toHaveText('2826.0 g', { timeout: 20_000 });
    await expect(confirmButton(page)).toBeDisabled();
    await key(page, 'F1');
    await expect(page.getByText('0 of 7 components weighed')).toBeVisible();
    await quitAttempt(page);
  });

  test('over tolerance: the overshoot cannot be confirmed and offers the recovery actions', async ({ page }) => {
    await openB24190(page);
    await prepare(page, NAVY, 'L-OVER');
    await sim.pour(NAVY, 'overshoot');

    await expect(statusLine(page)).toContainText('OVER TOLERANCE', { timeout: 20_000 });
    await expect(confirmButton(page)).toBeDisabled();
    await expect(page.getByTestId('surplus')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Supervisor override' })).toBeVisible();
    await key(page, 'F1');
    await expect(page.getByText('0 of 7 components weighed')).toBeVisible();
    await quitAttempt(page);
  });

  test('invalid weight: a reading over the scale capacity cannot be confirmed', async ({ page }) => {
    await openB24190(page);
    await prepare(page, NAVY, 'L-CAP');
    await sim.pour(NAVY, 'rising', 3500);

    await expect(statusLine(page)).toContainText('OVER CAPACITY', { timeout: 20_000 });
    await expect(page.getByText("The scale reading can't be used.")).toBeVisible();
    await expect(confirmButton(page)).toBeDisabled();
    await quitAttempt(page);
  });

  test('scale switching: exactly 3.0 kg still uses the precision scale', async ({ page }) => {
    await openB24190(page);
    await select(page, BLACK);

    await expect(page.getByTestId('scale-badge')).toContainText('3 kg precision');
  });
});

test.describe('re-tare and quit', () => {
  test('re-tare: the weight on the scale becomes the new zero', async ({ page }) => {
    await openB24190(page);
    await prepare(page, NAVY, 'L-RETARE');
    await sim.pour(NAVY, 'belowRange');
    await expect(weighedNow(page)).toHaveText('2826.0 g', { timeout: 20_000 });

    await key(page, 'F2');

    await expect(weighedNow(page)).toHaveText('0.0 g');
    await quitAttempt(page);
  });

  test('quit: the attempt is discarded safely and nothing is recorded', async ({ page }) => {
    await openB24190(page);
    await prepare(page, NAVY, 'L-QUIT');
    await sim.pour(NAVY, 'rising');
    await expect(statusLine(page)).toContainText('TARGET ACHIEVED', { timeout: 20_000 });

    await key(page, 'F4');
    await expect(page.getByRole('button', { name: /Discard attempt\?/ })).toBeVisible();
    await key(page, 'F4');

    // A safe exit: no record, the component back to pending, and nothing active until the operator chooses.
    await expect(page.getByText('0 of 7 components weighed')).toBeVisible();
    await expect(page.getByText('Select a pending component to weigh.')).toBeVisible();
    await expect(page.getByRole('button', { name: /^Remazol Navy RGB .* PENDING$/ })).toBeVisible();
  });
});

test.describe('printer unavailable', () => {
  test('should complete the batch, hold the ticket as print pending, and print once the printer returns', async ({ page }) => {
    test.setTimeout(240_000);
    await signIn(page);
    await openBatch(page, 'B-24188');
    await weighAll(page, sim, 'B-24188', B24188);
    await sim.fault('printer');

    await key(page, 'F1');

    await expect(page.getByRole('alert')).toContainText('printer is not responding');
    await expect(page.getByTestId('ticket-state')).toHaveText('Print pending');
    await expect(page.getByTestId('ticket-preview')).toBeVisible();
    expect(await sim.tickets()).toHaveLength(0);

    await sim.clearFaults();
    await key(page, 'F1');

    await expect(page.getByTestId('ticket-state')).toHaveText('Printed (1)');
    expect((await sim.tickets()).filter((t) => t.batchId === 'B-24188')).toHaveLength(1);
  });
});

test.describe('OrgaTEX unavailable', () => {
  test('on load: the queue says so and offers Retry', async ({ page }) => {
    await sim.fault('orgatex');
    await page.goto('/');
    await page.getByLabel('Badge number').fill('OP-1001');
    await page.getByRole('button', { name: /^OK/ }).click();

    await expect(page.getByRole('alert')).toContainText('OrgaTEX is not responding');
    await expect(page.locator('[data-device="orgatex"]')).toContainText('OFFLINE');
    await expect(page.getByRole('button', { name: 'Completed batches' })).toBeVisible();

    await sim.clearFaults();
    await page.getByRole('button', { name: /Retry/ }).click();

    await expect(page.getByRole('row', { name: 'Open batch B-24187' })).toBeVisible();
  });

  test('at completion: weighing goes on, the write-back fails visibly, and Retry sends it once OrgaTEX returns', async ({ page }) => {
    test.setTimeout(240_000);
    await signIn(page);
    await openBatch(page, 'B-24191');
    await sim.fault('orgatex');

    await weighAll(page, sim, 'B-24191', B24191);

    await expect(page.getByTestId('orgatex-state')).toContainText('Sync failed', { timeout: 60_000 });
    await expect(page.getByTestId('unsynced')).toContainText('1 UNSYNCED', { timeout: 30_000 });
    expect((await sim.consumption()).filter((c) => c.batchId === 'B-24191')).toHaveLength(0);

    await sim.clearFaults();
    await page.getByRole('button', { name: 'Retry' }).click();

    await expect(page.getByTestId('orgatex-state')).toContainText('CONFIRMED', { timeout: 30_000 });
    expect((await sim.consumption()).filter((c) => c.batchId === 'B-24191')).toHaveLength(1);
  });
});

test.describe('scanner unavailable', () => {
  test('should show SCANNER OFFLINE and still open a batch by touch', async ({ page }) => {
    await sim.fault('scanner');
    await signIn(page);

    await expect(page.locator('[data-device="scanner"]')).toContainText('OFFLINE');
    await openBatch(page, 'B-24190');
    await expect(page.getByRole('heading', { level: 1, name: NAVY.name, exact: true })).toBeVisible();
  });
});

test.describe('incomplete batch', () => {
  test('should not complete with components still to weigh', async ({ page }) => {
    await openB24190(page);
    await weigh(page, sim, NAVY, 'L-ONE');
    await expect(page.getByText('1 of 7 components weighed')).toBeVisible();

    const response = await page.request.post('/api/batches/B-24190/session/completion', {
      headers: await xsrfHeader(page),
    });

    expect(response.status()).toBe(409);
    expect((await response.json()).errorCode).toBe('Batch.Incomplete');
    expect((await sim.consumption()).filter((c) => c.batchId === 'B-24190')).toHaveLength(0);
    expect((await sim.tickets()).filter((t) => t.batchId === 'B-24190')).toHaveLength(0);

    await page.getByRole('button', { name: 'Back to queue' }).click();
    await expect(page.getByRole('row', { name: 'Open batch B-24190' })).toContainText('WEIGHING');
  });
});
