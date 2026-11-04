---
phase: inception
updated: 2026-11-04T16:00:00Z
---

# Global Story Index

Intent `001-assisted-weighing-station`: **56 stories** across 8 units (must 55, should 1). Short form `UUU/SSS` = unit / story number.

## Intent: 001-assisted-weighing-station

### Unit: 001-station-simulators

#### 001-dummy-orgatex-serves-batches-and-recipes.md ✅ GENERATED
**Title**: Dummy OrgaTEX serves the batch queue and recipes from seed  
**Requirements**: FR-25, FR-1, FR-3 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/001-station-simulators/stories/001-dummy-orgatex-serves-batches-and-recipes.md`

#### 002-dummy-orgatex-receives-consumption.md ✅ GENERATED
**Title**: Dummy OrgaTEX receives and exposes consumption  
**Requirements**: FR-25, FR-20 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/001-station-simulators/stories/002-dummy-orgatex-receives-consumption.md`

#### 003-dummy-scales-with-scenarios.md ✅ GENERATED
**Title**: Two dummy scales with weighing scenarios and resolution  
**Requirements**: FR-26, FR-9, FR-7 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/001-station-simulators/stories/003-dummy-scales-with-scenarios.md`

#### 004-dummy-scanner-emits-scans.md ✅ GENERATED
**Title**: Dummy scanner emits scans  
**Requirements**: FR-27 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/001-station-simulators/stories/004-dummy-scanner-emits-scans.md`

#### 005-dummy-printer-accepts-tickets.md ✅ GENERATED
**Title**: Dummy printer accepts and exposes tickets  
**Requirements**: FR-28, FR-21 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/001-station-simulators/stories/005-dummy-printer-accepts-tickets.md`

#### 006-fault-injection-and-seed-reset.md ✅ GENERATED
**Title**: Fault injection and seed reset  
**Requirements**: FR-25, FR-26, FR-27, FR-28, FR-23, FR-24 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/001-station-simulators/stories/006-fault-injection-and-seed-reset.md`

### Unit: 002-station-access

#### 001-operator-badge-sign-in.md ✅ GENERATED
**Title**: Operator badge sign-in and session  
**Requirements**: FR-19, NFR-Security · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/002-station-access/stories/001-operator-badge-sign-in.md`

#### 002-supervisor-pin-step-up.md ✅ GENERATED
**Title**: Supervisor badge + PIN step-up for one action  
**Requirements**: FR-15, NFR-Security · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/002-station-access/stories/002-supervisor-pin-step-up.md`

#### 003-immutable-audit-trail.md ✅ GENERATED
**Title**: Immutable audit trail  
**Requirements**: NFR-Security, FR-14, FR-15 · **Priority**: should  
**Path**: `intents/001-assisted-weighing-station/units/002-station-access/stories/003-immutable-audit-trail.md`

#### 004-plant-time-zone.md ✅ GENERATED
**Title**: Plant time zone for display and print  
**Requirements**: FR-22 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/002-station-access/stories/004-plant-time-zone.md`

### Unit: 003-batch-recipe

#### 001-view-batch-queue.md ✅ GENERATED
**Title**: View the OrgaTEX batch queue  
**Requirements**: FR-1 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/003-batch-recipe/stories/001-view-batch-queue.md`

#### 002-select-batch-by-touch-or-scan.md ✅ GENERATED
**Title**: Select a batch by touch or scanned batch ID  
**Requirements**: FR-2, FR-27 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/003-batch-recipe/stories/002-select-batch-by-touch-or-scan.md`

#### 003-load-recipe-snapshot.md ✅ GENERATED
**Title**: Load an immutable recipe snapshot  
**Requirements**: FR-3 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/003-batch-recipe/stories/003-load-recipe-snapshot.md`

#### 004-configured-tolerance-fallback.md ✅ GENERATED
**Title**: Configured tolerance per chemical + scale  
**Requirements**: FR-12 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/003-batch-recipe/stories/004-configured-tolerance-fallback.md`

#### 005-orgatex-unavailable-on-load.md ✅ GENERATED
**Title**: OrgaTEX unavailable while loading: error + Retry  
**Requirements**: FR-24 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/003-batch-recipe/stories/005-orgatex-unavailable-on-load.md`

### Unit: 004-device-integration

#### 001-live-scale-reading.md ✅ GENERATED
**Title**: Live scale reading at ≤ 250 ms  
**Requirements**: FR-9 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/004-device-integration/stories/001-live-scale-reading.md`

#### 002-tare-a-scale.md ✅ GENERATED
**Title**: Tare a scale  
**Requirements**: FR-7 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/004-device-integration/stories/002-tare-a-scale.md`

