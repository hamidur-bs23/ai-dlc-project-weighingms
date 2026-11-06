---
stage: test
bolt: 013-assisted-weighing-station-ui
created: 2026-11-06T08:58:00Z
---

## Test Report: assisted-weighing-station-ui (bolt 013)

### Summary

- **Web tests**: **194/194 passed**, 47 of them new (147 before this bolt).
- **Backend tests**: **706/706 passed**, 1 new, and 2 existing tests extended for `operators` (705 before).
- **Coverage**: web 97.67% of lines overall, and 99.52% for `batch-summary`, which holds the ticket preview. Line coverage was not re-measured for the small backend change.
- **Other checks**: build 0 warnings, format clean, lint clean, web production build with no warnings, no EF model changes pending, `specsmd:validate` 0 issues, `specsmd:integrity` 0 inconsistencies.
- **Live run**: a real browser against the real Api, simulators and dummy printer on a throwaway PostgreSQL container, with all migrations applied to an empty database. The container was removed afterwards.
- **Barcode**: Chrome's `BarcodeDetector` read the drawn bars as `code_128`, value `B-24187`. This is an independent decoder, but not a physical scanner.

### Test Files

- [x] `web/src/app/features/batch-summary/confirm-and-print.spec.ts` - the screen, printing, 503, 409, F1, F3, an older refresh not undoing a print, and read-only mode
- [x] `web/src/app/features/batch-summary/ticket-preview.component.spec.ts` - lines unchanged, barcode, value beneath, unknown symbology, print states and the REPRINT note
- [x] `web/src/app/features/batch-review/batch-review.component.spec.ts` - columns, empty list, paging, errors, and opening by touch, Enter and Space
- [x] `web/src/app/app.routes.spec.ts` - read-only only from the review route, and sign-in required
- [x] `web/src/app/features/batch-queue/batch-queue.component.spec.ts` - 2 new: the list opens, and stays reachable when the queue can't load
- [x] `web/src/app/core/clock/plant-clock.service.spec.ts` - 1 new: plant date and time, across midnight
- [x] `web/src/app/features/batch-summary/batch-summary.component.spec.ts` - updated to Next batch and the ticket read
- [x] `tests/WeighingMS.Api.IntegrationTests/TicketEndpointsTests.cs` - 1 new: two operators on the review list, in order
- [x] `tests/WeighingMS.Api.IntegrationTests/WeighingOutcomeEndpointsTests.cs`, `tests/WeighingMS.Application.Tests/WeighingOutcomeServiceTests.cs` - `operators` asserted

### Acceptance Criteria Validation

- ✅ **Summary rows and per-chemical lines after completion**: seen live after the last component was confirmed on the real Weigh screen.
- ✅ **Next batch (F3) opens the queue**: tested and seen live, with the ticket printed and with it unprinted (test only).
- ✅ **Sync failed shows Retry**: covered by the web specs. Not repeated live here; seen live in bolt 011.
- ✅ **Preview matches the printed ticket, including the barcode**: the lines at the dummy printer matched the preview exactly, and the bars were decoded.
- ✅ **Print (F1) prints; later prints show REPRINT**: live, the first copy was plain and the second started `*** REPRINT ***`, with `isReprint: true` and a `TicketReprinted` audit entry. The rest of the reprint matched the preview.
- ✅ **Print pending shows in the preview**: seen live.
- ✅ **Review list shows time, operator and override count**: seen live, including an override count of 1 on B-24191.
- ✅ **Opening one is read-only**: live, only Back was offered, and F1 and F3 did nothing; the print count stayed at 2.
- ✅ **Transloco, status not by colour alone, build, lint and earlier tests**: all pass.

### Other Behaviour Seen Live

