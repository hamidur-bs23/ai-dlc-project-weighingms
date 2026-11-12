import { expect, test } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { render, root, staging } from './sheet';

/**
 * STEP 15 (bolt 001's plan, exactly as Claude Code presented it for approval, from the session
 * transcript) and STEP 27 (the ticket test runs captured by the shell into staging).
 */
test('STEP 15 · bolt 001 plan stage', async ({ browser }) => {
  const t = JSON.parse(readFileSync(resolve(staging, '_step15.json'), 'utf8')) as { session: string; timestamp: string; plan: string; result: string };
  expect(t.plan).toContain('001-station-simulators');
  expect(t.result).toContain('User has approved your plan');
  const plan = readFileSync(resolve(root, 'memory-bank/bolts/001-station-simulators/implementation-plan.md'), 'utf8');
  const stories = plan.slice(plan.indexOf('### Stories and requirements in scope'), plan.indexOf('### What will change')).trim();
  expect(stories).toContain('FR-25');
  await render(browser, {
    title: 'STEP 15 · /specsmd-construction-agent · Plan stage of bolt 001-station-simulators (CHECKPOINT 5)',
    source: `Claude Code session ${t.session}, plan presented for approval at ${t.timestamp} (verbatim from the transcript); stories from memory-bank/bolts/001-station-simulators/implementation-plan.md`,
    note: 'Approved at CHECKPOINT 5 (APPROVE PLAN). No code was written before this approval.',
    text: `Claude's plan:\n\n${t.plan}\n\n────────────────────────────────────────\nimplementation-plan.md · ${stories}`,
  }, 'STEP-15-plan');
});

test('STEP 27 · ticket tests', async ({ browser }) => {
  const text = readFileSync(resolve(staging, '_step27.txt'), 'utf8');
  expect(text).not.toMatch(/\bfailed [1-9]|Failed: [1-9]|✗|FAIL /);
  await render(browser, {
    title: 'STEP 27 · Ticket generation tests (Domain, Application, Api integration, simulator printer, web)',
    source: 'dotnet test (Microsoft.Testing.Platform) and npm run test:web (Vitest), run by the evidence script',
    text,
    columns: 2,
  }, 'STEP-27-ticket-test');
});
