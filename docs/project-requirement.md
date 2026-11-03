# Dyeing Chemical Precision Weighing System

> **Source:** `docs/project-requirement.pdf` (12 pages). Converted to Markdown on 2026-11-04 for reference.
> **No content changed.** The text, lists and tables below are transcribed verbatim from the PDF, with page markers shown as `<!-- p.N -->`.
> Everything after the horizontal rule titled **"Reference notes (not part of the original)"** was added for traceability and is **not** part of the source document.

---

<!-- p.1 -->
## 1. Project Overview

The goal of this project is to develop a web-based application for managing and monitoring the precise weighing of chemicals used in the dyeing department of a Ready-Made Garments (RMG) factory.

During the dyeing process, different chemicals and dyes must be measured according to predefined recipes. Each recipe specifies the chemicals required and the quantity of each chemical needed for a particular dyeing batch.

The application will help operators select the dyeing jobs that need to be processed, identify the required chemicals and their target quantities, and accurately weigh each chemical using an appropriate weighing scale.

The system will automatically compare the measured weight against the required quantity and indicate whether the quantity is insufficient, acceptable, or excessive.

The primary objective is to improve weighing accuracy, reduce manual calculation errors, and provide a clear, controlled, and traceable chemical-weighing process.

**The application is a precision-weighing management system, not an automated chemical-dispensing system.** Operators will physically handle and dispense the chemicals, while the application guides and monitors the weighing process.

## 2. Business Context

The application operates within the dyeing department of an RMG factory.

Two important sets of information originate from external systems:

1. **Dyeing Job Information:** Information about which items or batches need to be dyed, including their processing schedules and associated requirements.
2. **Recipe Information:** The list of chemicals required for a particular dyeing job, including the required quantity of each chemical.

These details will be obtained from external systems through simulated third-party APIs.

For this project, actual integration with the factory's production systems is not required. Instead, mock APIs will simulate the external systems and provide realistic data for development and demonstration.

The application must consume this data and use it to manage the chemical-weighing workflow.

<!-- p.2 -->
## 3. Main Objectives

The application should:

- Display dyeing jobs received from an external production system.
- Allow operators to select a dyeing job for processing.
- Retrieve and display the recipe associated with the selected job.
- Identify the required chemicals and their target quantities.
- Automatically determine the appropriate weighing scale for each chemical.
- Display the live measured weight received from the selected scale.
- Compare the actual weight with the required weight.
- Clearly indicate whether the chemical is underweight, within the acceptable range, or overweight.
- Guide the operator through the weighing of all chemicals required by the recipe.
- Maintain records of weighing activities and their results.

## 4. External System Integration

The application will simulate integrations with external systems using mock APIs.

### 4.1 Dyeing Job API

The external production system provides the dyeing jobs that need to be processed.

Each job should contain relevant information such as:

- Job or batch identifier.
- Dyeing item or product information.
- Production order reference.
- Scheduled processing information.
- Associated recipe reference.
- Current processing status.

The application will retrieve these jobs and present them to the operator in an organized interface.

<!-- p.3 -->
The operator should be able to identify which jobs are ready for chemical preparation and select one to begin the weighing process.

### 4.2 Recipe API

The recipe system provides the chemical requirements for a selected dyeing job.

A recipe should contain a list of required chemicals. Each chemical entry should include information such as:

- Chemical identifier.
- Chemical name.
- Required quantity.
- Unit of measurement.
- Applicable weighing tolerance, if provided.

For example, a recipe might require:

| Chemical | Required Quantity |
|---|---|
| Chemical A | 0.250 kg |
| Chemical B | 1.500 kg |
| Chemical C | 2.800 kg |
| Chemical D | 5.000 kg |

These quantities are illustrative.

The application must use the recipe data to guide the operator through the weighing process.

The recipe and job data should be obtained through mock API endpoints rather than hardcoded directly into the user interface.

## 5. Weighing Scale Management

The application will support two types of weighing scales, each intended for a different weight range.

### 5.1 Precision Scale — Up to 3 kg

This scale is intended for chemicals whose required quantities are **3 kg or less**.

<!-- p.4 -->
It provides the precision necessary to measure relatively small quantities of chemicals.

For example:

- 0.250 kg
- 1.500 kg
- 3.000 kg

### 5.2 Heavy-Duty Scale — Above 3 kg

This scale is intended for chemicals whose required quantities are **greater than 3 kg**.

For example:

- 3.500 kg
- 5.000 kg
- 12.000 kg

The two scales have different measurement characteristics and precision levels. The system must therefore select the appropriate scale based on the required quantity.

### 5.3 Automatic Scale Selection

The application must determine which scale should be used for each chemical.

The selection rule is:

| Required Quantity | Selected Scale |
|---|---|
| Less than 3 kg | Precision Scale |
| Exactly 3 kg | Precision Scale |
| Greater than 3 kg | Heavy-Duty Scale |

The operator should not need to decide manually which scale is appropriate during the normal workflow.

The application should display the selected scale and use its measurement data to evaluate the weighing result.

The weighing thresholds and scale assignments should be designed so that they can be configured later if the factory's equipment or operational requirements change.

## 6. Chemical Weighing Workflow

<!-- p.5 -->
The application should implement the following end-to-end workflow.

### Step 1: Retrieve Dyeing Jobs

The application retrieves available dyeing jobs from the mock production API.

The operator sees a list of jobs that are ready for chemical preparation.

Each job should display enough information to identify the relevant batch and understand its processing requirements.

### Step 2: Select a Dyeing Job

The operator selects a job to begin the chemical-weighing process.

The application retrieves the corresponding recipe through the mock recipe API.

The selected job and its recipe should remain associated throughout the weighing process.

### Step 3: Display Recipe Items

The application displays all chemicals required for the selected job.

For each chemical, it shows:

- Chemical name.
- Required quantity.
- Unit of measurement.
- Selected weighing scale.
- Current weighing status.

The operator can identify which chemicals have already been weighed and which remain to be processed.

### Step 4: Select a Chemical for Weighing

The operator selects a chemical from the recipe list.

The application identifies the target quantity and automatically selects the appropriate weighing scale.

For example, if the target quantity is 1.500 kg, the application selects the precision scale.

If the target quantity is 5.000 kg, the application selects the heavy-duty scale.

### Step 5: Read the Measured Weight

<!-- p.6 -->
The application receives the measured weight from the selected scale.

For the initial implementation, the scale may be simulated through a mock scale API or a weighing-device simulator.

The purpose is to demonstrate the expected interaction between the application and the weighing equipment without requiring physical weighing hardware.

The displayed weight should update as new measurement values are received.

### Step 6: Compare Actual Weight Against Required Weight

The application continuously compares the measured weight with the target quantity.

It should clearly indicate one of three primary conditions:

**Underweight**

The measured quantity is below the acceptable range.

The application indicates that more chemical is required.

**Within Acceptable Range**

The measured quantity is within the configured tolerance for the target quantity.

The application indicates that the weighing requirement has been satisfied.

**Overweight**

The measured quantity exceeds the acceptable range.

The application warns the operator that the quantity is excessive.

The exact tolerance rules should be configurable rather than relying on an assumed universal tolerance for every chemical and scale.

The comparison must use the precision and tolerance rules applicable to the selected weighing scale and chemical.

### Step 7: Confirm the Weighing Result

When the required quantity has been reached within the acceptable tolerance, the operator can confirm the weighing result.

The application records the result and marks the chemical as weighed.

The record should include the target quantity, actual measured quantity, selected scale, associated job, chemical, and weighing status.

<!-- p.7 -->
The application should prevent an unsuccessful weighing attempt from being recorded as a successfully completed weighing without an explicit, authorized exception.

### Step 8: Continue with the Remaining Chemicals

After confirming one chemical, the operator returns to the recipe list and selects the next chemical.

The application repeats the scale-selection, measurement, comparison, and confirmation process for each remaining chemical.

The interface should make the current progress visible, including completed items, the current item, and outstanding items.

### Step 9: Complete the Recipe Weighing Process

Once all required chemicals have been weighed and their results confirmed, the application marks the weighing process as completed.

The application should provide a summary of the weighing results for the selected dyeing job.

The summary should show the required quantities, actual quantities, and final statuses of the individual chemicals.

The completed weighing record should remain available for review.

## 7. Weighing Accuracy and Tolerance

The application must account for the fact that different weighing scales have different measurement ranges and precision characteristics.

A measured quantity should not be considered acceptable merely because it is numerically close to the target quantity.

Instead, the system should evaluate the result against an explicitly defined acceptable range.

For example, suppose a recipe requires 1.000 kg of a chemical and the configured tolerance is ±0.010 kg.

The acceptable range would be:

- Minimum acceptable weight: 0.990 kg.
- Target weight: 1.000 kg.
<!-- p.8 -->
- Maximum acceptable weight: 1.010 kg.

The application should classify measurements as follows:

| Actual Weight | Result |
|---|---|
| 0.980 kg | Underweight |
| 0.995 kg | Within acceptable range |
| 1.000 kg | Target achieved |
| 1.008 kg | Within acceptable range |
| 1.020 kg | Overweight |

These values are illustrative. Actual tolerances must be defined according to the scale's capabilities and the factory's operational requirements.

The application should distinguish between the target weight and the acceptable weight range.

## 8. User Interface Requirements

The attached HTML mockup is an important reference for understanding the expected application.

It should be reviewed carefully before implementation.

The goal is to reproduce the intended user experience and implement the functionality represented by the mockup, rather than creating an unrelated interface.

The interface should make the following information easy to understand:

- Available dyeing jobs.
- The selected job and its recipe.
- Required chemicals and quantities.
- The automatically selected weighing scale.
- The current measured weight.
- The acceptable target range.
- The current weighing status.
<!-- p.9 -->
- The progress of the overall weighing process.

The interface should use clear visual indicators for underweight, acceptable, and overweight conditions.

Operators should be able to understand the current situation quickly without performing manual calculations.

The interface should also make it clear which chemical is currently being weighed and what action the operator should take next.

## 9. Mock API and Simulation Requirements

Because actual third-party systems and physical weighing equipment are not required for the initial implementation, the application must provide realistic simulations.

At minimum, the implementation should include:

1. **Mock Dyeing Job API:** Returns available dyeing jobs and their relevant information.
2. **Mock Recipe API:** Returns the required chemicals and quantities for a selected job.
3. **Mock Weighing Device:** Simulates weight measurements from the two types of scales.

The weighing simulation should support realistic scenarios, including:

- Weight gradually increasing toward the target.
- Weight remaining below the acceptable range.
- Weight entering the acceptable range.
- Weight exceeding the acceptable range.
- Measurements appropriate to the selected scale.

The mock services should behave like external integrations so that the application's business logic does not depend directly on hardcoded demonstration data.

The implementation should also keep the integration boundaries clear, making it possible to replace the mock services with actual external APIs and weighing-device integrations in the future.

## 10. Weighing Records and Traceability

Every confirmed weighing operation should produce a record associated with the relevant dyeing job and chemical.

<!-- p.10 -->
The record should capture, at minimum:

- Dyeing job or batch reference.
- Chemical identifier and name.
- Required quantity.
- Actual measured quantity.
- Selected weighing scale.
- Weighing result.
- Confirmation timestamp.

The application should maintain the relationship between the job, recipe, individual chemicals, and their weighing records.

This will make it possible to review the weighing results and determine which quantities were measured for a particular dyeing job.

## 11. Scope and Implementation Priorities

The implementation should prioritize the following capabilities.

### Priority 1 — Core Weighing Workflow

- Retrieve dyeing jobs.
- Select a job.
- Retrieve its recipe.
- Display the required chemicals.
- Automatically select the appropriate weighing scale.
- Receive simulated weight measurements.
- Compare measured weight against the target and tolerance.
- Confirm weighing results.
- Complete the recipe-weighing workflow.

### Priority 2 — Operational Visibility

- Display weighing progress.
- Clearly distinguish underweight, acceptable, and overweight conditions.
<!-- p.11 -->
- Maintain weighing records.
- Provide a completed-job summary.

### Priority 3 — Integration Readiness

- Keep mock APIs separate from business logic.
- Simulate the behavior of both weighing scales.
- Maintain clear integration boundaries for future external systems and physical weighing devices.

The attached HTML mockup should guide the interface implementation, while this brief should guide the business workflow and expected system behavior.

## 12. Expected Outcome

The final application should demonstrate a complete, working chemical-weighing process for an RMG dyeing department.

A user should be able to select a dyeing job, review its recipe, weigh each required chemical using the automatically selected scale, observe the measured weight, understand whether the quantity is acceptable, confirm the result, and complete the overall weighing process.

The application should use mock APIs and simulated weighing devices while preserving an architecture that can accommodate real integrations later.

**The most important acceptance criterion is that the complete workflow works end to end—not merely that the screens resemble the HTML mockup.**

### Important implementation note

Before starting development, the HTML mockup should be reviewed alongside this brief to identify any specific screens, controls, visual states, or interactions that need to be preserved.

Three rules should guide implementation:

1. **The product brief defines the business intent.**
2. **The HTML mockup defines the intended visual experience and interaction patterns.**
3. **The working application must connect both through functional business logic, mock integrations, and a complete weighing workflow.**

<!-- p.12 -->
### Also note -

**Please go through the brief.md and view the html along with this project description for better understanding.**

---

## Reference notes (not part of the original)

*Added on 2026-11-04 for traceability. Interpretations here are recorded as notes, not as requirements.*

### A. Page map
| Page | Content |
|---|---|
| 1 | §1 Project Overview, §2 Business Context |
| 2 | §3 Main Objectives, §4 External System Integration, §4.1 Dyeing Job API |
| 3 | §4.1 (end), §4.2 Recipe API (recipe example table), §5 Weighing Scale Management, §5.1 |
| 4 | §5.1 (examples), §5.2 Heavy-Duty Scale, §5.3 Automatic Scale Selection (selection-rule table), §6 heading |
| 5 | §6 Steps 1–5 |
| 6 | §6 Steps 5–7 |
| 7 | §6 Step 7 (authorised exception), Steps 8–9, §7 Weighing Accuracy and Tolerance |
| 8 | §7 (classification table), §8 User Interface Requirements |
| 9 | §8 (end), §9 Mock API and Simulation Requirements, §10 Weighing Records and Traceability |
| 10 | §10 (record fields), §11 Priorities 1–2 |
| 11 | §11 Priority 2 (end) and 3, §12 Expected Outcome, Important implementation note |
| 12 | "Also note" (read brief.md and the HTML) |

### B. Terminology map
| PDF term | brief.md / prototype / workbook term |
|---|---|
| Dyeing job | Batch (e.g. `B-24187`) |
| Production order reference | Order (e.g. `SO-9931`) |
| Chemical | Component / dyestuff / auxiliary (e.g. Remazol Yellow RGB, Sodium Sulphate) |
| Precision Scale (≤ 3 kg) | Prototype "3 kg precision" scale |
| Heavy-Duty Scale (> 3 kg) | Prototype "32 kg floor" scale |
| External production system / recipe system | OrgaTEX (MES) |
| Authorized exception | Supervisor override |
| Completed-job summary | View 3 · Confirm & print (summary + ticket) |

### C. "This brief" vs `brief.md`
§11 says "this brief should guide the business workflow", and the implementation note says "The product brief defines the business intent". The PDF refers to itself as "this brief", and page 12 also points to `brief.md`. **Project decision (2026-11-04, ADR-015):** this PDF and `workbook.md` define mandatory scope. `brief.md` fills gaps only where both are silent, citing its IDs and assumption IDs (A-xx), and is marked unconfirmed.

### D. Cross-source coverage of open topics
| Topic | This PDF | workbook.md | brief.md (draft, unconfirmed) | Prototype | Status |
|---|---|---|---|---|---|
| Chemical weighing order | Operator **selects** a chemical (Steps 4, 8) | STEP 23 shows a fixed progression | FR-06 "present in recipe sequence" | Any non-done chemical is selectable | **PD-07**, decide in STEP 7 |
| Overweight / overshoot | Warn (Step 6); no success without an authorised exception (Step 7) | STEP 24: present 2–3 options, human decides | FR-17–24, A-08 (compensation) | Overshoot + override behaviour | **PD-05**, decide in STEP 24 |
| OrgaTEX consumption write-back & outage | Not covered | STEP 26 (POST consumption), STEP 30 (defined behaviour) | FR-26, FR-30–33, BR-16–18, A-03 | Status strip | **PD-01**, STEP 7 |
| Printer / ticket | Not covered (summary only, Step 9) | STEP 27 (ticket fields, dummy printer), STEP 30 | FR-27, FR-28, IR-16, A-04 | Ticket view; content marked "invented" | **PD-02**, **PD-04**, STEP 7 |
| Scanner | Not covered | §0 Dummy Scanner required; contract undefined | FR-02, FR-09, FR-10, IR-15, A-09, A-10 | — | **PD-03**, STEP 7 |
| Time zone | Not covered ("confirmation timestamp") | Ticket "date" | Not covered | Ticket shows local time | **PD-06**, STEP 7 |
| Tolerance source | Recipe tolerance "if provided" (§4.2); configurable per scale and chemical (§6–7) | Yellow RGB 94.0 g ± 0.5 g example | Tolerance master data | Per-component tolerance | Precedence to be defined in STEP 7 |
