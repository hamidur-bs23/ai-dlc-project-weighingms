// Assembles the workbook's "FINAL EVIDENCE PACKAGE" (sections 01-project-bootstrap … 34-final-demo) from
// evidence/ and FINAL-EVIDENCE.md. The originals are only read, never moved or changed. Output (gitignored):
// dist/evidence-package/NN-section/… and dist/evidence-package.zip.
//   node scripts/evidence-package.mjs            check, assemble and zip
//   node scripts/evidence-package.mjs --table    print the mapping as a Markdown table instead
import { cpSync, existsSync, mkdirSync, readdirSync, rmSync, statSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const out = resolve(root, 'dist/evidence-package');

/** Package section → workbook steps → files (paths under evidence/, except the root FINAL-EVIDENCE.md). */
const sections = [
  ['01-project-bootstrap', 'STEP 0–1', ['STEP-00-git.png', 'STEP-01-files.png']],
  ['02-specsmd-installation', 'STEP 2', ['STEP-02-installation.png', 'STEP-02-manifest.txt']],
  ['03-context-analysis', 'STEP 3, 5', ['STEP-03-master-agent.png', 'STEP-05-context-analysis.png']],
  ['04-dashboard', 'STEP 4', ['STEP-04-dashboard.png']],
  ['05-intent', 'STEP 6', ['STEP-06-intent.png', 'STEP-06-intent-file.png']],
  ['06-intent-validation', 'STEP 6 · CHECKPOINT 1', ['STEP-06-GATE-APPROVED.png']],
  ['07-requirements', 'STEP 7 · CHECKPOINT 2', ['STEP-07-requirements.png', 'STEP-07-GATE-APPROVED.png']],
  ['08-ui-analysis', 'STEP 8', ['STEP-08-ui-analysis']],
  ['09-system-context', 'STEP 9', ['STEP-09-system-context.png', 'STEP-09-system-context.md']],
  ['10-units', 'STEP 10 · CHECKPOINT 3', ['STEP-10-units.png', 'STEP-10-unit-map.png']],
  ['11-stories', 'STEP 11', ['STEP-11-stories.png']],
  ['12-bolt-plan', 'STEP 12', ['STEP-12-bolt-plan.png']],
  ['13-inception-complete', 'STEP 12 · CHECKPOINT 4', ['STEP-12-INCEPTION-COMPLETE.png']],
  ['14-bolt-execution', 'STEP 13–15 · CHECKPOINT 5', ['STEP-13-bolt-list.png', 'STEP-14-bolt-start.png', 'STEP-15-plan.png']],
  // STEP 16 asks for no file of its own: the dummy APIs are shown by a response from each of them.
  ['15-dummy-apis', 'STEP 16', ['STEP-17-api-response.png', 'STEP-21-scale-api.png', 'STEP-26-request.png', 'STEP-27-printer-api.png']],
  ['16-dummy-data', 'STEP 17', ['STEP-17-dummy-data.json']],
  ['17-api-tests', 'STEP 18', ['STEP-18-api-tests.txt', 'STEP-18-api-results.png']],
  ['18-batch-selection', 'STEP 19', ['STEP-19-batch-screen.png', 'STEP-19-network.png', 'STEP-19-test.png', 'STEP-19-api-flow.txt', 'STEP-19-web-test.txt']],
  ['19-weighing', 'STEP 20', ['STEP-20-weigh-screen.png']],
  ['20-scale-integration', 'STEP 21', ['STEP-21-scale-switch.png', 'STEP-21-scale-api.png', 'STEP-21-scale-api.txt']],
  ['21-tolerance', 'STEP 22', ['STEP-22-tolerance-tests.txt', 'STEP-22-tolerance-ui.png', 'STEP-22-tolerance-ui-tests.txt']],
  ['22-component-progression', 'STEP 23', ['STEP-23-five-component-demo.mp4']],
  ['23-overshoot', 'STEP 24', ['STEP-24-overshoot-decision.md', 'STEP-24-overshoot-test.png', 'STEP-24-overshoot-ui.png', 'STEP-24-overshoot-tests.txt']],
  ['24-confirmation', 'STEP 25', ['STEP-25-confirmation.png', 'STEP-25-transaction.json']],
  ['25-orgatex-writeback', 'STEP 26', ['STEP-26-ui.png', 'STEP-26-request.png', 'STEP-26-response.png', 'STEP-26-data.png', 'STEP-26-writeback.txt']],
  ['26-ticket', 'STEP 27', ['STEP-27-ticket.png', 'STEP-27-printer-api.png', 'STEP-27-ticket-test.png', 'STEP-27-ticket-tests.txt', 'STEP-27-ticket-ui.txt', 'STEP-27-ticket.txt', 'STEP-27-ticket-override.png']],
  ['27-ui-validation', 'STEP 28', ['STEP-28-ui-validation']],
  ['28-e2e', 'STEP 29', ['STEP-29-playwright-report', 'STEP-29-e2e-video.mp4']],
  ['29-failure-tests', 'STEP 30', ['STEP-30-failure-tests.md', 'STEP-30-orgatex-outage.txt', 'STEP-30-orgatex-sync-failed.png', 'STEP-30-orgatex-retry-confirmed.png', 'STEP-30-printer-outage.txt', 'STEP-30-printer-banner.png', 'STEP-30-printer-print-pending.png']],
  ['30-final-project-status', 'STEP 31', ['STEP-31-project-status.png', 'STEP-31-project-status.md']],
  ['31-final-dashboard', 'STEP 32', ['STEP-32-final-dashboard.png', 'STEP-32-final-dashboard-specs.png', 'STEP-32-final-dashboard-bolts.png']],
  ['32-final-validation', 'STEP 33', ['STEP-33-final-validation.md']],
  ['33-final-evidence', 'STEP 34', ['../FINAL-EVIDENCE.md']],
  ['34-final-demo', 'STEP 35', ['STEP-35-final-demo.mp4']],
];

const src = (f) => resolve(root, 'evidence', f);
const shown = (f) => (f.startsWith('../') ? f.slice(3) : `evidence/${f}`);

if (process.argv.includes('--table')) {
  console.log('| Section | Workbook | Files |\n|---|---|---|');
  for (const [name, steps, files] of sections) console.log(`| ${name} | ${steps} | ${files.map((f) => `\`${shown(f)}\``).join(', ')} |`);
  process.exit(0);
}

// Every mapped file must exist, and every file in evidence/ must be mapped somewhere.
const missing = sections.flatMap(([, , files]) => files).filter((f) => !existsSync(src(f)));
const mapped = new Set(sections.flatMap(([, , files]) => files));
const unmapped = readdirSync(resolve(root, 'evidence')).filter((f) => !f.startsWith('.') && !mapped.has(f)); // skip .DS_Store etc.
if (sections.length !== 34 || missing.length || unmapped.length) {
  console.error({ sections: sections.length, missing, unmapped });
  process.exit(1);
}

rmSync(out, { recursive: true, force: true });
let count = 0;
for (const [name, , files] of sections) {
  const dir = resolve(out, name);
  mkdirSync(dir, { recursive: true });
  for (const f of files) {
    const to = resolve(dir, f.replace(/^\.\.\//, ''));
    cpSync(src(f), to, { recursive: true });
    count += statSync(src(f)).isDirectory() ? 0 : 1;
  }
}
const zip = resolve(root, 'dist/evidence-package.zip');
rmSync(zip, { force: true });
const z = spawnSync('zip', ['-qr', zip, 'evidence-package'], { cwd: resolve(root, 'dist'), stdio: 'inherit' });
if (z.status !== 0) process.exit(z.status ?? 1);
console.log(`34 sections, ${count} files plus folders → ${out}\nzip → ${zip} (${(statSync(zip).size / 1e6).toFixed(1)} MB)`);
