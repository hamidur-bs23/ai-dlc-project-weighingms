/**
 * Local ESLint rule: no batch-ID literals (B-12345) in application code (workbook Rule 5, STEP 19).
 * Every batch the UI shows comes from WeighingMS.Api, so a literal batch ID in a component, service
 * or template means scenario data has been hard-coded. Specs are exempt (configured in eslint.config.js).
 *
 * The rule scans the raw source text, so it covers TypeScript and Angular templates alike.
 */
const BATCH_ID = /\bB-\d{5}\b/g;

export default {
  meta: {
    type: 'problem',
    docs: { description: 'Disallow hard-coded batch IDs (B-\\d{5}) in application code' },
    messages: {
      batchLiteral:
        "Hard-coded batch ID '{{id}}'. Batches come from the Api (GET /api/batches), never from UI code.",
    },
    schema: [],
  },
  create(context) {
    return {
      Program(node) {
        const text = context.sourceCode.text;
        for (const match of text.matchAll(BATCH_ID)) {
          const start = context.sourceCode.getLocFromIndex(match.index);
          const end = context.sourceCode.getLocFromIndex(match.index + match[0].length);
          context.report({
            node,
            loc: { start, end },
            messageId: 'batchLiteral',
            data: { id: match[0] },
          });
        }
      },
    };
  },
};
