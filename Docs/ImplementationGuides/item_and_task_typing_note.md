# Item Types & Work System Implementation Guide

This document defines how **items**, **processing**, **perception**, and **work** interact in the system.
The goal is to guarantee *legal gameplay*, *correct routing*, and *NPC ignorance of physical truth*, while remaining jam‑friendly and extensible.

---

## 1️⃣ Base Items & Processing / Refinement Methods

### 1.1 Base Item Types

Base items represent the **material category** of an item.
They do **not** imply usability or intent.

```csharp
enum BaseItemType {
    Crop,
    Ore,
    Wood
}
```

Base item types:

- Are immutable
- Never change during gameplay
- Are never used by NPC logic or routing

---

### 1.2 Processing / Refinement Methods

Processing methods represent **actions applied to items** that transform their state.

```csharp
enum RefinementMethod {
    Cook,
    Extract,
    Smelt
}
```

These methods:

- Are applied at specific work stations
- Are the only way an item’s processing state can change
- Are defined independently of base item type

---

### 1.3 Processing State

Processing state represents **what has been done to the item**.

```csharp
enum ProcessingState {
    Raw,
    Cooked,
    Burnt,
    Extracted,
    Smelted,
    Ruined
}
```

Important properties:

- Exactly one processing state per item
- Total and closed set
- “Ruined” is a sink state
- "Burnt" and "Smelted" are terminal but stable

---

## 2️⃣ Transition Matrix (Authoritative Refinement Rules)

All refinement is governed by a **total transition matrix**.

There are:

- no conditionals
- no special cases
- no illegal transitions

### 2.1 Transition Table (Design View)

| Current \ Process | Cook | Extract | Smelt |
|------------------|------|---------|-------|
| **Raw**          | Cooked | Extracted | Smelted |
| **Cooked**       | Burnt | Ruined | Ruined |
| **Burnt**        | Burnt | Ruined | Burnt |
| **Extracted**    | Ruined | Ruined | Ruined |
| **Smelted**      | Ruined | Ruined | Smelted |
| **Ruined**       | Ruined | Ruined | Ruined |

This table is **the single source of truth**.

---

### 2.2 Transition Matrix (Code)

```csharp
static class RefinementTable {

    static readonly ProcessingState[,] Table = {
        // Cook                     Extract                   Smelt
        { ProcessingState.Cooked,    ProcessingState.Extracted, ProcessingState.Smelted }, // Raw
        { ProcessingState.Burnt,     ProcessingState.Ruined,    ProcessingState.Ruined  }, // Cooked
        { ProcessingState.Burnt,     ProcessingState.Ruined,    ProcessingState.Burnt   }, // Burnt
        { ProcessingState.Ruined,    ProcessingState.Ruined,    ProcessingState.Ruined  }, // Extracted
        { ProcessingState.Ruined,    ProcessingState.Ruined,    ProcessingState.Smelted }, // Smelted
        { ProcessingState.Ruined,    ProcessingState.Ruined,    ProcessingState.Ruined  }, // Ruined
    };

    public static ProcessingState Refine(
        ProcessingState current,
        RefinementMethod method
    ) {
        return Table[(int)current, (int)method];
    }
}
```

Only refinement systems may call this.

---

## 3️⃣ ItemPromise / ItemAlias (Perceived Item Types)

### 3.1 Concept

**ItemPromise** (also referred to as *ItemAlias*) represents:

> What the system *claims* this item is for routing, requests, and delivery.

This is **not derived by NPCs**.
It is **declared and trusted**.

There is a **one‑to‑one mapping** between:

- ItemPromise
- Perceived item mask
- Delivery endpoint types

**ItemPromise** comes from labels such as masks or dedicated containers.
They are **never** derived from BaseItemType or ProcessingState.

---

### 3.2 ItemPromise Type

```csharp
enum ItemPromise {
    Crop,
    Meal,
    Medicine,
    Metal,
    Ore,
    Wood
}
```

ItemPromise:

- Is the only item identity NPCs can see
- Represents *expected legal combinations*
- Is a **subset** of all (Base × State) combinations
- Corresponds to “what a masked carrier claims to be delivering”

---

### 3.3 Mapping Physical Truth -> ItemPromise

This mapping:

- Covers all the legal or expected combinations of BaseItemType and ProcessingState
- Is used for UI clarity only

Example conceptual mapping:

| Base | State | ItemPromise |
|----|------|-------------|
| Crop | Raw | Crop |
| Crop | Cooked | Meal |
| Crop | Extracted | Medicine |
| Ore | Raw | Ore |
| Ore | Smelted | Metal |
| Wood | Raw | Wood |

This table defines **what combinations are considered valid endpoints**.

Anything not mapped is **not promised** and thus never requested.

---

### 3.4 Why This Exists

ItemPromise is:

- The **delivery contract**
- The **routing vocabulary**
- The **intent surface**

NPCs do not understand items — they understand promises.

---

## 4️⃣ Related Systems

---

## 4.1 Work/Task Types

Work is modeled as **ScriptableObject‑defined tasks**.

### ✅ Architectural Choice: Inheritance Hierarchy

We use **multiple ScriptableObject types inheriting from a common base**.

**Reasoning:**

- Strongly different semantics
- Clear editor UX
- Fewer nullable fields
- Safer iteration under time pressure

---

### 4.1.1 Base Work Definition

```csharp
abstract class WorkDefinition : ScriptableObject {
    public string DisplayName;
    public string Description;
    public float Duration;
    public WorkStationDefinition RequiredWorkStation;
}
```

---

### 4.1.2 Production Work

Generates items from nothing.

```csharp
class ProductionWork : WorkDefinition {
    public BaseItemType produces;
    // ProcessingState always Raw
}
```

Examples:

- Farming
- Lumberjacking
- Mining

No inputs. No promises required.

---

### 4.1.3 Refinement Work

Consumes an item and produces another.

```csharp
class RefinementWork : WorkDefinition {
    public RefinementMethod method;
}
```

- Physical legality enforced by the transition matrix

---

### 4.1.4 Delivery Work

Moves an item **based on ItemPromise only**.

```csharp
class DeliveryWork : WorkDefinition {
    public ItemPromise requiredPromise;
}
```

Delivery:

- Picks up from a labeled source (room / carrier)
- Drops at a compatible station or consumption area
- Never inspects physical truth

## Task Definition (Behavior Authority)

**TaskDefinitions are ScriptableObjects that define *what work happens*.**
They are the single source of truth for task behavior and execution rules.

A task defines:

1. **Duration** — how long the work takes.
2. **Required Work Station** — a `WorkStationDefinition`, or `null` if the task does not require a station (e.g. delivery).
3. **Readiness Logic** — a `ReadyCheck` that validates whether the task can begin.
4. **Reservation Logic** — a `Reserve` step that claims all needed resources atomically.
5. **Task‑specific data** — production output, refinement method, delivery promise, etc.

Tasks do **not** define:

- input buffer size
- accepted item types
- physical capacity constraints

Those are enforced by work stations.

---

## Work Station Definition (Physical Constraint Authority)

**WorkStationDefinitions are ScriptableObjects that define *what a place can hold and accept*.**
They represent physical affordances, not behavior.

A work station defines:

1. **Input Buffer Size** — how many items can be stored simultaneously.
2. **Accepted ItemPromise** — the perceived item type the station is willing to accept.
3. **Structural Properties** — whether it accepts input, produces output, or is output‑only.

Work stations do **not** define:

- task duration
- refinement logic
- production rules
- AI behavior

They never decide *what happens* to items — only *whether items may be present*.

---

## Separation Invariant (Non‑Negotiable)

> **Tasks decide *what work occurs*.
> Work stations decide *what can physically sit there*.**

No property may be duplicated across both definitions.

If changing a value alters **task behavior**, it belongs in the **TaskDefinition**.
If changing a value alters **capacity or acceptance**, it belongs in the **WorkStationDefinition**.

---

## Runtime Interpretation

- Scene objects reference a `WorkStationDefinition` and act as thin containers.
- NPCs select and execute `TaskDefinitions`.
- Tasks reference stations only to validate availability.
- Stations never know *why* an item is present — only *whether it is allowed*.

---

## Jam‑Safe Outcome

This separation:

- prevents rule duplication
- keeps NPC logic promise‑only
- allows rapid content authoring via ScriptableObjects
- ensures physical legality without coupling behavior to space

This is the **minimal correct split** for a fast, maintainable system.