#### 003-scale-offline-and-invalid-readings.md ✅ GENERATED
**Title**: Scale offline and invalid readings handled safely  
**Requirements**: FR-23 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/004-device-integration/stories/003-scale-offline-and-invalid-readings.md`

#### 004-scan-delivery-to-station.md ✅ GENERATED
**Title**: Scans delivered from the scanner to the station  
**Requirements**: FR-27, FR-2, FR-5 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/004-device-integration/stories/004-scan-delivery-to-station.md`

#### 005-scanner-offline-manual-fallback.md ✅ GENERATED
**Title**: Scanner offline: manual fallback  
**Requirements**: FR-24 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/004-device-integration/stories/005-scanner-offline-manual-fallback.md`

#### 006-device-health-for-status-strip.md ✅ GENERATED
**Title**: Device health for the status strip  
**Requirements**: FR-23, FR-24 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/004-device-integration/stories/006-device-health-for-status-strip.md`

### Unit: 005-weighing-station

#### 001-start-or-resume-weighing-session.md ✅ GENERATED
**Title**: Start or resume a weighing session for a batch  
**Requirements**: FR-4, FR-17, NFR-Reliability · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/001-start-or-resume-weighing-session.md`

#### 002-select-pending-component.md ✅ GENERATED
**Title**: Select any pending component, sequence suggested  
**Requirements**: FR-4, FR-17 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/002-select-pending-component.md`

#### 003-automatic-scale-selection.md ✅ GENERATED
**Title**: Automatic scale selection by the ≤ 3 kg threshold  
**Requirements**: FR-8 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/003-automatic-scale-selection.md`

#### 004-capture-lot-and-container.md ✅ GENERATED
**Title**: Capture lot and container for the component  
**Requirements**: FR-5, FR-6 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/004-capture-lot-and-container.md`

#### 005-tare-required-and-re-tare.md ✅ GENERATED
**Title**: Tare required before weighing; re-tare  
**Requirements**: FR-7 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/005-tare-required-and-re-tare.md`

#### 006-classify-reading-four-classes.md ✅ GENERATED
**Title**: Classify each reading into 4 classes, with remaining  
**Requirements**: FR-9, FR-10, FR-11, FR-12 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/006-classify-reading-four-classes.md`

#### 007-resolve-tolerance-source.md ✅ GENERATED
**Title**: Resolve the tolerance source (recipe → config → blocked)  
**Requirements**: FR-12 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/007-resolve-tolerance-source.md`

#### 008-confirm-valid-weighing.md ✅ GENERATED
**Title**: Confirm a valid weighing  
**Requirements**: FR-13, FR-19 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/008-confirm-valid-weighing.md`

#### 009-progress-to-next-component.md ✅ GENERATED
**Title**: Progress to the next pending component  
**Requirements**: FR-17 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/009-progress-to-next-component.md`

#### 010-overshoot-warn-and-block.md ✅ GENERATED
**Title**: Overshoot: warn and block (no compensation)  
**Requirements**: FR-16 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/010-overshoot-warn-and-block.md`

#### 011-supervisor-override.md ✅ GENERATED
**Title**: Supervisor override of an out-of-tolerance weighing  
**Requirements**: FR-15 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/011-supervisor-override.md`

#### 012-quit-and-incomplete-batch.md ✅ GENERATED
**Title**: Quit, leaving the batch incomplete  
**Requirements**: FR-14 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/012-quit-and-incomplete-batch.md`

#### 013-complete-batch-all-confirmed.md ✅ GENERATED
**Title**: Complete the batch when all N are confirmed  
**Requirements**: FR-18 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/013-complete-batch-all-confirmed.md`

#### 014-weighing-records-and-review.md ✅ GENERATED
**Title**: Weighing records with traceability, and completed-batch review  
**Requirements**: FR-19, FR-18 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/005-weighing-station/stories/014-weighing-records-and-review.md`

### Unit: 006-consumption-writeback

#### 001-enqueue-consumption-on-completion.md ✅ GENERATED
**Title**: Enqueue consumption atomically with completion  
**Requirements**: FR-20 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/006-consumption-writeback/stories/001-enqueue-consumption-on-completion.md`

#### 002-dispatch-with-idempotency-and-retry.md ✅ GENERATED
**Title**: Dispatch with idempotency and bounded retry  
**Requirements**: FR-20 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/006-consumption-writeback/stories/002-dispatch-with-idempotency-and-retry.md`

