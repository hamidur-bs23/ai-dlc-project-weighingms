import AxeBuilder from '@axe-core/playwright';
import { expect, request, type APIRequestContext, type Page } from '@playwright/test';
import { SIMULATORS_PORT } from '../../stack.mjs';

/**
 * One recipe component as the seed defines it (`Seed/seed.json`). The tests state the expected
 * values themselves, so a seed change shows up as a failing test rather than passing silently.
 */
export interface Component {
  name: string;
  target: number;
  unit: 'g' | 'kg';
  tolerance: number;
  container: string;
  scaleId: 'scale-3kg' | 'scale-32kg';
  scaleName: '3 kg precision' | '32 kg floor';
}

const precision = { scaleId: 'scale-3kg', scaleName: '3 kg precision' } as const;
const floor = { scaleId: 'scale-32kg', scaleName: '32 kg floor' } as const;

export const B24187: readonly Component[] = [
  { name: 'Remazol Brill. Blue RSPL', target: 1240, unit: 'g', tolerance: 6, container: 'C-09', ...precision },
  { name: 'Remazol Red RB', target: 386, unit: 'g', tolerance: 2, container: 'C-11', ...precision },
  { name: 'Remazol Yellow RGB', target: 94, unit: 'g', tolerance: 0.5, container: 'C-12', ...precision },
  { name: 'Sodium Sulphate', target: 24, unit: 'kg', tolerance: 0.12, container: 'D-03', ...floor },
  { name: 'Soda Ash', target: 9.6, unit: 'kg', tolerance: 0.048, container: 'D-05', ...floor },
];

export const B24188: readonly Component[] = [
  { name: 'Remazol Brill. Blue RSPL', target: 1343.3, unit: 'g', tolerance: 6.5, container: 'C-14', ...precision },
  { name: 'Remazol Red RB', target: 418.2, unit: 'g', tolerance: 2.2, container: 'C-15', ...precision },
  { name: 'Remazol Yellow RGB', target: 101.8, unit: 'g', tolerance: 0.5, container: 'C-16', ...precision },
  { name: 'Sodium Sulphate', target: 26, unit: 'kg', tolerance: 0.13, container: 'D-07', ...floor },
  { name: 'Soda Ash', target: 10.4, unit: 'kg', tolerance: 0.052, container: 'D-08', ...floor },
];

export const B24191: readonly Component[] = [
  { name: 'Remazol Brill. Red F3B', target: 420, unit: 'g', tolerance: 2, container: 'C-31', ...precision },
  { name: 'Remazol Yellow RGB', target: 38, unit: 'g', tolerance: 0.3, container: 'C-32', ...precision },
  { name: 'Sodium Sulphate', target: 11.4, unit: 'kg', tolerance: 0.057, container: 'D-15', ...floor },
  { name: 'Soda Ash', target: 3.8, unit: 'kg', tolerance: 0.019, container: 'D-16', ...floor },
];

/** B-24190's first two components: 2850 g, and exactly 3.0 kg, which is still the precision scale. */
export const NAVY: Component = { name: 'Remazol Navy RGB', target: 2850, unit: 'g', tolerance: 12, container: 'C-21', ...precision };
export const BLACK: Component = { name: 'Remazol Black B', target: 3, unit: 'kg', tolerance: 0.015, container: 'C-22', ...precision };

export type Scenario = 'rising' | 'belowRange' | 'enterRange' | 'overshoot' | 'stable';
export type FaultTarget = 'orgatex' | 'printer' | 'scanner' | `scale:${Component['scaleId']}`;

/** The simulators' control and inspection endpoints (`/_sim/*`). */
export class Simulators {
  private constructor(private readonly api: APIRequestContext) {}

  static async connect(): Promise<Simulators> {
    return new Simulators(await request.newContext({ baseURL: `http://localhost:${SIMULATORS_PORT}` }));
  }

  /** All dummies back to their start state, with no faults. */
  async reset(): Promise<void> {
    expect((await this.api.post('/_sim/reset')).ok()).toBe(true);
    await this.clearFaults();
  }

  async fault(target: FaultTarget, mode: 'unavailable' | 'timeout' | 'error500' = 'unavailable'): Promise<void> {
    expect((await this.api.post('/_sim/faults', { data: { target, mode } })).ok()).toBe(true);
  }

  async clearFaults(): Promise<void> {
    expect((await this.api.delete('/_sim/faults')).ok()).toBe(true);
  }

  /** Starts a pour on the component's scale. `target` may differ from the recipe, for example to go over capacity. */
  async pour(c: Component, scenario: Scenario, target: number = c.target): Promise<void> {
    const response = await this.api.post(`/_sim/scales/${c.scaleId}/scenario`, {
      data: { scenario, target: { value: target, unit: c.unit }, tolerance: c.tolerance },
    });
    expect(response.status(), await response.text()).toBe(204);
  }

  /** Takes the filled container off the component's scale and puts an empty one on. */
  async placeEmptyContainer(c: Component): Promise<void> {
    expect((await this.api.post(`/_sim/scales/${c.scaleId}/container`)).status()).toBe(204);
  }