- **Printer down**: the banner appeared, the summary and preview stayed, and the strip showed PRINTER · OFFLINE. After the fault cleared, F1 printed one copy with the same key as the failed attempt, and the banner cleared.
- **Stale press, two tabs**: the second tab got 409 and the "already printed" message, then caught up to Printed (2). The printer stored no extra copy.
- **OrgaTEX down**: the queue showed its error, and the completed-batch list and a read-only batch still opened.
- **Override on a real ticket**: on B-24191 a confirm at 39.4 g (38.0 g ± 0.3 g) was refused with `Weighing.OutOfTolerance`, then a supervisor override passed. The preview showed the `OVR` line and `OVERRIDES: 1`.
- **Browser print**: only the ticket shows, about 80 mm wide, black on white.

### Test Quality Check

Five deliberate faults were put in one at a time and then reverted, with checksums confirming the source was restored:

- [x] Allowing print in read-only mode failed 3 tests.
- [x] Always sending a print count of 0 failed 1 test.
- [x] Letting the summary's state win over the ticket's failed 5 tests.
- [x] Trimming the ticket lines in the preview failed 1 test.
- [x] Opening the working screen from the review list failed 3 tests.

The route's read-only setting had no test at this point, so `app.routes.spec.ts` was added. That test was not itself checked with a deliberate fault.

### Issues Found

Found and fixed during this bolt:

- **Screen-reader text was visible on screen**: "Target achieved" showed below the frame after weighing. The CDK's `.cdk-visually-hidden` style was never loaded, so this dates from bolt 010. It is now 1 × 1 px, measured live.
- **Dark page behind a printed ticket**: the page background is now removed for print.
- **Lint**: 2 errors in the new specs.

Known and not fixed:

- **Title colour**: the mockup's "Batch … weighed" title is green and the app's is white. See `evidence/STEP-28-ui-validation/screen-3-comparison.png`.
- **Screen 3 differs from the mockup on purpose**: the extra Completed and Ticket rows, the per-chemical lines, full chemical names (TICKET-LINES), and the shorter consumption note (SUMMARY-CONSUMPTION-NOTE).
- **A refused OrgaTEX Retry replaces the whole screen**, as in bolt 011. Bolt 014 owns this (story 011).
- **Opening a summary by direct URL leaves the header without the batch line**, as in bolt 011. Seen again live. Bolt 014 takes it.
- **Retry on the review list after a failed page change** reloads the page the operator was on, not the one that failed.
- **No on-screen button starts a browser print.** F1 goes to the printer API; a browser print is only reached through the browser's own menu.

### Evidence

- [x] `evidence/STEP-27-ticket.png` - Confirm & print with the preview, from the live run
- [x] `evidence/STEP-27-ticket-override.png` - a ticket with an OVR line
- [x] `evidence/STEP-27-ticket-ui.txt` - tickets, print attempts, audit rows and print requests
- [x] `evidence/STEP-28-ui-validation/screen-3-comparison.png` - the mockup beside the working app
- [x] `evidence/STEP-30-printer-banner.png` - the printer-down banner with the screen intact
- [ ] `STEP-27-ticket-test.png` - the workbook's named screenshot; `evidence/STEP-27-ticket-tests.txt` from bolt 012 holds the results as text
- [ ] The rest of `STEP-28-ui-validation/` (screens 1 and 2, `validation.md`) - bolt 014

### Notes

- **How the batches were weighed**: only B-24187's last component went through the real Weigh screen. The other components, and all of B-24188 and B-24191, were weighed by calling the Api from the signed-in browser, because the screens under test come after completion. The full flow by hand is bolt 014's Playwright test (STEP 29).
- **No auto-print after recovery**: not directly counted in this run, because the browser tool lost that step's result after a long stall. The Api log shows the 200 only from the operator's press, 5 s after the 503, and the database shows one successful first print. Bolt 012's evidence counts it directly.
- **Elapsed on B-24188**: the 47 min in `STEP-30-printer-banner.png` comes from the stalled test run, not from the app.
- **Times in the evidence** are plant time, and the ticket content is invented for the lab (Q18, A-04).
- **Running stations**: an Api and simulators started before this bolt need a restart to return `operators`.
