# Grid, Construction, and Pathfinding Implementation Guide

This document specifies the **strict grid system** that underpins construction, movement, and pathfinding. It is a foundational system used by job scheduling, logistics, and mask interaction, but is defined independently.

This game uses a **strict logical grid** with **4-way movement**. Unity is used only for rendering and animation; all spatial reasoning happens on the grid.

---

## 1. Grid Model (Authoritative)

### Grid Coordinates

- The world is represented as a 2D grid of integer coordinates `(x, y)`
- All construction, routing, and reachability checks operate on grid coordinates
- World-space (float) positions are derived purely for visuals

### Movement Rules

- NPCs move **orthogonally only** (up, down, left, right)
- No diagonal movement exists at the logic level
- Visual interpolation may smooth paths, but must not affect logic

### Occupancy Rules

- Each grid cell may contain **at most one infrastructure type**
- NPCs **do not occupy grid cells**
- NPCs do not collide with each other

This keeps pathfinding and congestion reasoning deterministic and simple.

---

## 2. Infrastructure Types

Each grid cell may contain **zero or one** infrastructure entity.

Infrastructure entities may span multiple grid cells.

### 2.1 Walls

- Occupy one or more grid cells
- **Always block movement**
- Pathfinding treats wall cells as impassable

Walls define spatial constraints and are the primary cause of routing failures.

---

### 2.2 Mask Stations

- Occupy one grid cell
- **Never block movement**
- NPCs automatically swap masks when passing through

Mask stations are:
- Spatially meaningful
- Traversable
- Treated as normal walkable cells for pathfinding

---

### 2.3 Workstations / Autonomous Machines

Includes:
- Production stations
- Consumer stations
- Autonomous machines

Properties:
- May occupy one or more grid cells
- May be configured as:
  - **Blocking** (solid)
  - **Non-blocking** (walk-through)

Blocking behavior is a per-station property and does not affect job logic.

---

## 3. Construction Rules

### Placement

- All infrastructure snaps to grid
- Placement validates:
  - Target cells are within bounds
  - Target cells are unoccupied

For multi-cell infrastructure:
- All required cells must be valid before placement succeeds

### Removal

- Removing infrastructure immediately frees grid cells
- Pathfinding must update accordingly
- Active jobs depending on removed infrastructure are handled by the job system

---

## 4. Walkability Map

The grid maintains a **walkability map**:

- `walkable[x][y] = true` if:
  - Cell has no infrastructure
  - OR infrastructure is marked non-blocking

- `walkable[x][y] = false` if:
  - Cell contains a wall
  - OR infrastructure marked blocking

This map is the sole input to pathfinding.

---

## 5. Pathfinding System

### Algorithm

- Use a simple grid-based pathfinder (BFS or A*)
- Manhattan distance heuristic (if A*)
- Uniform movement cost

Given the grid scale and NPC count, simplicity is preferred over optimality.

### Path Requests

NPCs request paths for:
- Claiming jobs
- Moving to workstations
- Performing delivery jobs

If no valid path exists:
- Job claim fails
- NPC abandons the job attempt
- Job returns to Ready state (or remains Pending)

---

## 6. Dynamic Updates

### Infrastructure Changes

When infrastructure is placed or removed:

- Update walkability map immediately
- Existing NPC paths may:
  - Continue if still valid
  - Repath on failure or blockage

No global path cache is required.

---

## 7. Design Constraints (Locked)

- Grid logic is authoritative
- Movement is 4-way only
- NPCs do not occupy or reserve tiles
- No diagonal shortcuts at logic level
- Construction and pathfinding share the same grid

---

## 8. Design Intent

This grid system exists to:

- Make spatial problems **predictable and explainable**
- Allow players to reason about flow and congestion
- Keep pathfinding deterministic and debuggable
- Avoid hidden behavior from engine-level navigation systems

Visual smoothness must never override logical correctness.