#### 003-sync-failed-and-manual-retry.md ✅ GENERATED
**Title**: Sync failed and manual retry  
**Requirements**: FR-20, FR-24 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/006-consumption-writeback/stories/003-sync-failed-and-manual-retry.md`

#### 004-write-back-status-visible.md ✅ GENERATED
**Title**: Write-back status and unsynced count visible  
**Requirements**: FR-20, FR-24 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/006-consumption-writeback/stories/004-write-back-status-visible.md`

### Unit: 007-ticket-printing

#### 001-compose-ticket.md ✅ GENERATED
**Title**: Compose the 80 mm ticket (STEP 27 fields, Code 128, OVR)  
**Requirements**: FR-21, FR-22 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/007-ticket-printing/stories/001-compose-ticket.md`

#### 002-print-ticket.md ✅ GENERATED
**Title**: Print the ticket  
**Requirements**: FR-21 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/007-ticket-printing/stories/002-print-ticket.md`

#### 003-reprint-marked.md ✅ GENERATED
**Title**: Reprints marked REPRINT  
**Requirements**: FR-21 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/007-ticket-printing/stories/003-reprint-marked.md`

#### 004-printer-unavailable-print-pending.md ✅ GENERATED
**Title**: Printer unavailable: print pending  
**Requirements**: FR-24, FR-21 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/007-ticket-printing/stories/004-printer-unavailable-print-pending.md`

### Unit: 008-assisted-weighing-station-ui

#### 001-app-shell-top-bar-status-strip.md ✅ GENERATED
**Title**: App shell: top bar, status strip, clock, Transloco, function keys  
**Requirements**: FR-22, NFR-Usability · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/001-app-shell-top-bar-status-strip.md`

#### 002-sign-in-screen.md ✅ GENERATED
**Title**: Sign-in screen  
**Requirements**: FR-19, NFR-Security · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/002-sign-in-screen.md`

#### 003-select-batch-screen.md ✅ GENERATED
**Title**: Select-batch screen (scan prompt + queue table)  
**Requirements**: FR-1, FR-2 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/003-select-batch-screen.md`

#### 004-weigh-screen-rail-and-header.md ✅ GENERATED
**Title**: Weigh screen: recipe rail and component header  
**Requirements**: FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-17 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/004-weigh-screen-rail-and-header.md`

#### 005-weigh-screen-readout-bar-status.md ✅ GENERATED
**Title**: Weigh screen: readout, progress bar with band and target line, status line  
**Requirements**: FR-9, FR-10, FR-11, FR-12, FR-16 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/005-weigh-screen-readout-bar-status.md`

#### 006-weigh-actions-and-keys.md ✅ GENERATED
**Title**: Weigh actions and keys: Confirm F1, Re-tare F2, Quit F4, Supervisor override  
**Requirements**: FR-7, FR-13, FR-14, FR-15 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/006-weigh-actions-and-keys.md`

#### 007-lot-and-container-entry.md ✅ GENERATED
**Title**: Lot and container entry  
**Requirements**: FR-5, FR-6 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/007-lot-and-container-entry.md`

#### 008-supervisor-override-dialog.md ✅ GENERATED
**Title**: Supervisor override dialog  
**Requirements**: FR-15 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/008-supervisor-override-dialog.md`

#### 009-confirm-and-print-screen.md ✅ GENERATED
**Title**: Confirm & print screen: summary, write-back status, Next batch F3  
**Requirements**: FR-18, FR-20 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/009-confirm-and-print-screen.md`

#### 010-ticket-preview-and-print.md ✅ GENERATED
**Title**: Ticket preview and print / reprint  
**Requirements**: FR-21 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/010-ticket-preview-and-print.md`

#### 011-error-and-offline-states.md ✅ GENERATED
**Title**: Error and offline states across screens  
**Requirements**: FR-23, FR-24 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/011-error-and-offline-states.md`

#### 012-completed-batch-review-list.md ✅ GENERATED
**Title**: Completed-batch review list  
**Requirements**: FR-18, FR-19 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/012-completed-batch-review-list.md`

#### 013-end-to-end-batch-flow.md ✅ GENERATED
**Title**: End-to-end batch B-24187 (Playwright)  
**Requirements**: FR-1, FR-13, FR-18, FR-20, FR-21 · **Priority**: must  
**Path**: `intents/001-assisted-weighing-station/units/008-assisted-weighing-station-ui/stories/013-end-to-end-batch-flow.md`

