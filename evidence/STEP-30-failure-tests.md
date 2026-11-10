# STEP 30: Failure scenarios

Run on 2026-11-11 against the packaged app (Compose `full`, isolated E2E project) with the real browser, by `npm run e2e` (`playwright/tests/03-failures.e2e.ts`). Every row passed in two full runs from a clean stack (15/15 each run, including the B-24187 flow). The faults are real: the dummy device or OrgaTEX answers 503 through `/_sim/faults`.

| Scenario | Expected | Actual | Pass |
|---|---|---|---|
| Scale unavailable | Safe failure | Mid-pour, the 3 kg scale goes offline. The readout drops to "—", the status line says SCALE OFFLINE, the banner says "The scale is not responding.", Confirm and Tare are disabled, and the strip shows the scale OFFLINE. When the scale returns, the weight comes back and Tare works again; nothing was recorded. | ✅ |
| OrgaTEX unavailable | Defined behaviour (PD-01) | **On load:** the queue shows "OrgaTEX is not responding…" with Retry, the strip shows ORGATEX OFFLINE, and Completed batches stays available. Retry loads the queue once OrgaTEX is back. **At completion:** B-24191 is weighed to the end with OrgaTEX down; the summary shows Sync failed with Retry, the strip shows 1 UNSYNCED, and OrgaTEX received nothing. After it returns, Retry shows CONFIRMED, and OrgaTEX received exactly one post. | ✅ |
| Printer unavailable | Defined behaviour (PD-02) | B-24188 completes. F1 shows "The printer is not responding…", the ticket stays Print pending and the preview stays on screen; the printer received nothing. After it returns, F1 prints exactly one copy: Printed (1). | ✅ |
| Invalid weight | Cannot confirm | A reading above the 3 kg scale's capacity (3500 g) shows OVER CAPACITY and "The scale reading can't be used."; Confirm stays disabled. | ✅ |
| Over tolerance | Cannot confirm | An overshoot on Navy RGB (2850 g ± 12 g) shows OVER TOLERANCE with the surplus and recovery actions and offers Supervisor override; Confirm is disabled and F1 records nothing. | ✅ |
| Under tolerance | Cannot confirm | A pour that stops at 2826.0 g (below 2838.0) shows DISPENSE; Confirm is disabled and F1 records nothing. | ✅ |
| Re-tare | Weight reset | With 2826.0 g on the scale, F2 re-tares and the readout shows 0.0 g. | ✅ |
| Quit | Safe exit | After a full pour, F4 asks "Discard attempt?" and the second F4 discards it: still 0 of 7 weighed, the component is PENDING again, and nothing is active until the operator chooses. | ✅ |
| Incomplete batch | Cannot complete | With 1 of 7 components weighed on B-24190, `POST …/session/completion` is refused with 409 `Batch.Incomplete`; OrgaTEX and the printer received nothing, and the queue shows the batch as WEIGHING. | ✅ |

Also covered in the same suite:

- **Scanner unavailable**: SCANNER OFFLINE on the strip; the batch still opens by touch (SCANNER-FALLBACK-SCOPE).
- **Scale switching at the boundary**: Remazol Black B at exactly 3.0 kg uses the 3 kg precision scale (≤ 3 kg inclusive).

Earlier evidence for the same behaviour, from bolts 011 and 012: `STEP-30-orgatex-outage.txt`, `STEP-30-orgatex-sync-failed.png`, `STEP-30-orgatex-retry-confirmed.png`, `STEP-30-printer-outage.txt`, `STEP-30-printer-print-pending.png`, `STEP-30-printer-banner.png`.

The real OrgaTEX, scales, scanner and printer are simulated, so these results hold for the training contracts only.
