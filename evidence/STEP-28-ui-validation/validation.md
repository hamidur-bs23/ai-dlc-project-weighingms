# STEP 28: UI/UX validation

The prototype (`docs/customize-weighing-station.html`) against the working app on the Compose `full` stack, captured on 2026-11-11 by `playwright/evidence/capture-step28.mjs` at 1280 × 860. Each sheet has the mockup on the left and the app on the right.

- `screen-1-comparison.png`: 1 · Select batch
- `screen-2-comparison.png`: 2 · Weigh (the app shows B-24190's Yellow RGB part-poured, because B-24187 was completed by the E2E run)
- `screen-3-comparison.png`: 3 · Confirm & print (B-24187)

The comparison below is from the text and behaviour of both. The pictures themselves are for a person to judge.

## Screens

| Screen | Match | Differences, and why |
|---|---|---|
| 1 · Select batch | Same layout: scan prompt, "Or touch a row below. Queue received from OrgaTEX at …", the same seven columns, the four seed batches with the same order, machine, fabric, shade and component count, the status strip. | The status tags come from the server (QUEUED, WEIGHING, COMPLETE, SYNC PENDING, SYNC FAILED, PRINT PENDING) and TRAINING marks training batches. A **Completed batches** button opens the review list, which the mockup doesn't have (REVIEW-ACCESS). The strip names the scales in full ("3 KG PRECISION"). |
| 2 · Weigh | Recipe rail with done/weighing/pending and actual weights, header with chemical, code, LOT, CONTAINER and TARE, the automatic scale badge, Weighed now / Still to weigh / Target, the bar with the tolerance band, the status line, OK · Confirm F1, Re-tare F2, Quit F4. | Lot and container fields are on screen (the mockup has none, Q-UI-2). The "before you can confirm" hints say what is missing. **Supervisor override** appears only when it is offered (PD-05), not permanently. No Simulate buttons: the dummy scale is driven by its own API, never by the UI. |
| 3 · Confirm & print | Title "Batch … weighed" in green, the summary rows (components weighed, all within tolerance, total dispensed, elapsed, operator, Written to OrgaTEX), the consumption sentence, Print ticket F1 / Next batch F3, the ticket preview with the barcode. | Extra **Completed** and **Ticket** rows and a per-chemical table. Full chemical names on the ticket, never shortened (TICKET-LINES). The sentence stops at "Actual consumption posted against the batch." (SUMMARY-CONSUMPTION-NOTE). Print becomes **Reprint** after the first copy. |

## The workbook's list

| Aspect | Result |
|---|---|
| States | Underweight, within range, target achieved, overweight, unstable, no tolerance, scale offline, over capacity; queue and write-back states; print pending and printed. Each has its own text, never colour alone. |
| Interactions | Touch or scan to open, F1 / F2 / F3 / F4, quit with confirmation, override dialog, Retry, review list. |
| Data | Every value comes from the Api and the dummies; nothing is hard-coded in the UI (lint rule `no-batch-literals`). |
| Labels | The mockup's wording, through Transloco. |
| Units | Each component in its recipe unit (g or kg), weighed in its scale's unit. See the known issue below. |
| Tolerance | Inclusive bands as in the mockup, e.g. "TOLERANCE ±0.5 g (93.5 – 94.5)". |
| Scale switching | Automatic: ≤ 3 kg precision, > 3 kg floor scale, shown on the badge and the rail. |
| Component progression | The rail and "n of N components weighed" follow each confirm. |
| Completion | The last confirm completes the batch and opens Confirm & print. |
| Ticket | Batch, order, machine, shade, date, operator, components, target, actual and a Code 128 barcode of the batch ID. |

## Known differences that are not fixed

- **Mixed units on the precision scale for a kg target.** B-24190's Remazol Black B (3.000 kg on the 3 kg precision scale, which weighs in g) shows Target 3.000 kg, while Weighed now, Still to weigh and the tolerance are in g. The values are right; the units differ on one screen.
- **Dim text is lighter than the mockup.** The mockup's `--hmi-dim` (#63717f) fails WCAG AA contrast on the dark panels (2.9–3.6:1). The app uses #8193a5 (≥ 4.6:1), which sits closer to the muted text.
- **The F1 hint on the green OK key is at full strength** rather than faded, for the same reason (3.8:1 faded, 6.9:1 at full strength).
