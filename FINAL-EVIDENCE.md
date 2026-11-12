# FINAL-EVIDENCE: WeighingMS (workbook STEP 34)

Every row maps one workbook requirement to the UI route, the Api endpoint, the code, the tests and the evidence files that prove it. A ✅ means the tests in the row passed in this run, and every evidence file it names exists in `evidence/`. A cross-check script verified both.

## This run

| Item | Value |
|---|---|
| Date | 2026-11-13 |
| Commit | `7456aca` (branch `bolt/014-assisted-weighing-station-ui`) plus the evidence captures below |
| Backend tests | `dotnet test --solution WeighingMS.sln`: **717 passed, 0 failed** (Domain, Application, Api integration with Testcontainers PostgreSQL, Simulators, Architecture) |
| Web unit tests | `npm run test:web` (Vitest): **26 files, 205 tests passed** |
| E2E tests | `npm run e2e` (Playwright, packaged app on Compose `full`, isolated project): **15 passed, 0 failed** (smoke 2, B-24187 end to end 1, failure scenarios 12) |
| AI-DLC state | 1 intent, 8 units, 56 stories and 14 bolts, all `complete`. `specsmd:integrity`: 0 inconsistencies. `specsmd:validate`: 0 issues |

The dummy OrgaTEX, scales, scanner and printer (`WeighingMS.Simulators.Host`) are training contracts. They make no claim about the real OrgaTEX interface (ADR-015, CLAUDE.md).

## Matrix