## Requirement coverage (FR → stories)

| FR | Stories |
|---|---|
| FR-1 | 001/001, 003/001, 008/003, 008/013 |
| FR-2 | 003/002, 004/004, 008/003 |
| FR-3 | 001/001, 003/003, 008/004 |
| FR-4 | 005/001, 005/002, 008/004 |
| FR-5 | 004/004, 005/004, 008/004, 008/007 |
| FR-6 | 005/004, 008/004, 008/007 |
| FR-7 | 001/003, 004/002, 005/005, 008/004, 008/006 |
| FR-8 | 005/003, 008/004 |
| FR-9 | 001/003, 004/001, 005/006, 008/005 |
| FR-10 | 005/006, 008/005 |
| FR-11 | 005/006, 008/005 |
| FR-12 | 003/004, 005/006, 005/007, 008/005 |
| FR-13 | 005/008, 008/006, 008/013 |
| FR-14 | 002/003, 005/012, 008/006 |
| FR-15 | 002/002, 002/003, 005/011, 008/006, 008/008 |
| FR-16 | 005/010, 008/005 |
| FR-17 | 005/001, 005/002, 005/009, 008/004 |
| FR-18 | 005/013, 005/014, 008/009, 008/012, 008/013 |
| FR-19 | 002/001, 005/008, 005/014, 008/002, 008/012 |
| FR-20 | 001/002, 006/001, 006/002, 006/003, 006/004, 008/009, 008/013 |
| FR-21 | 001/005, 007/001, 007/002, 007/003, 007/004, 008/010, 008/013 |
| FR-22 | 002/004, 007/001, 008/001 |
| FR-23 | 001/006, 004/003, 004/006, 008/011 |
| FR-24 | 001/006, 003/005, 004/005, 004/006, 006/003, 006/004, 007/004, 008/011 |
| FR-25 | 001/001, 001/002, 001/006 |
| FR-26 | 001/003, 001/006 |
| FR-27 | 001/004, 001/006, 003/002, 004/004 |
| FR-28 | 001/005, 001/006 |

## Mockup UI/UX coverage (`docs/customize-weighing-station.html`)

| Mockup element | Lines | Stories |
|---|---|---|
| Top bar: brand, station, batch crumb, operator chip | L440–445 | 008/001, 002/001 |
| View 1: scan prompt | L450–455 | 008/003, 003/002, 004/004 |
| View 1: queue table and status tags | L458–484 | 008/003, 003/001 |
| View 2: recipe rail and counter | L494–499, L696–716 | 008/004, 005/002, 005/009 |
| View 2: header (name, code, lot, container, tare) | L503–507 | 008/004, 008/007, 005/004, 005/005 |
| View 2: scale badge "automatic" | L508–512 | 008/004, 005/003 |
| View 2: readout (now / still to weigh / target) | L515–528 | 008/005, 005/006, 004/001 |
| View 2: progress bar, tolerance band, target line, caption | L530–540 | 008/005, 005/006 |
| View 2: status line per zone | L541, L768–776 | 008/005, 005/006, 005/010 |
| View 2: surplus panel (excess only; recalculation not built, PD-05) | L544–554 | 008/005, 005/010 |
| View 2: OK · Confirm F1, Re-tare F2, Quit F4 | L556–559 | 008/006, 005/005, 005/008, 005/012 |
| View 2: Supervisor override | L560, L824–826 | 008/006, 008/008, 005/011, 002/002 |
| View 3: completion summary | L570–578 | 008/009, 005/013 |
| View 3: write-back note "Written to OrgaTEX" | L577–582 | 008/009, 006/004 |
| View 3: Print ticket F1, Next batch F3 | L583–586 | 008/009, 008/010, 007/002 |
| View 3: ticket preview (fields, lines, result, barcode) | L590–613 | 008/010, 007/001, 007/003 |
| Status strip: scales, scanner, printer, OrgaTEX | L618–623 | 008/001, 004/006, 006/004, 008/011 |
| Status strip: language EN, clock | L625–626 | 008/001, 002/004 |
| *Added from ui-analysis gaps*: sign-in, lot/container entry, override dialog, Target achieved state, error/offline states, review list, F-key handlers | — | 008/002, 008/007, 008/008, 008/005, 008/011, 008/012, 008/001 |
| *Demo-only, not built*: Simulate dispense / overshoot / reset, screen switcher, question pins, notes | L422–434, L633–670 | Replaced by `/_sim/*`: 001/003, 001/006 |
