import { expect, test } from '@playwright/test';
import {
  B24187,
  Simulators,
  expectAccessible,
  key,
  openBatch,
  signIn,
  weigh,
} from './support/station';

/**
 * STEP 29 (story 013): the whole B-24187 workflow on the packaged app against the dummies, by hand
 * on the real screens: sign in → select → five components confirmed → complete → OrgaTEX write-back
 * → ticket → review.
 */
test.describe('B-24187 end to end', () => {
  let sim: Simulators;

  test.beforeAll(async () => {
    sim = await Simulators.connect();
    await sim.reset();
  });

  test.afterAll(async () => {
    await sim.dispose();
  });

  test('should weigh all five components, write back to OrgaTEX and print the ticket', async ({ page }) => {
    test.setTimeout(240_000);

    await signIn(page);
    await expectAccessible(page);
    await expect(page.getByRole('row', { name: 'Open batch B-24187' })).toContainText('QUEUED');

    await openBatch(page, 'B-24187');
    await expect(page.getByTestId('batch-crumb')).toContainText('B-24187');
    await expect(page.getByTestId('batch-crumb')).toContainText('SO-9931');
    await expect(page.getByTestId('batch-crumb')).toContainText('JET-04');

    for (const [i, c] of B24187.entries()) {
      await weigh(page, sim, c, `LOT-${String(i + 1)}`);
      if (i === 2) {
        // The Weigh screen mid-batch, with the scale switching to the floor scale next.
        await expect(page.getByText('3 of 5 components weighed')).toBeVisible();
        await expectAccessible(page);
      }
    }

    // Screen 3 · Confirm & print.
    await expect(page.getByRole('heading', { name: 'Batch B-24187 weighed' })).toBeVisible();
    await expect(page.getByTestId('summary-weighed')).toHaveText('5 of 5');
    await expect(page.getByTestId('orgatex-state')).toContainText('CONFIRMED', { timeout: 30_000 });
    await expect(page.getByTestId('consumption-note')).toBeVisible();
    await expect(page.getByTestId('ticket-state')).toHaveText('Print pending');
    await expect(page.getByTestId('ticket-preview')).toBeVisible();
    await expectAccessible(page);

    const consumption = (await sim.consumption()).filter((c) => c.batchId === 'B-24187');
    expect(consumption).toHaveLength(1);
    expect(consumption[0]?.lines.map((l) => l.lot)).toEqual(['LOT-1', 'LOT-2', 'LOT-3', 'LOT-4', 'LOT-5']);

    await key(page, 'F1');
    await expect(page.getByTestId('ticket-state')).toHaveText('Printed (1)');
    const tickets = await sim.tickets();
    expect(tickets).toHaveLength(1);
    expect(tickets[0]?.batchId).toBe('B-24187');
    expect(tickets[0]?.isReprint).toBe(false);
    const printed = tickets[0]?.lines.join('\n') ?? '';
    for (const c of B24187) {
      expect(printed).toContain(c.name);
    }

    // Next batch (F3): the queue, with B-24187 complete.
    await key(page, 'F3');
    await expect(page.getByRole('row', { name: 'Open batch B-24187' })).toContainText('COMPLETE');

    // Completed jobs stay reviewable (FR-18): the review list, then the batch read-only.
    await page.getByRole('button', { name: 'Completed batches' }).click();
    const row = page.getByRole('row', { name: /B-24187/ });
    await expect(row).toContainText('R. Karim');
    await row.click();
    await expect(page.getByTestId('read-only')).toBeVisible();
    await expect(page.getByTestId('batch-crumb')).toContainText('B-24187');
    await expect(page.getByRole('button', { name: /Print ticket|Reprint ticket/ })).toHaveCount(0);
  });
});
