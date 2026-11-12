import { defineConfig, devices } from '@playwright/test';
import base from './playwright.config';

/**
 * Workbook evidence captures (STEPs 15, 20–27, 35). Same isolated stack as the E2E suite, but its own
 * test folder, so `npm run e2e` never runs these. Captures land in test-results-evidence/staging and are
 * promoted into evidence/ by evidence/promote.mjs, which never overwrites a file.
 */
export default defineConfig({
  ...base,
  testDir: './evidence',
  testMatch: '*.evidence.ts',
  outputDir: 'test-results-evidence/runs',
  reporter: [['list']],
  timeout: 600_000,
  // Full-size video: Playwright otherwise scales recordings down to fit 800×800.
  use: { ...base.use, video: { mode: 'on', size: { width: 1280, height: 800 } }, trace: 'off' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 800 } } }],
});
