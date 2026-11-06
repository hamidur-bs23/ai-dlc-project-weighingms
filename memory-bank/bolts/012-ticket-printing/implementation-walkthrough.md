---
stage: implement
bolt: 012-ticket-printing
created: 2026-11-06T04:31:00Z
---

## Implementation Walkthrough: ticket-printing (bolt 012)

### Summary

Every completed batch now gets a frozen ticket, composed in the completing transaction and held as print pending. An operator's press sends it to the dummy printer with an idempotency key; a later press is marked REPRINT and audited. A printer fault leaves completion and the ticket untouched. The state shows on the summary and the queue, and the preview and Print key follow in bolt 013.

### Structure Overview

The Domain owns the layout rules, the ticket's state and the print keys. The Application layer holds the completion handler (composes, never prints) and the service that reads and prints a ticket, behind the new `IPrinter` port. Infrastructure provides persistence and the HTTP client. The Api exposes the ticket and print endpoints.

### Completed Work

- [x] `src/WeighingMS.Domain/Tickets/TicketComposer.cs` - the fixed V1 layout as text lines, and the REPRINT mark
- [x] `src/WeighingMS.Domain/Tickets/Ticket.cs` - the frozen ticket, its status, print count and print keys
- [x] `src/WeighingMS.Domain/Tickets/PrintJob.cs` - one print attempt, successful or not
- [x] `src/WeighingMS.Domain/Tickets/TicketStatus.cs` - the status values
- [x] `src/WeighingMS.Domain/Audit/AuditEventType.cs` - adds the print and reprint audit events
- [x] `src/WeighingMS.Application/Tickets/TicketCompletedHandler.cs` - composes the ticket in the completing unit of work
- [x] `src/WeighingMS.Application/Tickets/TicketService.cs` - reads and prints a ticket
- [x] `src/WeighingMS.Application/Tickets/IPrinter.cs`, `ITicketRepository.cs` - the ports and the printer error
- [x] `src/WeighingMS.Application/Batches/BatchService.cs`, `BatchStatus.cs` - the print pending queue status
- [x] `src/WeighingMS.Application/Weighing/BatchSummary.cs`, `WeighingSessionService.cs` - the summary carries the ticket
- [x] `src/WeighingMS.Infrastructure/Integrations/Printer/PrinterClient.cs` - the HTTP print call
- [x] `src/WeighingMS.Infrastructure/Tickets/TicketRepository.cs` - persistence with `xmin` concurrency
- [x] `src/WeighingMS.Infrastructure/Persistence/Configurations/TicketConfiguration.cs`, `PrintJobConfiguration.cs` - the table mappings
- [x] `src/WeighingMS.Infrastructure/Persistence/Migrations/20261106042726_AddTickets.cs` - the `tickets` and `print_jobs` tables
- [x] `src/WeighingMS.Api/Controllers/TicketController.cs`, `Contracts/Weighing/TicketContracts.cs` - the ticket and print endpoints
- [x] `src/WeighingMS.Api/openapi/WeighingMS.Api.json`, `web/src/app/core/api/generated/schema.ts` - the regenerated contract and types
- [x] `web/src/app/features/batch-summary/batch-summary.component.ts` - the ticket state on the summary
- [x] `web/src/app/features/batch-queue/batch-queue.component.html`, `web/src/app/core/api/api.types.ts`, `web/public/i18n/en.json` - the PRINT PENDING tag, types and text
- [x] `memory-bank/standards/decision-index.md`, `CLAUDE.md` - the decisions, endpoints and rules
- [x] Existing test doubles and expectations - updated to the new constructors and states (no new tests; those are Stage 3)

### Key Decisions

- **D1 to D3 applied**: printing is always manual, the ticket lists all operators, and full chemical names keep their own line (TICKET-PRINT-MANUAL, TICKET-OPERATORS, TICKET-LINES).
- **The ticket is frozen at completion**: later changes to a user's name or anything else can't alter a printed record or a reprint.
- **The press carries the print count it saw**: a stale press is refused with no time window to tune.

### Deviations from Plan

- **Print keys**: the plan assumed a new key per reprint press. Each key is now derived from the ticket's fixed key and the print number, so concurrent presses and retries after a lost response print one copy, on reprints too. The plan's accepted risk of an extra copy no longer applies.
- **Units**: target and actual each show their unit, rather than one unit at the end of the line, so the two can never be misread if their units differ.
- **Summary state**: the ticket state on the summary is delivered as planned; no Print key was added, as bolt 013 owns it.

### Dependencies Added

None.

### Developer Notes

- Any non-success answer from the printer, including a 4xx, maps to `Printer.Unavailable`, through the shared integration call. The dummy only returns 400 for a malformed request, which would be our own defect.
- Batches completed before this bolt have no ticket: their summary shows "—", and the queue shows COMPLETE once written to OrgaTEX.
- The first print and reprint paths have not yet run against the dummy printer. Stage 3 and the live run must exercise them.
