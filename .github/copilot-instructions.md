# The Faces of Labor - Copilot Instructions

## Project Overview

**The Faces of Labor** is a Unity strategy game where players design and maintain a facility with autonomous NPCs. NPCs autonomously select tasks, navigate the facility, and complete jobs. The game's core mechanic: **NPCs' perceived identity (via masks) differs from their actual work**, creating emergent social complexity.

## Architecture Map

### Core Systems (`Assets/Scripts/Core/`)

**Three Singleton Managers:**
- `GridSystem`: Authoritative spatial grid for pathfinding, infrastructure placement, wall tracking. Speaks to `PathfindingSystem` and `TaskManager` via events.
- `TaskManager`: Manages task lifecycle (Pending → Ready → Claimed → Executing). Tasks transition to Ready when free workstations become available. NPCs claim from Ready pool.
- `PathfindingSystem`: Lazy flow-field pathfinding using incremental expansion. Caches flow fields per destination; invalidates on wall changes. Agents share computed paths.

**Infrastructure Hierarchy:**
- `Infrastructure` (base): All scene entities (workstations, mask stations, walls) inherit. Uses GUID-based event registration for decoupling. Events trigger grid registration and task manager updates.
  - `WorkStation` (subclass): Thin container for `WorkStationDefinition`. Manages input buffer with reservation system (reserve slots before adding items). Never decides *what work* occurs—only whether items may sit there.
  - `MaskStation`: Changes NPC perceived identity. Does NOT control what NPCs do.
  - `Wall`: Blocks walkability; triggers pathfinding invalidation.

**Task & Definition System:**
- `TaskDefinition` (ScriptableObject base): Immutable task metadata. Specifies required workstation type and execution time.
  - Subclasses: `ProductionTaskDefinition`, `RefinementTaskDefinition`, `DeliveryTaskDefinition`, `ConsumptionTaskDefinition`
- `TaskInstance`: Runtime task instance. Tracks state (Pending, Ready, Claimed, Executing) and assigned workstation.
- `WorkStationDefinition`: Defines physical constraints: accepted `ItemPromise`, input buffer size.

**Type System:**
- `BaseItemType`: Material category (Crop, Ore, Wood, None). Never used by NPC logic directly.
- `ProcessingState`: Transformation state (Raw, Cooked, Burnt, Extracted, Smelted, Ruined).
- `RealItem`: Immutable struct combining BaseItemType + ProcessingState.
- `RefinementTable`: Authoritative state transition matrix (no conditionals, no illegal transitions).

### Key Design Principles

1. **Definition ≠ Instance**: Scene objects (WorkStation, TaskInstance) are thin wrappers around ScriptableObject definitions. Definitions are immutable; instances hold mutable state.
2. **Separation of Concerns**:
   - Tasks define *what work*; stations define *what can sit there*.
   - Masks define *perceived identity*; they don't control action.
3. **Event-Based Decoupling**: Infrastructure broadcasts start/destroy events. Systems subscribe, preventing tight coupling.
4. **Atomic Operations**: Workstation reservations ensure buffer safety. Always call `TryReserveSlot()` before `AddItem()`.
5. **Lazy Initialization**: Flow fields expand on demand, never pre-computing entire map.

## Developer Workflows

### Adding a New Task Type

1. Create `MyTaskDefinition : TaskDefinition` in `Assets/Scripts/Core/Definitions/`.
2. Populate base fields: `Type`, `DisplayName`, `RequiredWorkstationType`.
3. Create ScriptableObject asset via CreateAssetMenu (e.g., `Assets/Definitions/Tasks/MyTask.asset`).
4. NPCs claim from `TaskManager.readyTasks`. Implement execution logic in NPC controller.

### Adding a New Workstation Type

1. Create `MyWorkStation : WorkStation` in `Assets/Scripts/Core/` (optional; use base if no custom behavior).
2. Create `MyWorkStationDefinition` asset specifying `Type`, `AcceptsInput` (ItemPromise), `InputBufferSize`.
3. Drag scene prefab into level. `Infrastructure.InfrastructureStarted` event fires → `GridSystem` registers placement → `TaskManager` learns it's free.
4. When tasks of matching type enter Ready state, workstation is reserved.

### Modifying Task State Transitions

Edit `TaskManager.PromoteTasks()` (called each frame). Current flow:
- Pending → Ready: when free workstation of matching type exists
- Ready → Claimed: when NPC claims task
- Claimed → Executing: when NPC starts execution
- Executing → complete: NPC-driven (not in TaskManager)

**Never** bypass reservation system; always call `TryReserveSlot()` before `AddItem()`.

### Debugging Pathfinding

`PathfindingSystem` has debug visualization in inspector:
- `debugDestination`: Target cell for flow field display
- `debugCoverPoint`: Ensures field covers this point before drawing
- `showBoundary` / `showStoredFrontier`: Visualize expansion state

Check `GridSystem.walls` (HashSet) for incorrect walkability. Wall changes fire `GridSystem.OnWalkabilityChanged` → `PathfindingSystem.OnWalkabilityChanged` → field invalidation.

## Code Patterns

**Singleton Access:** `GridSystem.Instance`, `TaskManager.Instance`, `PathfindingSystem.Instance` (safe after Awake/Start).

**Event Subscription:**
```csharp
// In Awake/Start:
Infrastructure.InfrastructureStarted += OnInfrastructureStarted;
GridSystem.OnWalkabilityChanged += OnWalkabilityChanged;

// Handler signature:
private void OnInfrastructureStarted(Infrastructure infra) { }
```

**Workstation Buffer Safety:**
```csharp
if (station.TryReserveSlot(1)) {
    station.AddItem(item, 1);
} else {
    // Buffer full; try next frame
}
```

**Pathfinding Query:**
```csharp
Vector2Int direction = PathfindingSystem.Instance.GetDirection(start, destination);
if (direction != Vector2Int.zero) {
    // Move toward destination
}
```

## Critical Files by Purpose

| Purpose | File(s) |
|---------|---------|
| Spatial logic | `GridSystem.cs`, `PathfindingSystem.cs` |
| Task lifecycle | `TaskManager.cs`, `TaskInstance.cs`, `TaskDefinition.cs` |
| Item state machine | `SharedTypes.cs` (`RefinementTable`) |
| Infrastructure events | `Infrastructure.cs`, `WorkStation.cs` |
| NPC state & masks | `NPCCore.cs`, `MaskStation.cs` |
| Game design | `Docs/GameSystem.md`, `Docs/GameContent.md` |

## Testing & Iteration

- Use `DemoManager.cs` for test scenarios.
- Monitor `TaskManager.PendingCount`, `ReadyCount`, `ClaimedCount`, `ExecutingCount` in inspector.
- Verify workstation types match task requirements (typos break task promotion).
- Check grid for isolated regions; unreachable destinations never compute paths.

---

**Last Updated:** February 2026
**Contact:** See Git history for author context.
