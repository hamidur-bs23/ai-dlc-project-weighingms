---
stage: implement
bolt: 013-assisted-weighing-station-ui
created: 2026-11-06T06:40:00Z
---

## Implementation Walkthrough: assisted-weighing-station-ui (bolt 013)

### Summary

The interim summary is now screen 3 · Confirm & print: the server's summary, the ticket preview with its Code 128 barcode, Print or Reprint (F1) and Next batch (F3). A new completed-batch list opens the same screen read-only. The review list's Api response gains the operators, because story 012 requires them.

### Structure Overview

The summary component keeps owning the server state and the actions. A small preview component only displays the ticket the Api sent, so no ticket content is composed in the browser. The review list is its own feature with its own route, and a second route opens the summary component in read-only mode through route data.

### Completed Work

- [x] `web/src/app/features/batch-summary/batch-summary.component.ts`, `.html`, `.scss` - Confirm & print: the ticket read, print, reprint, printer and stale-press errors, F1, F3 and the read-only mode (template and styles moved out to files)
- [x] `web/src/app/features/batch-summary/ticket-preview.component.ts` - the frozen lines, barcode, value beneath it, print state and print styles
- [x] `web/src/app/features/batch-review/batch-review.component.ts`, `.html`, `.scss` - the completed-batch list with paging
- [x] `web/src/app/app.routes.ts` - `/review` and the read-only `/review/:batchId`
- [x] `web/src/app/features/batch-queue/batch-queue.component.ts`, `.html`, `.scss` - the Completed batches button, shown even when the queue can't load
- [x] `web/src/app/core/weighing/weighing-api.service.ts`, `web/src/app/core/api/api.types.ts` - the ticket, print and review list calls and types
- [x] `web/src/app/core/clock/plant-clock.service.ts`, `plant-time.pipe.ts` - a plant date and time, for records older than today
- [x] `web/src/styles/_tokens.scss`, `_base.scss` - ticket paper tokens and the print-only-the-ticket rules
- [x] `web/public/i18n/en.json` - the new text
- [x] `web/package.json`, `web/angular.json`, `package-lock.json` - JsBarcode, allow-listed as a CommonJS dependency
- [x] `src/WeighingMS.Application/Weighing/BatchSummary.cs`, `WeighingSessionService.cs` - operators on a completed batch
- [x] `src/WeighingMS.Api/Contracts/Weighing/BatchSummaryResponse.cs`, `Controllers/WeighingReviewController.cs` - operators in the response
- [x] `src/WeighingMS.Api/openapi/WeighingMS.Api.json`, `web/src/app/core/api/generated/schema.ts` - the regenerated contract and types
- [x] `web/src/app/features/batch-summary/batch-summary.component.spec.ts` - existing tests updated to Next batch and the ticket read (new tests are Stage 3)
- [x] `memory-bank/standards/decision-index.md`, `CLAUDE.md` - the decisions and routes

### Key Decisions

- **D1 to D3 applied**: two ways into the screen (REVIEW-READ-ONLY), any signed-in user from the queue (REVIEW-ACCESS), and only the confirmed consumption sentence (SUMMARY-CONSUMPTION-NOTE).
- **The ticket's own state wins over the summary's**: a summary refresh already on its way could otherwise show Print pending just after a print succeeded.
- **The ticket is read once**: it is frozen, so following a pending write-back doesn't repeat the read.
- **Print errors keep the screen**: they show in a banner above the summary, so the operator still sees the preview and can press F1 again.

### Deviations from Plan

- **Failed Retry**: the plan keeps print errors on screen. A refused OrgaTEX Retry still replaces the screen with its error, as it did in bolt 011, because error states across screens belong to bolt 014 (story 011).
- **The direct-URL header gap**: not fixed here. Opening a summary or review by URL leaves the header without the batch line, so it stays with bolt 014.

### Dependencies Added

- [x] `jsbarcode` 3.12.3 - draws the Code 128 barcode (named in the tech-stack standard; pinned exactly; includes its own types)

### Developer Notes

- The review list reads only WeighingMS's own records, so it should stay available during an OrgaTEX outage. Stage 3 should confirm this.
- Nothing here has run in a browser against the real Api yet: layout, the barcode and F1 and F3 still need the live run in Stage 3.