| Requirement | UI | API | Code | Test | Evidence | Result |
|---|---|---|---|---|---|---|
| Batch queue | ✓ `/batches` (Screen 1 · Select batch) | ✓ `GET /api/batches` (Api), backed by dummy OrgaTEX `GET /api/batches` | ✓ `web/src/app/features/batch-queue/`, `src/WeighingMS.Api/Controllers/BatchesController.cs`, `src/WeighingMS.Application/Batches/BatchService.cs`, `src/WeighingMS.Infrastructure/Integrations/OrgaTex/OrgatexGatewayClient.cs` | ✓ `BatchEndpointsTests`, `BatchServiceTests`, `BatchQueueTests` (simulator), `batch-queue.component.spec.ts`, E2E `02-batch-b24187` | `STEP-17-dummy-data.json`, `STEP-17-api-response.png`, `STEP-18-api-tests.txt`, `STEP-18-api-results.png`, `STEP-19-batch-screen.png`, `STEP-19-network.png`, `STEP-19-test.png` | ✅ |
| Batch selection | ✓ queue row → `/batches/:batchId/weigh` (scan or touch) | ✓ `GET /api/batches/{id}`, `POST /api/batches/{id}/session` (start 201 / resume 200) | ✓ `WeighingSessionController.cs`, `src/WeighingMS.Application/Weighing/WeighingSessionService.cs`, `web/src/app/core/batches/active-batch.service.ts` | ✓ `WeighingSessionEndpointsTests`, `WeighingSessionServiceTests`, `ScannerTests`, `scanner.service.spec.ts`, E2E `02-batch-b24187`, `03-failures` (scanner offline, touch fallback) | `STEP-19-batch-screen.png`, `STEP-19-api-flow.txt`, `STEP-19-web-test.txt`, `STEP-20-weigh-screen.png` | ✅ |
| Recipe | ✓ recipe rail on `/batches/:batchId/weigh` (five components, own unit and scale) | ✓ `GET /api/batches/{id}/recipe`, backed by dummy OrgaTEX `GET /api/batches/{id}/recipe` | ✓ `src/WeighingMS.Domain/Batches/RecipeSnapshot.cs`, `web/src/app/features/weigh/recipe-rail.component.ts` | ✓ `RecipeSnapshotTests`, `RecipeTests` (simulator), `BatchEndpointsTests`, `recipe-rail.component.spec.ts` | `STEP-17-dummy-data.json`, `STEP-20-weigh-screen.png`, `STEP-23-five-component-demo.mp4` | ✅ |
| Scale selection | ✓ "Scale selected · automatic" badge (3 kg precision / 32 kg floor) | ✓ `GET /api/scales`, `GET /api/scales/{id}/status`, `GET /api/batches/{id}/session/reading` (the active component's own scale) | ✓ `src/WeighingMS.Domain/Weighing/ScaleSelector.cs`, `ScaleSelectionPolicy.cs`, `src/WeighingMS.Application/Weighing/ScaleSelectionOptions.cs` (`Weighing:ScaleSelection`, ≤ 3 kg inclusive → precision) | ✓ `ScaleSelectorTests`, `ScaleTests` (simulator), E2E `03-failures` (exactly 3.0 kg → precision) | `STEP-21-scale-switch.png`, `STEP-21-scale-api.png`, `STEP-21-scale-api.txt` | ✅ |
| Live weight | ✓ "Weighed now / Still to weigh / Target", progress bar, UNSTABLE flag | ✓ `GET /api/batches/{id}/session/reading` (classified), backed by dummy scale `GET /api/scales/{id}/weight` | ✓ `src/WeighingMS.Infrastructure/Integrations/Scales/ScaleDeviceClient.cs`, `src/WeighingMS.Domain/Weighing/MeasuredReading.cs`, `web/src/app/features/weigh/weigh-bar.component.ts`, `weighing-session.store.ts` | ✓ `ScaleReadingRulesTests`, `ScaleScenarioTests` (rising, belowRange, enterRange, overshoot, stable), `weigh-bar.component.spec.ts`, `weighing-session.store.spec.ts` | `STEP-20-weigh-screen.png`, `STEP-23-five-component-demo.mp4` | ✅ |
| Tolerance | ✓ tolerance band and status line (DISPENSE / WITHIN / TARGET ACHIEVED / OVER) | — | ✓ `src/WeighingMS.Domain/Weighing/ReadingClassifier.cs`, `ToleranceBand.cs`, `ToleranceResolver.cs`, `ConfirmationRules.cs` (inclusive band) | ✓ `ReadingClassifierTests`, `ToleranceResolverTests`, `ToleranceSettingsTests`, `ToleranceCatalogueTests`, `weigh.component.spec.ts`, E2E `03-failures` (under, over, invalid) | `STEP-22-tolerance-ui.png` (93.4 / 93.5 / 94.0 / 94.5 / 94.6 g), `STEP-22-tolerance-tests.txt`, `STEP-22-tolerance-ui-tests.txt` | ✅ |
| Lot tracking | ✓ "Lot (scan or type)" field, shown as `LOT …` in the header | ✓ `POST /api/batches/{id}/session/attempt/lot`, `GET /api/scanner/scans?after=` | ✓ `src/WeighingMS.Domain/Weighing/WeighingAttempt.cs`, `WeighingRecord.cs`, `src/WeighingMS.Infrastructure/Integrations/Scanner/ScannerClient.cs` | ✓ `WeighingAttemptTests`, `WeighingConfirmationEndpointsTests`, `ConsumptionTests` (lots reach OrgaTEX), E2E `02-batch-b24187` (lots LOT-1…5 asserted at OrgaTEX) | `STEP-25-confirmation.png` (record `lot`), `STEP-25-transaction.json`, `STEP-26-data.png` | ✅ |
| Container/tare | ✓ Container field, Tare / Re-tare (F2), `TARE …` in the header | ✓ `POST …/session/attempt/container`, `POST …/session/tare`, backed by dummy scale `POST /api/scales/{id}/tare` | ✓ `src/WeighingMS.Domain/Weighing/TareTaken.cs`, `WeighingAttempt.cs`, `ScaleDeviceClient.cs` | ✓ `WeighingAttemptTests`, `WeighingConfirmationEndpointsTests`, `ScaleTests`, E2E `03-failures` (re-tare → 0.0 g) | `STEP-25-confirmation.png` (record `container`, `tare`), `STEP-30-failure-tests.md` (Re-tare row) | ✅ |
| Component progression | ✓ recipe rail `n of 5 components weighed`, ✓ per component, next component suggested | ✓ `POST …/session/selection`, `POST …/session/confirmation`, `POST …/session/completion` (refused 409 `Batch.Incomplete` until all are confirmed) | ✓ `src/WeighingMS.Domain/Weighing/WeighingSession.cs`, `NextComponentRule.cs`, `ComponentWeighing.cs` | ✓ `WeighingSessionTests`, `NextComponentRuleTests`, `CompletionTests`, E2E `02-batch-b24187`, `03-failures` (incomplete batch) | `STEP-23-five-component-demo.mp4`, `STEP-30-failure-tests.md` | ✅ |
| Overshoot | ✓ OVER TOLERANCE with SURPLUS, recovery text, Quit / Re-tare / Supervisor override, Confirm disabled (PD-05 option 1, no recalculation) | ✓ `POST …/session/quit`, `POST …/session/override` (step-up grant from another supervisor) | ✓ `src/WeighingMS.Domain/Weighing/Surplus.cs`, `OverrideDetails.cs`, `web/src/app/features/weigh/override-dialog.component.ts` | ✓ `OvershootAndOverrideTests`, `WeighingOutcomeEndpointsTests`, `StepUpTests`, `override-dialog.component.spec.ts`, E2E `03-failures` (over tolerance) | `STEP-24-overshoot-decision.md`, `STEP-24-overshoot-ui.png`, `STEP-24-overshoot-test.png`, `STEP-24-overshoot-tests.txt` | ✅ |
| OrgaTEX write-back | ✓ Screen 3 "Written to OrgaTEX · CONFIRMED", Sync failed with Retry, `n UNSYNCED` on the status strip | ✓ dummy OrgaTEX `POST /api/batches/{id}/consumption` (`Idempotency-Key`, 200 OK), `GET …/session/write-back`, `POST …/session/write-back/retry` | ✓ `src/WeighingMS.Application/WriteBack/WriteBackDispatcher.cs`, `src/WeighingMS.Domain/WriteBack/ConsumptionWriteBack.cs`, `RetryPolicy.cs`, `OrgatexGatewayClient.cs` | ✓ `ConsumptionWriteBackTests`, `WriteBackTests`, `WriteBackEndpointsTests`, `ConsumptionTests`, E2E `02-batch-b24187`, `03-failures` (OrgaTEX unavailable) | `STEP-26-ui.png`, `STEP-26-request.png`, `STEP-26-response.png`, `STEP-26-data.png`, `STEP-26-writeback.txt`, `STEP-30-orgatex-sync-failed.png`, `STEP-30-orgatex-retry-confirmed.png` | ✅ |
| Ticket | ✓ `/batches/:batchId/summary` (Screen 3 · Confirm & print): 80 mm preview with barcode, F1 Print / Reprint, Print pending | ✓ `GET …/session/ticket`, `POST …/session/ticket/print`, backed by dummy printer `POST /api/tickets` | ✓ `src/WeighingMS.Domain/Tickets/Ticket.cs`, `TicketComposer.cs`, `src/WeighingMS.Application/Tickets/TicketService.cs`, `src/WeighingMS.Infrastructure/Integrations/Printer/PrinterClient.cs` | ✓ `TicketTests` (21), `TicketServiceTests` (9), `TicketEndpointsTests` (23), `PrinterTests` (6), web ticket and summary specs (47), E2E `02-batch-b24187`, `03-failures` (printer unavailable) | `STEP-27-ticket.png`, `STEP-27-printer-api.png`, `STEP-27-ticket-test.png`, `STEP-27-ticket-tests.txt`, `STEP-30-printer-print-pending.png` | ✅ |

## Final evidence package (workbook sections 01–34)

The workbook's submission list numbers its sections differently from the steps (for example `03-context-analysis` covers STEPs 3 and 5, and `13-inception-complete` is STEP 12's checkpoint). `npm run evidence:package` builds `dist/evidence-package/01-…/` … `34-…/` and `dist/evidence-package.zip` from the files below as copies. The originals in `evidence/` are not changed. The script stops if a mapped file is missing or if any file in `evidence/` is not mapped.