  async consumption(): Promise<{ batchId: string; lines: { chemicalId: string; lot: string }[] }[]> {
    return (await this.api.get('/_sim/orgatex/consumption')).json();
  }

  async tickets(): Promise<{ batchId: string; isReprint: boolean; lines: string[] }[]> {
    return (await this.api.get('/_sim/printer/tickets')).json();
  }

  async dispose(): Promise<void> {
    await this.api.dispose();
  }
}

/** Escapes a chemical name for use in a RegExp ("Brill." has a dot). */
const literal = (text: string): RegExp => new RegExp(text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'));

export async function signIn(page: Page, badge = 'OP-1001'): Promise<void> {
  await page.goto('/');
  await page.getByLabel('Badge number').fill(badge);
  await page.getByRole('button', { name: /^OK/ }).click();
  await expect(page.getByText('Scan a batch card to begin')).toBeVisible();
}

/** Touches a queue row (works with the scanner down: SCANNER-FALLBACK-SCOPE). */
export async function openBatch(page: Page, batchId: string): Promise<void> {
  await page.getByRole('row', { name: `Open batch ${batchId}` }).click();
  await expect(page.getByRole('complementary', { name: 'Recipe' })).toBeVisible();
}

/** Presses a function key with the focus on no field, as an operator's hand on the keypad would. */
export async function key(page: Page, name: 'F1' | 'F2' | 'F3' | 'F4'): Promise<void> {
  await page.getByTestId('status-line').or(page.getByRole('heading', { level: 1 })).first().click();
  await page.keyboard.press(name);
}

export const statusLine = (page: Page) => page.getByTestId('status-line');
export const weighedNow = (page: Page) => page.getByTestId('weighed-now');
export const confirmButton = (page: Page) => page.getByRole('button', { name: /OK · Confirm/ });
export const tareButton = (page: Page) => page.getByRole('button', { name: /^(Re-)?tare/i });

/** Makes the component the active one, choosing it on the recipe rail if it isn't already. */
export async function select(page: Page, c: Component): Promise<void> {
  const heading = page.getByRole('heading', { level: 1, name: c.name, exact: true });
  if (!(await heading.isVisible())) {
    await page.getByRole('complementary', { name: 'Recipe' }).getByRole('button', { name: literal(c.name) }).click();
  }
  await expect(heading).toBeVisible();
}

/**
 * A fresh empty container on the scale, then lot, container and its tare: everything except the
 * pour. `sim` is optional only for the first component on a scale after a reset.
 */
export async function prepare(page: Page, c: Component, lot: string, sim?: Simulators): Promise<void> {
  await select(page, c);
  await sim?.placeEmptyContainer(c);
  await expect(page.getByTestId('scale-badge')).toContainText(c.scaleName);
  const lotField = page.getByLabel('Lot (scan or type)');
  await lotField.fill(lot);
  await lotField.press('Enter');
  const container = page.getByLabel('Container');
  await expect(container).toHaveValue(c.container);
  await container.press('Enter');
  await expect(page.getByText(`LOT ${lot} · CONTAINER ${c.container}`, { exact: false })).toBeVisible();
  await key(page, 'F2');
  await expect(page.getByRole('button', { name: /^Re-tare/ })).toBeVisible();
}

/** Weighs one component to its target and confirms it with F1. */
export async function weigh(page: Page, sim: Simulators, c: Component, lot: string): Promise<void> {
  await prepare(page, c, lot, sim);
  await sim.pour(c, 'rising');
  await expect(statusLine(page)).toContainText('TARGET ACHIEVED', { timeout: 20_000 });
  await expect(confirmButton(page)).toBeEnabled();
  await key(page, 'F1');
  await expect(page.getByRole('heading', { level: 1, name: c.name, exact: true })).toBeHidden();
}

/** Weighs a whole batch in recipe order, ending on Confirm & print. */
export async function weighAll(page: Page, sim: Simulators, batchId: string, components: readonly Component[]): Promise<void> {
  for (const [i, c] of components.entries()) {
    await weigh(page, sim, c, `L-${String(i + 1)}`);
  }
  await expect(page.getByRole('heading', { name: `Batch ${batchId} weighed` })).toBeVisible();
}

/** The antiforgery header the Api requires on a state-changing call, from the signed-in page's cookie. */
export async function xsrfHeader(page: Page): Promise<Record<string, string>> {
  const cookie = (await page.context().cookies()).find((c) => c.name === 'XSRF-TOKEN');
  expect(cookie, 'the XSRF-TOKEN cookie').toBeDefined();
  return { 'X-XSRF-TOKEN': cookie?.value ?? '' };
}

/** WCAG 2 A and AA checks (ux-guide); serious and critical findings fail the test. */
export async function expectAccessible(page: Page): Promise<void> {
  const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  const blocking = result.violations
    .filter((v) => v.impact === 'serious' || v.impact === 'critical')
    .flatMap((v) =>
      v.nodes.map((n) => `${v.id} at ${n.target.join(' ')}: ${n.any[0]?.message ?? v.help}`),
    );
  expect(blocking).toEqual([]);
}
