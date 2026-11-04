---
intent: 001-assisted-weighing-station
phase: inception
status: approved
created: 2026-11-03T22:30:00Z
updated: 2026-11-04T07:30:00Z
---

# Decisions Brief: STEP 7 product-owner decisions

> **Status: approved at CHECKPOINT 2 (`APPROVE REQUIREMENTS`, 2026-11-04T07:30:00Z).**
> The product owner made these decisions on 2026-11-03 under workbook Rule 2 (Ask → Record → Decide → Continue).
> They are **project decisions, not FCI confirmations**. The brief's assumptions A-01…A-15 remain unconfirmed.
> Register of record: `memory-bank/standards/decision-index.md`. Requirements that apply them: `requirements.md`.
> **PD-05 (overshoot / surplus handling) is not decided here.** It stays pending until workbook STEP 24.

**Evidence order (ADR-015):** PDF (`docs/project-requirement.md`) → `workbook.md` → mockup
(`docs/customize-weighing-station.html`, line numbers as of commit 423b4dc) → `docs/brief.md` (*unconfirmed*).
"Silent" means the source says nothing on the topic.

## Summary

| ID | Topic | Decision |
|---|---|---|
| PD-01 | OrgaTEX unavailable at write-back | Store-and-forward outbox, automatic bounded retry, then a visible "sync failed" state |
| PD-02 | Printer unavailable at completion | Completion is not blocked. The ticket is held as print pending and printed manually; prints after the first are marked REPRINT |
| PD-03 | Scanner mode and scope | Device-style HTTP scanner dummy. It scans the batch and the dyestuff lot. Touch or typed entry is the fallback. No wedge, badge or container scan |
| PD-04 | Ticket | Fixed V1 layout, 80 mm, Code 128 barcode = batch ID, override lines marked "OVR" |
| PD-06 | Time zone | Stored in UTC. Shown and printed in a configured IANA zone (default `Asia/Dhaka`) |
| PD-07 | Weighing order | Free selection of any pending chemical. The recipe sequence is shown, and the next pending chemical is suggested |
| TOL | Tolerance source | Recipe tolerance if provided. Otherwise configured tolerance per chemical + scale. Weighing is blocked if neither exists |
| OVR | Authorised exception | Any out-of-tolerance weighing (under or over) can be confirmed only with a supervisor badge + PIN step-up (ADR-009) and a mandatory reason |

---

## PD-01: OrgaTEX unavailable, consumption write-back

**Options considered:** outbox with automatic retry · local save with manual retry · stop the station.
**Decision:** outbox with automatic retry.
- Completion and an outbox entry are written in one local transaction. The POST `POST /api/batches/:batchId/consumption` is sent afterwards, so completion never waits for OrgaTEX.
- Every POST carries an idempotency key, so a retry never posts twice.
- Retries use back-off up to a configurable limit. After that the entry becomes **failed** and is surfaced for manual retry.
- The status strip shows the OrgaTEX state and the count of unsynced entries. The summary moves from "Pending sync" to "Written to OrgaTEX hh:mm:ss".

**Evidence**
- PDF: silent on write-back and outages.
- Workbook STEP 26: completion leads to `POST /api/batches/B-24187/consumption`, the response is `200 OK`, and the UI shows "Written to OrgaTEX CONFIRMED".
- Workbook STEP 30: "OrgaTEX unavailable | Defined behaviour".
- Mockup: view 3 shows "Written to OrgaTEX CONFIRMED 07:48:22". Pin Q13 asks "does weighing continue offline and sync later, or does the station stop?"
- Brief (*unconfirmed*, A-03):
  - BR-16 "A network or OrgaTEX outage must not stop weighing that is already in progress" (Must). BR-17 "…must reach OrgaTEX once connectivity returns, without re-keying" (Must). BR-18 "…see which weighings have not yet reached OrgaTEX" (Should).
  - FR-30–33, including FR-33 "Surface synchronisation failures for manual attention". IR-08 requires idempotent posting.
  - SYNC_ENTRY status is `PENDING | SENT | FAILED`.

**Why not the alternatives:** A-03 itself says stopping the station would be "a simplification". It is a legitimate option, but it would block completion during outages. Manual-only retry would leave BR-18 visibility and FR-31 automatic transmission to operator discipline.

## PD-02: Printer unavailable at completion

**Options considered:** print pending + manual print · print pending + auto-print · block completion.
**Decision:** print pending + manual print.
- The batch completes regardless of the printer. The ticket is stored as print pending.
- The status strip shows the printer as offline.
- "Print ticket (F1)" in the summary sends the ticket once the printer is available. Every print after the first is marked REPRINT.

