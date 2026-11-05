// Proves the "no hard-coded batches" lint rule fires (STEP 19): a component that declares
// `const batches = [...]` must fail lint, and the same code in a spec must pass.
// The fixtures are written into src/app for the run (so the typed lint config applies) and removed after.
import { rmSync, writeFileSync } from 'node:fs';
import { ESLint } from 'eslint';

const offending = "export const batches = [{ batchId: 'B-24187', shade: 'Teal 4C' }];\n";
const fixtures = [
  { file: 'src/app/lint-fixture.ts', expectError: true },
  { file: 'src/app/lint-fixture.spec.ts', expectError: false },
];

const eslint = new ESLint({ cwd: process.cwd() });
let ok = true;
try {
  for (const { file, expectError } of fixtures) {
    writeFileSync(file, offending);
    const [result] = await eslint.lintFiles([file]);
    const hits = result.messages.filter((m) => m.ruleId === 'wms/no-batch-literals').length;
    const pass = expectError ? hits > 0 : hits === 0;
    ok &&= pass;
    console.log(
      `${pass ? 'ok  ' : 'FAIL'} ${file}: ${hits} batch-literal error(s), expected ${expectError ? '≥ 1' : '0'}`,
    );
  }
} finally {
  for (const { file } of fixtures) rmSync(file, { force: true });
}
process.exit(ok ? 0 : 1);
