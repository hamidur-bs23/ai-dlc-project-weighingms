---
stage: plan
bolt: 012-ticket-printing
created: 2026-11-06T04:25:00Z
---

## Implementation Plan: ticket-printing (bolt 012)

### Objective

Every completed batch gets a frozen 80 mm ticket, composed in the completing transaction. The ticket is sent to the dummy printer without a printer fault ever blocking completion (PD-02, PD-04). Later prints are marked REPRINT and audited.

Workbook **STEP 27**: Batch Complete → Generate Ticket → Dummy Printer → Print / Print Preview. The printer-down part of **STEP 30** is covered too.

### Stories in scope

1. `001-compose-ticket`: FR-21, FR-22
2. `002-print-ticket`: FR-21
3. `003-reprint-marked`: FR-21
4. `004-printer-unavailable-print-pending`: FR-24, FR-21

### What will change

1. **Domain** (`Domain/Tickets`):
   - `Ticket` (aggregate): batch, frozen text lines, barcode value, status `PrintPending | Printed`, print count, and a first-print idempotency key that never changes.
   - `PrintJob`: is-reprint, key, operator, outcome, time.
   - `TicketComposer`: pure layout rules (field order, OVR marks, the result line, the width limit).
2. **Application** (`Application/Tickets`):
   - `TicketCompletedHandler : IBatchCompletedHandler`: composes and adds the ticket to the completing unit of work. It never calls the printer.
   - `TicketService`: read a ticket, and print (first print or reprint).
   - New port `IPrinter`, and new audit events `TicketPrinted` and `TicketReprinted`.
3. **Infrastructure**:
   - Tables `tickets` and `print_jobs`, migration `AddTickets`.
   - `PrinterClient : IPrinter` for `POST /api/tickets` with `Idempotency-Key`. 5xx, timeout and unreachable map to `Printer.Unavailable`.
4. **Api**:
   - `GET /api/batches/{batchId}/session/ticket`.
   - `POST /api/batches/{batchId}/session/ticket/print`.
   - The summary's `ticket.state` becomes real: `printPending` or `printed` with the print count.
   - Queue status gains `printPending`.
5. **Web** (minimal; bolt 013 owns the preview and the Print key):
   - Regenerated types, the summary's ticket state text, and the queue's PRINT PENDING tag.

### Ticket layout (invented for the lab; printer model and label unconfirmed, Q18, A-04)

Text lines at 42 columns, showing decision D2 option 1 and D3 option 1:

```text
FCI DYEING - WEIGHING TICKET
BATCH    B-24187
ORDER    SO-9931
MACHINE  JET-04
SHADE    Teal 4C
DATE     2026-11-06 10:02
OPER.    R. Karim
------------------------------------------
COMPONENT                     TGT       ACT
Remazol Brill. Blue RSPL
                           1240.0    1241.5 g
Remazol Yellow RGB
                         OVR 94.0      96.4 g
------------------------------------------
OVERRIDES: 1
```

- The barcode is sent separately as `{ symbology: Code128, value: B-24187 }`; the batch ID beneath it is the barcode's human-readable text.
- Without overrides the result line reads `ALL WITHIN TOLERANCE`.
- A reprint adds `*** REPRINT ***` as its first line and sets `isReprint: true`.

### Files to be created / modified

- **Created**:
  - `src/WeighingMS.Domain/Tickets/{Ticket,PrintJob,TicketComposer,TicketStatus}.cs`
  - `src/WeighingMS.Application/Tickets/{TicketCompletedHandler,TicketService,IPrinter,ITicketRepository}.cs`
  - `src/WeighingMS.Infrastructure/Tickets/TicketRepository.cs`, `Integrations/Printer/PrinterClient.cs`
  - `Persistence/Configurations/{Ticket,PrintJob}Configuration.cs`, migration `*_AddTickets`
  - `src/WeighingMS.Api/Controllers/TicketController.cs`, contracts
  - tests (Stage 3)
- **Modified**:
  - `AuditEventType`, `BatchService` / `BatchStatus`, `BatchSummary`, `WeighingSessionService`, `WeighingSessionController`
  - `IntegrationsRegistration`, DI, `WeighingDbContext`, OpenAPI
  - web: `api.types`, generated schema, `batch-summary`, `batch-queue`, `en.json`
  - `CLAUDE.md`

### APIs involved

| Call | Direction |
|---|---|
| `POST /api/tickets` + `Idempotency-Key`, body `{ batchId, isReprint, lines, barcode }` → 201 receipt; a repeated key returns the first receipt (dummy printer) | Api → simulator |
| `GET …/session/ticket` → lines, barcode, state, print count · 404 `Ticket.NotFound` | UI → Api |
| `POST …/session/ticket/print` `{ expectedPrintCount }` → 200 · 409 `Ticket.AlreadyPrinted` · 503 `Printer.Unavailable` · 404 | UI → Api |
| `GET …/session/summary` → `ticket: { state, printCount }` | UI → Api |
| `GET /api/batches` → status `printPending` | UI → Api |

