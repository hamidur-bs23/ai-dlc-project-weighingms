// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

/** Loaded lazily: the local rule is an ES module. */
const wmsPlugin = import('./eslint/no-batch-literals.mjs').then((m) => ({
  rules: { 'no-batch-literals': m.default },
}));

module.exports = (async () => {
  const wms = await wmsPlugin;
  return defineConfig([
    { ignores: ['src/app/core/api/generated/**'] },
    {
      files: ['**/*.ts'],
      extends: [
        eslint.configs.recommended,
        tseslint.configs.strictTypeChecked,
        tseslint.configs.stylisticTypeChecked,
        angular.configs.tsRecommended,
      ],
      languageOptions: {
        parserOptions: { projectService: true, tsconfigRootDir: __dirname },
      },
      processor: angular.processInlineTemplates,
      plugins: { wms },
      rules: {
        '@angular-eslint/directive-selector': [
          'error',
          { type: 'attribute', prefix: 'wms', style: 'camelCase' },
        ],
        '@angular-eslint/component-selector': [
          'error',
          { type: 'element', prefix: 'wms', style: 'kebab-case' },
        ],
        '@angular-eslint/prefer-on-push-component-change-detection': 'error',
        '@typescript-eslint/no-explicit-any': 'error',
        '@typescript-eslint/restrict-template-expressions': ['error', { allowNumber: true }],
        'wms/no-batch-literals': 'error',
      },
    },
    {
      files: ['**/*.html'],
      extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
      plugins: { wms },
      rules: { 'wms/no-batch-literals': 'error' },
    },
    {
      // Specs describe the seeded training scenario, so they may name batches.
      files: ['**/*.spec.ts', 'src/testing/**/*.ts'],
      rules: {
        'wms/no-batch-literals': 'off',
        '@typescript-eslint/no-non-null-assertion': 'off',
        '@typescript-eslint/unbound-method': 'off',
      },
    },
  ]);
})();
