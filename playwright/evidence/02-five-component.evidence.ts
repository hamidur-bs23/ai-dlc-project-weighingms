import { expect, test } from '@playwright/test';
import { B24187, Simulators, openBatch, signIn, weigh } from '../tests/support/station';
import { keepVideo } from './sheet';

/** Workbook STEP 23: one continuous recording of B-24187's five components, in recipe order, to completion. */
test('STEP 23 · five-component progression on B-24187', async ({ page }) => {
  const s = await Simulators.connect();
  await s.reset();
  await signIn(page);
  await openBatch(page, 'B-24187');
  await page.waitForTimeout(1500);
  for (const [i, c] of B24187.entries()) {
    await weigh(page, s, c, `LOT-${String(i + 1)}`);
    if (i < B24187.length - 1) await expect(page.getByText(`${String(i + 1)} of 5 components weighed`)).toBeVisible();
    await page.waitForTimeout(1500);
  }
  await expect(page.getByRole('heading', { name: 'Batch B-24187 weighed' })).toBeVisible();
  await expect(page.getByTestId('summary-weighed')).toHaveText('5 of 5');
  await expect(page.getByTestId('orgatex-state')).toContainText('CONFIRMED', { timeout: 30_000 });
  await expect(page.getByTestId('ticket-preview')).toBeVisible();
  await page.waitForTimeout(3000);
  await s.dispose();
  await keepVideo(page, 'STEP-23-five-component-demo');
});
