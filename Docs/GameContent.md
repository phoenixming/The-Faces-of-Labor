# Content Description Document

## Game Goal

The goal is to keep the settlement alive **for as long as possible**.

The settlement fails when NPC survival needs can no longer be met due to:

- Resource shortages
- Delivery breakdowns
- Environmental collapse

There is no win condition.

---

## Core Survival Needs

Each NPC has three needs that decay over time:

| Need   | Restored By |
| ------ | ----------- |
| Hunger | Eating food |
| Warmth | Global base temperature |
| Health | Medicine |

Failure to satisfy needs leads to sickness, reduced productivity, and death.

---

## Global Systems

### Base Temperature

- Global value shared by all NPCs
- Slowly decreases over time
- Raised or maintained by fuel consumption at furnaces
- Affects warmth and health drain rates

---

## Job System Overview

All NPCs pull jobs from a **global job list**.

Jobs are:

- Not assigned manually
- Not restricted by masks
- Selected based on availability and proximity

### Job Categories

1. **Production Jobs**
   Create resources at work sites.

2. **Delivery Jobs**
   Move items between locations.

3. **Service Jobs (Implicit)**
   NPCs consuming resources (eating, healing).

---

## Production Jobs

Production jobs create items and place them in a local output buffer.

| Job | Work Site | Input | Output |
| --- | --------- | ----- | ------ |
| Farming | Farm | — | Crops |
| Cooking | Kitchen | Crops | Food |
| Lumberjacking | Forest | — | Wood |
| Mining | Mine | — | Ore |
| Processing | Processor | Ore | Matter |
| Medical Processing | Clinic | Crops | Medicine |

Notes:

- Any NPC can perform any production job
- Production sites do not check masks
- Production speed may degrade with poor health

---

## Delivery Jobs

Delivery is the only way items move between systems.

### Delivery Job Definition

A delivery job specifies:

- Source location
- Destination location
- Item type

Delivery jobs:

- Do not require a mask
- Never fail
- Never reject items
- May cause accidental mask swaps during transit

### Example Delivery Jobs

- Deliver Crops -> Kitchen
- Deliver Food -> Dining Hall
- Deliver Wood -> Furnace
- Deliver Medicine -> Medical Station
- Deliver Matter -> Machines

---

## Consumers

Consumers automatically use items delivered to them.

Consumers never:

- Reject items
- Check NPC identity
- Assign jobs

### Consumer List

| Consumer | Consumes | Effect |
| -------- | -------- | ------ |
| Dining Hall | Food | Restores hunger |
| Furnace / Fireplace | Wood | Raises base temperature |
| Medical Station | Medicine | Restores health |
| Auto‑Machines | Matter | Enable production / construction |
| Recruitment Machine | Matter | Creates NPCs |

---

## NPC Consumption Behavior

NPCs:

- Route themselves to consumers when a need is unmet
- Consume resources if available
- Suffer penalties if consumers are empty or unreachable

---

## Production Chains

All production follows this structure:

```
Production -> Output Buffer -> Delivery -> Consumer / Next Stage
```

### Food Chain

```
Farm -> Crops -> Kitchen -> Food -> Dining Hall -> NPCs
```

### Warmth Chain

```
Forest -> Wood -> Furnace -> Base Temperature
```

### Medicine Chain

```
Farm -> Crops -> Clinic -> Medicine -> Medical Station -> NPCs
```

### Matter Chain

```
Mine -> Ore -> Processor -> Matter -> Machines / Recruitment
```

---

## Failure Conditions (Systemic)

The game fails when:

- Food delivery collapses -> starvation
- Fuel delivery collapses -> freezing
- Medicine delivery collapses -> sickness
- Matter delivery collapses -> no recovery

Failure is gradual and visible.

---

## Player Control Surface

The player influences the system by:

- Placing work sites and consumers
- Enabling or disabling jobs
- Adding or removing delivery capacity
- Shaping movement through layout
- Placing mask stations

The player does **not**:

- Assign NPCs directly
- Control delivery routes manually
- Override consumption rules

---

## Content Scope (Jam‑Safe)

Minimum required content:

- 4 production jobs
- 1 delivery job type
- 3 consumer types
- 1 global pressure (temperature)

Optional expansions:

- Waste / sanitation
- Power / electricity
- Additional medicine types

---

## Design Summary

This is a survival logistics game where:

- Production is simple
- Delivery is the bottleneck
- Space and flow determine success
- Systems fail gradually, not instantly

The content is intentionally minimal to support emergence.
