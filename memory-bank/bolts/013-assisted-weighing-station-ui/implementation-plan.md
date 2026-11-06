---
stage: plan
bolt: 013-assisted-weighing-station-ui
created: 2026-11-06T06:33:00Z
---

## Implementation Plan: assisted-weighing-station-ui (bolt 013)

### Objective

Turn the interim summary into **screen 3 · Confirm & print**: the summary and per-chemical lines, the ticket preview with its barcode, Print / Reprint (F1) and Next batch (F3). Add the completed-batch review list, which the mockup doesn't have.

Workbook: the UI side of **STEP 26** and **STEP 27** (Print / Print Preview), and the screen 3 part of **STEP 28**.

### Stories in scope

1. `009-confirm-and-print-screen`: FR-18, FR-20
2. `010-ticket-preview-and-print`: FR-21
3. `012-completed-batch-review-list`: FR-18, FR-19

### What will change

1. **Confirm & print** (`features/batch-summary`, extended, not replaced):
   - Two columns as in the mockup: summary and actions on the left, ticket preview on the right.
   - **Next batch (F3)** replaces "Back to queue" and opens the queue.
   - **Print ticket (F1)** becomes **Reprint ticket (F1)** after the first print. Both are disabled while a press is in flight and when the batch has no ticket.
   - The existing summary rows, Retry and write-back following stay as they are.
2. **Ticket preview** (new `wms-ticket-preview`):
   - Shows the server's ticket lines exactly as sent, in mono on the mockup's paper style. It never composes or reformats them.
   - Barcode drawn as SVG by JsBarcode from the server's symbology and value, with the batch ID beneath it.
   - Shows Print pending or Printed (n), and notes that the next copy will be marked REPRINT.
   - `@media print` styles: black on white, 80 mm (ux-guide).
3. **Printing** (`WeighingApiService`): `ticket()` and `printTicket(expectedPrintCount)`, sending the count the screen last showed.
   - 503 `Printer.Unavailable`: a banner on the screen; the ticket stays print pending and F1 can be pressed again.
   - 409 `Ticket.AlreadyPrinted`: reload the ticket and say another press already printed it. Nothing extra prints.
   - These errors show in a banner above the content, so a failed press doesn't replace the summary and preview.
4. **Review list** (new `features/batch-review`, route `/review`):
   - Newest first, 20 per page with Previous and Next: batch, order, machine, shade, completed time, operators, override count.
   - Opening a row shows that batch's summary and preview (see D1).
5. **Backend (small addition)**: `CompletedBatchResponse` has no operator, and story 012 requires one. Add `operators`, resolved the same way as on the summary, and regenerate the OpenAPI document and types. No migration expected.
6. **Text and tidying**: Transloco keys in `en.json`; remove the INTERIM-SUMMARY comments and mark that decision superseded.

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-06)

- **D1: option 1.** Two ways in: the queue opens the working screen, the review list opens it read-only (REVIEW-READ-ONLY).
- **D2: option 1.** Any signed-in user, from a "Completed batches" button on the queue (REVIEW-ACCESS).
- **D3: option 1.** Show only "Actual consumption posted against the batch.", once OrgaTEX confirms (SUMMARY-CONSUMPTION-NOTE).

#### Options as presented

**D1. What does "read-only" mean when a batch is opened from the review list?** Story 012 says its summary and ticket preview are read-only. Story 007-003 expects a Print key "on a reopened summary".

1 - **(Recommended)** **Two ways in.** A queue row opens the working screen with Print, Reprint and Retry. A review list row opens the same screen with no action keys except Back. This satisfies both stories' wording, and a supervisor reviewing can't reprint by accident.
2 - **One screen, Reprint available everywhere.** Simpler, with one less mode. "Read-only" then means only that records can't be edited, and reprints from review are possible (still audited).

**D2. Who can open the review list, and from where?** The story's persona is a supervisor, the Api allows any signed-in user, and the mockup has no such screen (Q-UI-5).

1 - **(Recommended)** **Any signed-in user, via a "Completed batches" button on the queue.** Matches the Api and FR-18's "completed jobs stay reviewable". No backend change.
2 - **Supervisors only.** Matches the persona, but needs an Api policy change and hides records operators may need.

**D3. The mockup's sentence "Actual consumption posted against the batch. Container released for transport to JET-04."** No requirement defines releasing a container, so showing it would imply a rule that doesn't exist.

1 - **(Recommended)** **Show only "Actual consumption posted against the batch."**, and only once OrgaTEX has confirmed it.
2 - **Show the full sentence once confirmed.** Closest to the mockup, with the machine added to the summary response, but it states something the system doesn't check.
3 - **Leave it out.** The "Written to OrgaTEX" row already says this.

### Assumptions (say if any is wrong)

- **Next batch with the ticket unprinted is allowed, with no warning.** The queue already shows PRINT PENDING, and a printer fault must never stop production (PD-02).
- **The preview shows the ticket without the REPRINT line.** The Api returns the frozen lines, and the mark is added when printing. The preview says the next copy will be marked, and the Reprint label says the same.
- **F1 stays enabled while the printer is offline.** The server decides, and the operator gets the banner. This matches the OrgaTEX Retry and avoids acting on a stale status strip.
- **Batches completed before bolt 012** show no preview and no Print key, and their summary still works.

### Dependencies

- Bolts 010, 011 and 012 (all complete).
- **New package: `jsbarcode` 3.12.3 (MIT)**, already named in the tech-stack standard. Pinned exactly. Its bundled TypeScript types are checked at install.

### Acceptance criteria (from the stories)

- [ ] After completion the summary rows and per-chemical lines are shown, including Written to OrgaTEX, Pending sync or Sync failed.
- [ ] Next batch (F3) opens the queue.
- [ ] "Sync failed" shows Retry.
- [ ] The preview shows the same content as the printed ticket, including the barcode.
- [ ] Print (F1) prints; later prints show REPRINT.
- [ ] Print pending shows in the preview.
- [ ] The review list shows completed batches with time, operator and override count.
- [ ] Opening one shows its summary and preview read-only (per D1).
- [ ] All text goes through Transloco, status is never shown by colour alone, and build, lint and all earlier tests pass.

### How this will be tested (Stage 3)

1. **Vitest component tests**: each write-back and print state; F1 and F3; 503 and 409 keeping the screen intact; the count sent on a press; preview lines unchanged from the Api; barcode drawn with the batch ID; the review list and its paging; no action keys in read-only mode.
2. **Backend**: the new `operators` field, with one and two operators.
3. **Live run with a real browser** against the Api, simulators and dummy printer: complete a batch, print, reprint, printer fault and recovery, then review. Check the stored ticket matches the preview.
4. **Evidence**: `STEP-27-ticket.png`, plus `STEP-28-ui-validation/screen-3-comparison.png`.

### Limits to expect

- The barcode is drawn correctly as far as tests can check. Nobody will have read it with a physical scanner.
- `npx playwright test` and the rest of STEP 28, 29 and 30 belong to bolt 014.
- Opening the summary by direct URL still leaves the header without the batch line (known from bolt 011). The review route will show the same gap unless it turns out to be trivial to fix; otherwise bolt 014 takes it, with the other error and offline states.

### Out of scope

- Search and export on the review list; editing a ticket; configurable layouts (story 012, story 010, PD-04).
- The queue's OrgaTEX-outage behaviour and other error states (story 011, bolt 014).