**Evidence**
- PDF: silent. It requires only a completed-job summary (§6 Step 9).
- Workbook STEP 27: Batch Complete → Generate Ticket → Dummy Printer → Print / Print Preview. STEP 30: "Printer unavailable | Defined behaviour".
- Mockup: view 3 has "Print ticket F1" and "Next batch F3". The status strip has a `PRINTER` indicator (l.622).
- Brief (*unconfirmed*): FR-27 "Print a weighing ticket on batch completion". FR-28 "Permit ticket reprint, marked as a reprint". AUDIT_EVENT type REPRINT.

**Accepted risk:** brief BR-15 (Must, A-04) says "A physical record must accompany the weighed container". When the printer is down, the container may leave before its ticket is printed. Mitigation: the queue and summary show print pending prominently.

## PD-03: Scanner mode and scope

**Options considered:** device API + wedge · device API only · wedge only.
**Decision:** device API only, with manual fallback.
- The scanner dummy is a device in the simulator. Tests and demos emit a scan with `POST /_sim/scanner/scan {"code":"…"}`, which follows the `/_sim/*` convention in `api-conventions.md`.
- WeighingMS receives scans through its own API and routes them by context:
  - on the queue view, a scan is a batch ID;
  - on the weigh view, a scan is a dyestuff lot.
- The scanner has READY / OFFLINE status and can be faulted.
- **Fallbacks:** touch a queue row to select a batch; type the lot.
- **Out of V1:** keyboard wedge, operator badge scan (operators type their badge number, ADR-009), container scan, and dyestuff identity verification by barcode.

**Evidence**
- **No source in `/docs` specifies a scanner mode.** This is a product-owner decision.
- PDF: silent on the scanner. It only says "the operator selects a job".
- Workbook STEP 16: "You need: … Dummy Scanner". Rule 5: "Dummy integrations are real development artifacts… Use APIs." Neither defines a contract.
- Mockup: view 1 says "Scan a batch card to begin. Or touch a row below." The status strip has a `SCANNER` indicator (l.621).
- Brief (*unconfirmed*): BR-02 and FR-02 cover starting a batch by scan. FR-10 covers capturing the lot. IR-18 covers peripheral status.

**Deviations from the brief, recorded deliberately:**
- IR-15 asks for keyboard-wedge or serial mode. V1 uses an HTTP device API instead.
- FR-09 and BR-10 ask for dyestuff identity verification. V1 is lot capture only (FR-10); identity verification is out of V1.

## PD-04: Ticket

**Options considered:** fixed V1 layout on 80 mm · fixed V1 layout on A6 · configurable template.
**Decision:** fixed V1 layout on 80 mm thermal paper, also shown as an on-screen preview. Contents, top to bottom:
- Header: batch, order, machine, shade, date/time (PD-06), operator.
- Component lines: target and actual, with unit. Overridden lines are marked "OVR".
- A closing line: "ALL WITHIN TOLERANCE" or "OVERRIDES: n".
- A Code 128 barcode encoding the batch ID, with the batch ID printed beneath it.

**Evidence**
- PDF: silent.
- Workbook STEP 27 fields: Batch, Order, Machine, Shade, Date, Operator, Components, Target, Actual, Barcode.
- Mockup ticket (l.590–610): `FCI DYEING — WEIGHING TICKET`, the fields above, "ALL WITHIN TOLERANCE", and a barcode followed by `B-24187`. The mockup itself states **"Label content is invented"**, and pin Q18 asks about fields, barcode symbology, size and printer language. So the layout is a reference, not a specification.
- The 80 mm width and Code 128 symbology are decisions; no source specifies them.

**Deviation from the brief, recorded deliberately:** IR-16 and NFR-14 (*unconfirmed*) want ticket layout configurable without a code change. V1 uses a fixed layout.

## PD-06: Time zone

**Options considered:** configured IANA zone · station OS zone · UTC everywhere.
**Decision:**
- All timestamps are stored as UTC.
- The UI and the ticket render them in a configured IANA zone, defaulting to `Asia/Dhaka` (UTC+6, no DST).
- The browser or server OS time zone doesn't affect what is displayed or printed.

**Evidence**
- PDF: silent beyond "confirmation timestamp" (§10).
- Workbook STEP 27: the ticket has a "Date".
- Mockup: the ticket shows a local time, `2026-08-04 07:48`.
- Brief: silent.
- Rationale: a fixed configured zone makes the displayed time independent of each machine's OS setting, so automated tests produce deterministic output.

## PD-07: Weighing order

