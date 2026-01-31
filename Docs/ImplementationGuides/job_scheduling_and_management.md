# Job System Implementation Guide

This document specifies the **job scheduling and execution system** used by the game. It is intended as a standalone implementation reference and should be read independently from other systems (masks, resources, UI, etc.).

The job system is modeled after an **out-of-order (OoO) processor**:

* Jobs behave like instructions
* Inputs behave like operands
* NPCs behave like functional units
* Workstations behave like execution units
* Buffers behave like reservation stations

The system is fully autonomous: NPCs are never assigned manually, and players influence behavior only by enabling jobs and shaping infrastructure.

---

## 1. Job Definitions (Static Templates)

A **JobDefinition** describes what a job *is*. It never changes at runtime and never appears directly in the job list.

JobDefinitions act as immutable templates from which runtime job instances are created.

### JobDefinition Properties

* **type**

  * Logical job category (e.g. Farming, Cooking, Delivery)

* **description**

  * Human-readable text for UI / debugging only

* **workstation_type**

  * The type of workstation required to execute this job

* **input_transformation_rule**

  * A static rule or lookup table defining how inputs are transformed into outputs
  * May be implemented as a function pointer, enum, or external LUT

* **base_execution_time**

  * Time required to complete the job under normal conditions

* **other static metadata**

  * Any invariant data required by this job type

> **Invariant:** JobDefinitions are immutable and never scheduled directly.

---

## 2. Job Instances (Runtime Instructions)

A **JobInstance** represents a single executable occurrence of a job.

Only JobInstances participate in scheduling, reservation, and execution.

### JobInstance Properties

* **definition_id**

  * Reference to the JobDefinition template

* **state**

  * One of: `Pending`, `Ready`, `Claimed`, `Executing`

* **reservations**

  * Handles to all reserved inputs and output buffer slots

* **time_remaining**

  * Remaining execution time

* **reschedule** (unsigned integer)

  * Number of times this job should re-spawn itself after completion
  * `0` → one-shot job
  * `>0` → reschedule and decrement
  * `UINT_MAX` → effectively infinite (jam-safe replacement for true forever jobs)

> **Invariant:** JobInstances carry all runtime state. No scheduling intent is stored outside the instance.

---

## 3. Job Readiness and Reservation

A job may only become **Ready** if *all required resources can be atomically reserved*.

This rule prevents:

* Input duplication
* Phantom throughput
* Race conditions between NPCs

### Production Job Readiness

A production JobInstance becomes Ready when:

* Required inputs exist in the workstation input buffer
* Output buffer has free capacity
* All required inputs and one output slot are successfully reserved atomically

If any reservation fails, the job remains Pending.

### Delivery Job Readiness

A delivery JobInstance becomes Ready when:

* Source output buffer contains an item
* Destination input buffer has free capacity
* The source item and destination buffer slot are both reserved atomically

---

## 4. NPC Job Claiming

NPCs act as generic functional units.

* NPCs do **not** care about masks when selecting jobs
* NPCs do **not** have job affinity
* NPCs select jobs purely based on availability and proximity

### Job Selection Policy (Minimal)

1. If an immediate continuation job is available (see Section 6), claim it
2. Otherwise, select the nearest Ready job
3. If no jobs are Ready, remain Idle

---

## 5. Job Execution Lifecycle

### Standard Lifecycle

1. **Pending**

   * Job exists but inputs are not fully available

2. **Ready**

   * All inputs and output slots are reserved

3. **Claimed**

   * An NPC has committed to executing the job

4. **Executing**

   * NPC is performing work at the workstation

5. **Completion**

   * Inputs are consumed
   * Output is committed
   * Reservations are released

---

## 6. Rescheduling and Continuous Jobs

Continuous jobs are implemented via **instance-level rescheduling**, not special job types.

### Rescheduling Semantics

* Rescheduling is controlled solely by the JobInstance `reschedule` counter
* JobDefinitions are not aware of rescheduling

### On Job Completion

When a JobInstance `J` completes:

1. Release all reservations
2. If `J.reschedule == 0`:

   * Destroy `J`
3. If `J.reschedule > 0`:

   * Create a new JobInstance `J'`:

     * Same JobDefinition
     * `reschedule = J.reschedule - 1`
     * Fresh runtime state

### Immediate Continuation Check (Fast Path)

After creating `J'`, the system performs a **one-off continuation check**:

* If:

  * Inputs for `J'` are immediately available
  * Output buffer has space
  * Same workstation still exists
  * The NPC that completed `J` is idle

* Then:

  * Reserve inputs and output
  * NPC immediately claims `J'`
  * `J'` does **not** enter the global job list

If any condition fails, `J'` enters the global job list normally.

> **Important:** This continuation check is *momentary*. No priority or preference is stored if it fails.

---

## 7. Design Principles (Non‑Negotiable)

* JobDefinitions are immutable templates
* JobInstances are the only schedulable entities
* All readiness requires atomic reservation
* Continuous work uses instance-level rescheduling
* No persistent NPC–job affinity
* No hidden scheduling state

---

## 8. Summary

This job system ensures:

* Deterministic scheduling
* Safe automation at scale
* Natural continuous work behavior
* Clear congestion and backpressure
* Zero micromanagement

The system defines **when work happens**, not **what work means**. Semantic interpretation is handled entirely by other systems (masks, consumers, rooms).
