# Task System Implementation Guide

This document specifies the **task scheduling and execution system** used by the game. It is intended as a standalone implementation reference and should be read independently from other systems (masks, resources, UI, etc.).

The task system is modeled after an **out-of-order (OoO) processor**:

* Tasks behave like instructions
* Inputs behave like operands
* NPCs behave like functional units
* Workstations behave like execution units
* Buffers behave like reservation stations

The system is fully autonomous: NPCs are never assigned manually, and players influence behavior only by enabling tasks and shaping infrastructure.

---

## 1. Task Definitions (Static Templates)

A **TaskDefinition** describes what a task *is*. It never changes at runtime and never appears directly in the task list.

TaskDefinitions act as immutable templates from which runtime task instances are created.

### TaskDefinition Properties

* **type**

  * Logical task category (e.g. Farming, Cooking, Delivery)

* **description**

  * Human-readable text for UI / debugging only

* **workstation_type**

  * The type of workstation required to execute this task

* **input_transformation_rule**

  * A static rule or lookup table defining how inputs are transformed into outputs
  * May be implemented as a function pointer, enum, or external LUT

* **base_execution_time**

  * Time required to complete the task under normal conditions

* **other static metadata**

  * Any invariant data required by this task type

> **Invariant:** TaskDefinitions are immutable and never scheduled directly.

---

## 2. Task Instances (Runtime Instructions)

A **TaskInstance** represents a single executable occurrence of a task.

Only TaskInstances participate in scheduling, reservation, and execution.

### TaskInstance Properties

* **definition_id**

  * Reference to the TaskDefinition template

* **state**

  * One of: `Pending`, `Ready`, `Claimed`, `Executing`

* **reservations**

  * Handles to all reserved inputs and output buffer slots

* **time_remaining**

  * Remaining execution time

* **respawn_count** (unsigned integer)

  * Number of times this task should re-spawn itself after completion
  * `0` → one-shot task
  * `>0` → respawn and decrement
  * `UINT_MAX` → effectively infinite (jam-safe replacement for true forever tasks)

> **Invariant:** TaskInstances carry all runtime state. No scheduling intent is stored outside the instance.

---

## 3. Task Readiness and Reservation

A task may only become **Ready** if *all required resources can be atomically reserved*.

This rule prevents:

* Input duplication
* Phantom throughput
* Race conditions between NPCs

### Production Task Readiness

A production TaskInstance becomes Ready when:

* Required inputs exist in the workstation input buffer
* Output buffer has free capacity
* All required inputs and one output slot are successfully reserved atomically

If any reservation fails, the task remains Pending.

### Delivery Task Readiness

A delivery TaskInstance becomes Ready when:

* Source output buffer contains an item
* Destination input buffer has free capacity
* The source item and destination buffer slot are both reserved atomically

---

## 4. NPC Task Claiming

NPCs act as generic functional units.

* NPCs do **not** care about masks when selecting tasks
* NPCs do **not** have task affinity
* NPCs select tasks purely based on availability and proximity

### Task Selection Policy (Minimal)

1. If an immediate continuation task is available (see Section 6), claim it
2. Otherwise, select the nearest Ready task
3. If no tasks are Ready, remain Idle

---

## 5. Task Execution Lifecycle

### Standard Lifecycle

1. **Pending**

   * Task exists but inputs are not fully available

2. **Ready**

   * All inputs and output slots are reserved

3. **Claimed**

   * An NPC has committed to executing the task

4. **Executing**

   * NPC is performing work at the workstation

5. **Completion**

   * Inputs are consumed
   * Output is committed
   * Reservations are released

---

## 6. Respawning and Continuous Tasks

Continuous tasks are implemented via **instance-level respawning**, not special task types.

### Respawning Semantics

* Respawning is controlled solely by the TaskInstance `respawn_count` counter
* TaskDefinitions are not aware of respawning

### On Task Completion

When a TaskInstance `T` completes:

1. Release all reservations
2. If `T.respawn_count == 0`:

   * Destroy `T`
3. If `T.respawn_count > 0`:

   * Create a new TaskInstance `T'`:

     * Same TaskDefinition
     * `respawn_count = T.respawn_count - 1`
     * Fresh runtime state

### Immediate Continuation Check (Fast Path)

After creating `T'`, the system performs a **one-off continuation check**:

* If:

  * Inputs for `T'` are immediately available
  * Output buffer has space
  * Same workstation still exists
  * The NPC that completed `T` is idle

* Then:

  * Reserve inputs and output
  * NPC immediately claims `T'`
  * `T'` does **not** enter the global task list

If any condition fails, `T'` enters the global task list normally.

> **Important:** This continuation check is *momentary*. No priority or preference is stored if it fails.

---

## 7. Design Principles (Non‑Negotiable)

* TaskDefinitions are immutable templates
* TaskInstances are the only schedulable entities
* All readiness requires atomic reservation
* Continuous work uses instance-level respawning
* No persistent NPC–task affinity
* No hidden scheduling state

---

## 8. Summary

This task system ensures:

* Deterministic scheduling
* Safe automation at scale
* Natural continuous work behavior
* Clear congestion and backpressure
* Zero micromanagement

The system defines **when work happens**, not **what work means**. Semantic interpretation is handled entirely by other systems (masks, consumers, rooms).