| Section | Workbook | Files |
|---|---|---|
| 01-project-bootstrap | STEP 0–1 | `evidence/STEP-00-git.png`, `evidence/STEP-01-files.png` |
| 02-specsmd-installation | STEP 2 | `evidence/STEP-02-installation.png`, `evidence/STEP-02-manifest.txt` |
| 03-context-analysis | STEP 3, 5 | `evidence/STEP-03-master-agent.png`, `evidence/STEP-05-context-analysis.png` |
| 04-dashboard | STEP 4 | `evidence/STEP-04-dashboard.png` |
| 05-intent | STEP 6 | `evidence/STEP-06-intent.png`, `evidence/STEP-06-intent-file.png` |
| 06-intent-validation | STEP 6 · CHECKPOINT 1 | `evidence/STEP-06-GATE-APPROVED.png` |
| 07-requirements | STEP 7 · CHECKPOINT 2 | `evidence/STEP-07-requirements.png`, `evidence/STEP-07-GATE-APPROVED.png` |
| 08-ui-analysis | STEP 8 | `evidence/STEP-08-ui-analysis` |
| 09-system-context | STEP 9 | `evidence/STEP-09-system-context.png`, `evidence/STEP-09-system-context.md` |
| 10-units | STEP 10 · CHECKPOINT 3 | `evidence/STEP-10-units.png`, `evidence/STEP-10-unit-map.png` |
| 11-stories | STEP 11 | `evidence/STEP-11-stories.png` |
| 12-bolt-plan | STEP 12 | `evidence/STEP-12-bolt-plan.png` |
| 13-inception-complete | STEP 12 · CHECKPOINT 4 | `evidence/STEP-12-INCEPTION-COMPLETE.png` |
| 14-bolt-execution | STEP 13–15 · CHECKPOINT 5 | `evidence/STEP-13-bolt-list.png`, `evidence/STEP-14-bolt-start.png`, `evidence/STEP-15-plan.png` |
| 15-dummy-apis | STEP 16 | `evidence/STEP-17-api-response.png`, `evidence/STEP-21-scale-api.png`, `evidence/STEP-26-request.png`, `evidence/STEP-27-printer-api.png` |
| 16-dummy-data | STEP 17 | `evidence/STEP-17-dummy-data.json` |
| 17-api-tests | STEP 18 | `evidence/STEP-18-api-tests.txt`, `evidence/STEP-18-api-results.png` |
| 18-batch-selection | STEP 19 | `evidence/STEP-19-batch-screen.png`, `evidence/STEP-19-network.png`, `evidence/STEP-19-test.png`, `evidence/STEP-19-api-flow.txt`, `evidence/STEP-19-web-test.txt` |
| 19-weighing | STEP 20 | `evidence/STEP-20-weigh-screen.png` |
| 20-scale-integration | STEP 21 | `evidence/STEP-21-scale-switch.png`, `evidence/STEP-21-scale-api.png`, `evidence/STEP-21-scale-api.txt` |
| 21-tolerance | STEP 22 | `evidence/STEP-22-tolerance-tests.txt`, `evidence/STEP-22-tolerance-ui.png`, `evidence/STEP-22-tolerance-ui-tests.txt` |
| 22-component-progression | STEP 23 | `evidence/STEP-23-five-component-demo.mp4` |
| 23-overshoot | STEP 24 | `evidence/STEP-24-overshoot-decision.md`, `evidence/STEP-24-overshoot-test.png`, `evidence/STEP-24-overshoot-ui.png`, `evidence/STEP-24-overshoot-tests.txt` |
| 24-confirmation | STEP 25 | `evidence/STEP-25-confirmation.png`, `evidence/STEP-25-transaction.json` |
| 25-orgatex-writeback | STEP 26 | `evidence/STEP-26-ui.png`, `evidence/STEP-26-request.png`, `evidence/STEP-26-response.png`, `evidence/STEP-26-data.png`, `evidence/STEP-26-writeback.txt` |
| 26-ticket | STEP 27 | `evidence/STEP-27-ticket.png`, `evidence/STEP-27-printer-api.png`, `evidence/STEP-27-ticket-test.png`, `evidence/STEP-27-ticket-tests.txt`, `evidence/STEP-27-ticket-ui.txt`, `evidence/STEP-27-ticket.txt`, `evidence/STEP-27-ticket-override.png` |
| 27-ui-validation | STEP 28 | `evidence/STEP-28-ui-validation` |
| 28-e2e | STEP 29 | `evidence/STEP-29-playwright-report`, `evidence/STEP-29-e2e-video.mp4` |
| 29-failure-tests | STEP 30 | `evidence/STEP-30-failure-tests.md`, `evidence/STEP-30-orgatex-outage.txt`, `evidence/STEP-30-orgatex-sync-failed.png`, `evidence/STEP-30-orgatex-retry-confirmed.png`, `evidence/STEP-30-printer-outage.txt`, `evidence/STEP-30-printer-banner.png`, `evidence/STEP-30-printer-print-pending.png` |
| 30-final-project-status | STEP 31 | `evidence/STEP-31-project-status.png`, `evidence/STEP-31-project-status.md` |
| 31-final-dashboard | STEP 32 | `evidence/STEP-32-final-dashboard.png`, `evidence/STEP-32-final-dashboard-specs.png`, `evidence/STEP-32-final-dashboard-bolts.png` |
| 32-final-validation | STEP 33 | `evidence/STEP-33-final-validation.md` |
| 33-final-evidence | STEP 34 | `FINAL-EVIDENCE.md` |
| 34-final-demo | STEP 35 | `evidence/STEP-35-final-demo.mp4` |