**Options considered:** free selection with the sequence shown · strict sequence · sequence + supervisor skip.
**Decision:**
- Any chemical that isn't done can be selected.
- Recipe sequence is displayed. After each confirmation, the next pending chemical in sequence is pre-selected.
- The batch can complete only when **all N** chemicals are confirmed.

**Evidence**
- PDF §6 Step 4 (p.5): "The operator selects a chemical from the recipe list." Step 8 (p.7): "After confirming one chemical, the operator returns to the recipe list and selects the next chemical."
- Workbook STEP 23 shows a fixed progression (Blue RSPL → … → Soda Ash) and says: "A component cannot be marked complete unless its weight is valid. The batch cannot complete until all components are complete."
- Mockup:
  - l.711 binds a click handler to every rail item. l.718 `select(i)` returns early only when the chemical is `done`.
  - After OK, the next chemical is `components.findIndex(x => x.state === "pending")`, i.e. the first pending one in sequence.
- **Counter-evidence (considered):** brief FR-06 "Present components in recipe sequence" and RECIPE_COMPONENT.sequence "Weighing order" (*unconfirmed*). STEP 23's fixed order is satisfied as the suggested path, not a lock.

## TOL: Tolerance source

**Options considered:** recipe first with configured fallback · configuration only · recipe only.
**Decision:**
- If the recipe line provides a tolerance, use it.
- Otherwise use the WeighingMS-configured tolerance for that **chemical + scale** pair.
- If neither exists, weighing that chemical is blocked with "no tolerance defined".
- **No universal or per-scale default value.**
- Bounds are inclusive.
- The tolerance applied, and its source, are snapshotted onto the weighing record.

**Evidence**
- PDF §4.2: "Applicable weighing tolerance, if provided."
- PDF §6 Step 6: "The exact tolerance rules should be configurable rather than relying on an assumed universal tolerance for every chemical and scale… must use the precision and tolerance rules applicable to the selected weighing scale and chemical."
- PDF §7 example: 1.000 kg ± 0.010 kg gives 0.980 → Underweight, 0.995 → Within acceptable range, 1.000 → Target achieved, 1.008 → Within acceptable range, 1.020 → Overweight.
- Workbook STEP 22: Yellow RGB 94.0 g ± 0.5 g. 93.4 → cannot confirm. 93.5, 94.0 and 94.5 → can confirm. 94.6 → cannot confirm.
- Brief (*unconfirmed*, A-07): BR-09 says tolerances are "per component and must originate from OrgaTEX where available". WEIGHING_RECORD snapshots tolerance_min and tolerance_max.

## OVR: Authorised exception (supervisor override)

**Options considered:** any out-of-tolerance weighing, with PIN + reason · overweight only · decide together with PD-05.
**Decision:**
- An underweight or overweight weighing can be confirmed only through "Supervisor override", which needs:
  - a supervisor badge + PIN step-up (ADR-009);
  - a mandatory reason.
- The record keeps its true class (Underweight or Overweight). It is flagged as overridden, with the supervisor, the reason and the time.
- Without the override, Confirm stays disabled.
- **This does not decide what happens on overshoot beyond warn / block / override.** That is PD-05, at STEP 24.
- No credentials appear in this document. Seed supervisor credentials belong only in dev seed configuration.

**Evidence**
- PDF §6 Step 7 (p.7): "The application should prevent an unsuccessful weighing attempt from being recorded as a successfully completed weighing without an explicit, authorized exception."
- Workbook STEP 25: "Valid? NO → Confirm disabled". STEP 30: "Over tolerance | Cannot confirm" and "Under tolerance | Cannot confirm". These hold for the operator alone.
- Mockup:
  - l.779 `$("btn-ok").disabled = (z !== "ok")`.
  - l.824 "Supervisor override" sets "OVERRIDE REQUESTED — AWAITING SUPERVISOR". Pin Q33 asks "who may override… must it be logged…".
- Brief (*unconfirmed*):
  - BR-07 (A-07): outside tolerance not accepted "without explicit authorisation". BR-13 (A-08): who authorised and why.
  - FR-18 "Permit confirmation only while the weight is inside tolerance, or after authorised override". FR-35 supervisor authentication.
  - The OVERRIDE entity has a reason field marked "Mandatory".

---

## Not decided here

| ID | Topic | Decide at |
|---|---|---|
| PD-05 | Overshoot / surplus handling (compensation, recalculation, discard rules). The prototype's recalculation is not adopted | Workbook STEP 24 |
| A-01…A-15 | FCI confirmation of the brief's assumptions | Outside the lab |