### Decisions at this checkpoint: resolved (APPROVE PLAN, 2026-11-06)

- **D1: option 1.** Always manual (F1); no automatic print (TICKET-PRINT-MANUAL). FR-21's "send it to the printer" is read together with PD-02 and FR-24.
- **D2: option 1.** All operators, in the order they first weighed (TICKET-OPERATORS).
- **D3: option 1.** Full chemical name on its own line, target and actual beneath (TICKET-LINES).

#### Options as presented

**D1. Does the first print happen automatically?** The sources disagree: FR-21 says "on completion… send it to the printer", while PD-02, FR-24 and stories 002 and 004 say the operator prints with F1 and there is no auto-print.

1 - **(Recommended)** **Always manual (F1).** The ticket is composed at completion and stays print pending until the operator presses Print, whether or not the printer is up. This follows the resolved decision PD-02 and the mockup's "Print ticket F1". FR-21's wording gets a note.
2 - Try once automatically at completion, with F1 as the fallback. Closer to FR-21's wording, but it prints with no operator at the printer and adds a second path to test.

**D2. Whose name goes on OPER. when several operators weighed a batch?** The mockup has one name; the summary already lists all of them.

1 - **(Recommended)** **All operators**, in the order they first weighed, wrapping if needed. Consistent with the summary and with FR-19 traceability.
2 - Only the operator who completed the batch. Shorter, but it hides who weighed most of it.

**D3. How do long chemical names fit on 80 mm?** The mockup invents short names ("Na2SO4", "Blue RSPL") that exist nowhere in the data.

1 - **(Recommended)** **Full name on its own line, target and actual beneath it.** Longer ticket, but a chemical's name is never altered on a physical record.
2 - One line per component, with the name truncated. Compact like the mockup, but two similar names could become hard to tell apart.

### Assumptions (say if any is wrong)

- **Who may print**: any signed-in operator, first print or reprint, always audited. Story 003 places further authorisation out of scope.
- **Double press**: the client sends the print count it last saw. A stale press gets 409 and prints nothing, so a double press can't produce a surprise REPRINT.
- **Queue tag precedence**: one tag per row, in the order SYNC FAILED, SYNC PENDING, PRINT PENDING, COMPLETE. COMPLETE then means written to OrgaTEX and printed.
- **Lost response on a first print**: retrying reuses the ticket's fixed key, so it can't print twice. A reprint uses a new key per press, so a lost response could produce one extra copy, which is already marked REPRINT.
- **Batches completed before this bolt** have no ticket, and their summary shows the state as not available.

### Dependencies

- Bolt 009 (`IBatchCompletedHandler`, records, summary), bolt 002 (dummy printer, `/_sim/faults printer`, `GET /_sim/printer/tickets`), bolt 003 (plant time, audit), bolt 004 (printer health on the strip). All complete.
- No new packages.

### Acceptance criteria (from the stories)

- [ ] A completed batch has a ticket in this order: batch, order, machine, shade, date/time (plant zone), operator; each component's target and actual with unit, OVR on overridden lines; "ALL WITHIN TOLERANCE" or "OVERRIDES: n"; a Code 128 barcode of the batch ID.
- [ ] The same content is available for the on-screen preview.
- [ ] READY printer + Print → sent with an `Idempotency-Key`, marked printed, and `GET /_sim/printer/tickets` shows it once; a double press gives one print.
- [ ] A later print is marked REPRINT and audited.
- [ ] Printer faulted → the batch still completes, the ticket is print pending, the strip shows PRINTER OFFLINE; after recovery Print sends it, with no auto-print.
- [ ] Build 0 warnings, format clean, all earlier tests pass, the migration applies.

### How this will be tested (Stage 3)

1. **Domain**: composition (field order, OVR, result line, barcode, wrapping, REPRINT line) and the ticket state rules.
2. **Application**, with fakes: handler in the completing unit of work, print outcomes, stale press, audit.
3. **Api integration** (Testcontainers + the in-process simulator): first print plain and second REPRINT; exactly one receipt on double press; `/_sim/faults printer` leaves completion intact and print pending; recovery then Print; a failing handler rolls back; no auto-print after recovery.
4. **Web**: the summary state text and the queue tag.
5. **STEP 27 evidence**: live capture of the printer API and test output. `STEP-27-ticket.png` needs the preview from bolt 013, so it is taken there.

### Out of scope

- The ticket preview, Print and Reprint keys, and Next batch (bolt 013).
- Configurable templates (deviation from brief IR-16, PD-04); auto-print on recovery.
