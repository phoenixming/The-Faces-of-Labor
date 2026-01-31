# Game Design Overview (High‑Level)

## What the Game Is

The player designs and maintains a growing facility populated by autonomous NPCs.
NPCs select work from a global task list, move through the facility using simple pathfinding, and complete jobs at designated work areas.

The player does **not** command NPCs directly.
Instead, the player:

- Defines which tasks exist
- Builds infrastructure
- Shapes movement and interaction through spatial design

The challenge of the game comes from **misinterpretation**: NPCs perform jobs correctly, but their *identity* is inferred by others based on masks they wear — not on what they actually do.

---

## Core Concept: Work vs Identity

### Work (Objective Reality)

- Jobs are real and deterministic.
- Tasks are defined by the player (ONI‑style).
- Any NPC can perform any task.
- Completing a task always produces the same result.

### Identity (Perceived Reality)

- NPC identity is inferred by other NPCs.
- Identity is determined **only** by the mask currently worn.
- Identity affects who can interact with whom and which stations will accept deliveries.

**Key Rule:**
Masks do **not** control what an NPC does.
Masks control how other NPCs interpret that NPC.

---

## Core Gameplay Loop

1. Player defines tasks and builds infrastructure
2. NPC selects an available task from the task list
3. NPC routes to the task location
4. NPC may swap masks while moving through the facility
5. NPC completes the task and produces a resource
6. NPC routes to a consumer or destination
7. System stabilizes or destabilizes → player intervenes

The loop is continuous and never fully “solved.”

---

## Player Role

The player:

- Places buildings and machines
- Defines which tasks are active
- Controls flow through spatial layout
- Adds or removes mask stations
- Fixes emergent failures

The player never:

- Assigns NPCs to jobs
- Commands movement
- Selects masks directly on NPCs

---

## NPC Behavior Model

NPCs are:

- Autonomous
- Predictable
- Intentionally simple

NPC decision-making:

- Select nearest valid task
- Route using deterministic pathfinding
- Interact with stations encountered along their path

Complex behavior emerges from **system interaction**, not AI sophistication.

---

## Infrastructure Overview

### 1. Task Stations (Work Sites)

- Represent locations where jobs are performed
- Linked to task types in the global task list
- Do not care about masks
- Always produce the same output

Examples:

- Material Printer
- Mask Fabricator
- Recruiter
- Kitchen

---

### 2. Mask Stations (Identity Modifiers)

- Physical stations placed by the player
- NPCs automatically swap masks when passing through
- Masks overwrite perceived identity

Mask stations:

- Do not assign jobs
- Do not produce resources
- Exist only to influence downstream interactions

---

### 3. Consumer Stations (Interpretation Points)

Consumer stations:

- Accept NPC interactions based on **mask**
- Consume resources or trigger effects
- Never check what job the NPC performed

Examples:

- Dining Hall (accepts "chef" identity)
- Recruiter Input
- Storage Access
- Processing Machines

All interpretation logic lives here.

---

### 4. Recruitment Infrastructure

NPCs are created by machines, not commands.

- Recruitment machines:
  - Consume resources
  - Fill a progress bar
  - Emit a new NPC when full

NPCs are treated as throughput, not characters.

---

## Resource Model (Minimal)

### Explicit Resource

- **Matter** (e.g. “3D‑Print Material”)
  - Used for:
    - Building infrastructure
    - Producing masks
    - Recruiting NPCs

Produced by:

- Jobs at task stations

---

### Implicit Resources

- Masks (identity control)
- NPC count
- Spatial complexity

These are the primary balancing levers.

---

## Failure & Emergence

Failures occur when:

- NPCs wear incorrect masks
- Resources are delivered to the wrong consumers
- Routing causes unintended mask swaps
- Identity spreads incorrectly through the system

Failures are:

- Visible
- Spatial
- Fixable through redesign, not menus

---

## Simulation Philosophy (“Movie Set”)

The game simulates only what the player can meaningfully affect.

- Machines are simple timers + containers
- Resources have no quality or decay
- NPC logic is intentionally shallow

Complexity comes from **interaction**, not simulation depth.

---

## Design Constraints (Non‑Negotiable)

- Jobs must be mask‑agnostic
- Consumers must be mask‑sensitive
- Masks can only change at physical stations
- NPCs must not prefer tasks based on masks
- The player must never directly control NPCs

---

## Design Intent Summary

This is a game about:

- Planning work
- Shaping interpretation
- Managing systemic misunderstandings

The player builds order.
The system creates chaos.
The gameplay lives in between.
