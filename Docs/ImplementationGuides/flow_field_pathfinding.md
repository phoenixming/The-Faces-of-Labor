# Lazy Flow‑Field Pathfinding

## Goal

Implement a **lazy, incremental flow‑field pathfinding system** that:

- Supports unbounded maps
- Computes paths only when needed
- Shares work across agents
- Reuses partial computation
- Invalidates safely on map changes

Agents move by **reading precomputed directions** from the grid.
No per‑agent pathfinding is allowed.

---

## Assumptions (Do Not Violate)

- Grid‑based world
- 4‑directional (Manhattan) movement
- All walkable cells have uniform cost
- Obstacles are binary (walkable / blocked)
- One destination cell per flow field
- Expansion buffer is configurable (10–100 cells)

---

## Core Concepts

- One **FlowField** exists per destination cell
- A FlowField owns:
  - A dictionary of flow directions
  - A stored frontier set
  - A square (Manhattan) boundary
- Fields expand **lazily** when an agent requests a path outside current coverage
- Fields are invalidated when walls change

---

## Data Structures

### Direction

```csharp
enum Direction { Up = 0, Down = 1, Left = 2, Right = 3 }
Vector2Int[] directions = {
    new Vector2Int(0, 1),  // Up
    new Vector2Int(0, -1), // Down
    new Vector2Int(-1, 0), // Left
    new Vector2Int(1, 0)   // Right
};
```

---

### FlowField

```csharp
class FlowField {
    Vector2Int destination;

    Dictionary<Vector2Int, Direction> flow;

    Queue<Vector2Int> storedFrontier;

    int boundaryRadius;
}
```

Notes:

- `flow` stores only **reachable, processed cells**
- Unvisited cells are **not stored**
- Unreachable cells are **implicitly known** by boundary + missing flow entry

---

## Cell State (Implicit, Do Not Encode)

| Condition | Meaning |
|----|----|
| Cell in `flow` | Reachable, usable |
| Cell inside boundary but not in `flow` | Unreachable |
| Cell in `workingFrontier` | Discovered, pending |
| Cell in `storedFrontier` | Discovered, outside boundary |
| Cell in nothing | Unknown |

---

## Boundary Definition

- Shape: square using Manhattan distance
- A cell is inside the boundary if:

```csharp
abs(x - destination.x) + abs(y - destination.y) <= boundaryRadius
```

---

## System API

### Public Entry Point

```csharp
Vector2Int GetDirection(Vector2Int start, Vector2Int destination)
```

Returns:

- A direction to move
- `Vector2Int.zero` if unreachable

---

## Field Lifecycle

### 1. Field Creation

When a destination is first requested:

```csharp
field.destination = destination
field.flow = empty dictionary
field.storedFrontier = empty
field.boundaryRadius = 0
```

The destination cell itself has no direction.

---

### 2. Coverage Check

If:

- `start` is inside boundary

Then:

- If `flow` contains `start` → return direction
- Else → return unreachable

No computation required.

---

### 3. Boundary Expansion

If `start` is outside boundary:

```csharp
boundaryRadius =
    ManhattanDistance(destination, start)
    + expansionBuffer
```

`expansionBuffer` is configurable (10–100 cells).

---

### 4. Field Expansion (BFS)

Use **only `workingFrontier`**.

#### Expansion Loop

```pseudo
while workingFrontier not empty:
    cell = pop one from workingFrontier
    if cell is blocked → continue

    for each neighbor of cell:
        if neighbor is blocked → continue
        if neighbor already in flow → continue

        if neighbor inside boundary:
            flow[neighbor] = direction pointing toward cell
            add neighbor to workingFrontier
        else:
            add neighbor to storedFrontier
```

Movement is 4‑way only.

---

### 5. Stop Condition

Stop expanding when **workingFrontier is empty**:

At this point:

- All reachable cells inside boundary have flow directions
- Any cell inside boundary not in `flow` is unreachable

---

### 6. Frontier Persistence

Before expansion, create a temporary working frontier set and move stored
frontier to it:

```csharp
workingFrontier = new();
swap(workingFrontier, storedFrontier);
storedFrontier.clear();
```

This resumes cleanly from past expansions.

---

### 7. Answer Request

Now `start` is guaranteed inside boundary.

- If `flow` contains `start` → return direction
- Else → return unreachable

---

## Map Changes (Invalidation Rules)

### Wall Added

Invalidate a FlowField if:

- Wall cell is inside field boundary
- AND wall cell exists in `flow`

If invalidated:

- Delete the `DirectionField` completely
- Field will be rebuilt lazily on next request

---

### Frontier Validation on Resume

When resuming expansion:

- Remove frontier cells that are now blocked
- Keep the rest

---

### Wall Removed

Invalidate a FlowField if:

- Wall cell intersects boundary (plus 1)

This avoids missing newly opened paths.

Do not attempt partial repair.

---

## Pathfinding System Responsibilities

- Own all FlowFields
- Route agent queries to fields
- Expand fields lazily
- Handle invalidation on map changes

---

## Agent Responsibilities

Agents must:

- Query the pathfinding system every step
- Move according to returned direction

Agents must NOT:

- Cache paths
- Recompute paths
- Know about obstacles

---

## Debug Visualization (Strongly Recommended)

Render:

- Boundary square
- Flow arrows
- Working vs stored frontier cells

This is critical for correctness and tuning.

---

## Performance Characteristics

| Scenario | Cost |
|----|----|
| First agent to destination | O(expanded area) |
| Subsequent agents | O(1) |
| Large maps | Safe (sparse storage) |
| Many agents | Excellent |

---

## Non‑Goals (Do Not Implement)

- Diagonal movement
- Weighted costs
- Partial invalidation on wall removal
- Global precomputation
- Per‑agent path memory

---

## Final Summary

This system implements a **lazy, resumable, BFS‑based flow‑field pathfinding architecture** that computes only what agents touch, shares work across agents, and remains correct under dynamic map changes — ideal for jam‑scale logistics or simulation games.