## Workbook evidence index

| Step | Files in `evidence/` |
|---|---|
| 00–14 (bootstrap, specsmd, inception, first bolt) | `STEP-00-git.png` … `STEP-14-bolt-start.png`, `STEP-08-ui-analysis/` |
| 15 · Bolt plan stage | `STEP-15-plan.png` (bolt 001's plan exactly as presented at CHECKPOINT 5, rendered from the Claude Code session transcript) |
| 16–19 · Dummy APIs, seed, API tests, batch selection | `STEP-17-*`, `STEP-18-*`, `STEP-19-*` |
| 20–27 · Weigh, scale, tolerance, progression, overshoot, confirmation, write-back, ticket | `STEP-20-*` … `STEP-27-*` |
| 28 · UI/UX validation | `STEP-28-ui-validation/` (screen 1–3 comparisons, `validation.md`) |
| 29 · E2E | `STEP-29-playwright-report/`, `STEP-29-e2e-video.mp4` |
| 30 · Failure scenarios | `STEP-30-failure-tests.md` and the outage files |
| 31–33 · Status, dashboard, human validation | `STEP-31-project-status.*`, `STEP-32-final-dashboard*.png`, `STEP-33-final-validation.md` |
| 35 · Final demonstration (recorded backup of the live demo) | `STEP-35-final-demo.mp4` |

The STEP 15, 20–27 and 35 captures come from the evidence scripts in `playwright/evidence/` (config `playwright/playwright.evidence.config.ts`). Each script runs on a fresh, isolated stack. A screenshot is taken only after the assertions about that screen's state pass. Run them with: `cd playwright && npx playwright test -c playwright.evidence.config.ts`.
