# Implementation Plan: Character & Item Managers

## Overview

Incrementally build the CharacterManager and ItemManager systems for a Unity 2D game. Start with the foundational Singleton fix and SpatialGrid data structure, then build each manager with registration, spatial queries, and integration with existing CharacterBase and Sword classes. Each step builds on the previous and wires into the existing codebase.

## Tasks

- [x] 1. Fix Singleton base class and create shared interfaces
  - [x] 1.1 Change `Singleton<T>.Awake()` from `private` to `protected virtual` in `Assets/_GAME/Scripts/Manager/Singleton.cs`
    - Ensure `GameManager` still compiles (it defines its own `Awake()` which currently hides the base — it should now use `override`)
    - Update `GameManager.Awake()` to call `base.Awake()` then run its own initialization
    - _Requirements: 9.3_

  - [x] 1.2 Create `IManagedUpdate` interface in `Assets/_GAME/Scripts/IManagedUpdate.cs`
    - Define `void ManagedUpdate(float deltaTime)` and `Vector3 Position { get; }`
    - _Requirements: 3.1, 3.2_

  - [x] 1.3 Create `ICollectibleItem` interface and `CollectibleItemType` enum in `Assets/_GAME/Scripts/ICollectibleItem.cs`
    - Define `Position`, `IsActive`, `GameObject`, `ItemType`, `OnSpawn(Vector3)`, `OnDespawn()`
    - Define `CollectibleItemType` enum with `Sword = 0`, `HealthPack = 1`, `SpeedBoost = 2`
    - _Requirements: 5.1, 5.4_

- [x] 2. Implement SpatialGrid<T> generic class
  - [x] 2.1 Create `SpatialGrid<T>` in `Assets/_GAME/Scripts/SpatialGrid.cs`
    - Implement constructor with `cellSize` parameter, cache `invCellSize = 1f / cellSize`
    - Implement cell key packing: `(long)cellX << 32 | (uint)cellY`
    - Implement `Add(T entity, Vector3 worldPosition)` — add to `cells` dictionary and `entityCells` tracking
    - Implement `Remove(T entity)` — remove from cell list and `entityCells`, pool empty lists
    - Implement `UpdatePosition(T entity, Vector3 newWorldPosition)` — move between cells only when cell key changes
    - Implement `Clear()` — reset all internal state
    - Implement `GetInRadius(Vector3 center, float radius, List<T> results)` — compute cell range, iterate overlapping cells, distance-squared check, clear results list before populating
    - Implement `GetNearest(Vector3 center, float radius, T exclude)` — find minimum-distance entity in radius
    - Implement `Count` property
    - Pool cell lists via `Stack<List<T>>` for reuse
    - Handle edge cases: NaN/Infinity positions (clamp + log warning), remove/update untracked entity (silently ignore)
    - _Requirements: 1.1, 1.2, 2.1, 2.2, 2.3, 2.4, 2.5, 5.1, 5.2, 6.1, 6.2_

  - [ ]* 2.2 Write property test: Spatial grid add/remove correctness
    - **Property 1: Spatial grid add/remove correctness**
    - **Validates: Requirements 1.1, 1.2, 5.1, 5.2, 5.4**

  - [ ]* 2.3 Write property test: Spatial grid position update correctness
    - **Property 2: Spatial grid position update correctness**
    - **Validates: Requirements 2.2, 3.3**

  - [ ]* 2.4 Write property test: Radius query matches brute-force scan
    - **Property 3: Radius query matches brute-force scan**
    - **Validates: Requirements 2.3, 2.4, 6.2**

  - [ ]* 2.5 Write property test: Nearest query correctness
    - **Property 6: Nearest query correctness**
    - **Validates: Requirements 4.1, 6.4, 8.1, 8.2**

- [x] 3. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement CharacterManager
  - [x] 4.1 Create `CharacterManager` in `Assets/_GAME/Scripts/Manager/CharacterManager.cs`
    - Extend `Singleton<CharacterManager>`, override `Awake()` calling `base.Awake()`
    - Add `[SerializeField] float gridCellSize = 5f` and `[SerializeField] bool persistAcrossScenes = false`
    - Initialize `SpatialGrid<CharacterBase>` in `Awake()`, call `DontDestroyOnLoad` if configured
    - Implement `Register(CharacterBase)` — add to `characters` list, `characterSet` HashSet, and grid; defer if `isUpdating`
    - Implement `Deregister(CharacterBase)` — remove from list, set, and grid; defer if `isUpdating`
    - Implement `Update()` — flush pending add/remove buffers, set `isUpdating = true`, iterate characters calling `ManagedUpdate(deltaTime)` and `grid.UpdatePosition()`, set `isUpdating = false`
    - Implement `CharacterCount` property
    - Implement `GetNearbyCharacters(position, radius, results)` — delegates to grid `GetInRadius`
    - Implement `GetNearestCharacter(position, radius, excludeSelf)` — delegates to grid `GetNearest`
    - Implement `GetCharactersInRadius(position, radius, results)` — delegates to grid `GetInRadius` then sorts results by distance ascending
    - Handle duplicate registration (silently ignore), deregister unregistered (silently ignore)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 9.1, 9.3, 9.4_

  - [ ]* 4.2 Write property test: Registration idempotence
    - **Property 4: Registration idempotence**
    - **Validates: Requirements 1.5**

  - [ ]* 4.3 Write property test: Character count invariant
    - **Property 5: Character count invariant**
    - **Validates: Requirements 1.3, 4.3**

  - [ ]* 4.4 Write property test: Sorted radius query ordering
    - **Property 7: Sorted radius query ordering**
    - **Validates: Requirements 4.2**

- [x] 5. Implement ItemManager
  - [x] 5.1 Create `ItemManager` in `Assets/_GAME/Scripts/Manager/ItemManager.cs`
    - Extend `Singleton<ItemManager>`, override `Awake()` calling `base.Awake()`
    - Add serialized fields: `gridCellSize`, `poolInitialSize`, `poolMaxSize`, `persistAcrossScenes`
    - Initialize `SpatialGrid<ICollectibleItem>` in `Awake()`, call `DontDestroyOnLoad` if configured
    - Pre-allocate object pool in `Awake()` based on `poolInitialSize`
    - Implement `RegisterItem(ICollectibleItem)` — add to `allItems` HashSet and grid
    - Implement `DeregisterItem(ICollectibleItem)` — remove from `allItems` and grid; silently ignore if not registered
    - Implement `RegisterDroppedSword(Sword)` — add to `droppedSwords` HashSet, also register as `ICollectibleItem`
    - Implement `DeregisterDroppedSword(Sword)` — remove from `droppedSwords`, also deregister as `ICollectibleItem`
    - Implement `SpawnItem(prefab, position)` — pull from pool or instantiate, call `OnSpawn`, register in grid
    - Implement `DespawnItem(item)` — call `OnDespawn`, deregister from grid, return to pool or destroy if pool full
    - Implement query methods: `GetNearbyItems`, `GetNearbySwords`, `GetNearestItem`, `GetNearestSword`, `GetNearestItemOfType`
    - Implement `ItemCount` and `DroppedSwordCount` properties
    - Handle null prefab (log warning, return null), despawn already-despawned (silently ignore)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4, 8.1, 8.2, 8.3, 8.4, 8.5, 9.2, 9.3, 9.5_

  - [ ]* 5.2 Write property test: Dropped sword tracking invariant
    - **Property 8: Dropped sword tracking invariant**
    - **Validates: Requirements 5.3, 8.3, 8.4**

  - [ ]* 5.3 Write property test: Filtered sword query correctness
    - **Property 9: Filtered sword query correctness**
    - **Validates: Requirements 6.3**

  - [ ]* 5.4 Write property test: Object pool bounded size
    - **Property 10: Object pool bounded size**
    - **Validates: Requirements 7.4**

  - [ ]* 5.5 Write property test: Spawn/despawn round-trip
    - **Property 11: Spawn/despawn round-trip**
    - **Validates: Requirements 7.1, 7.2**

- [x] 6. Checkpoint
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Integrate CharacterBase with IManagedUpdate and CharacterManager
  - [x] 7.1 Modify `Assets/_GAME/Scripts/CharacterBase.cs` to implement `IManagedUpdate`
    - Add `: IManagedUpdate` to class declaration
    - Add `public Vector3 Position => transform.position;` property
    - Move movement and input logic from `Update()` into `ManagedUpdate(float deltaTime)` — replace `Time.deltaTime` with the `deltaTime` parameter
    - Remove the existing `Update()` method (manager handles the update loop now)
    - Register with `CharacterManager.Instance.Register(this)` in `OnEnable()`
    - Deregister with `CharacterManager.Instance.Deregister(this)` in `OnDisable()`
    - _Requirements: 1.1, 1.2, 3.1, 3.2_

- [x] 8. Integrate Sword with ICollectibleItem and ItemManager
  - [x] 8.1 Modify `Assets/_GAME/Scripts/Sword.cs` to implement `ICollectibleItem`
    - Add `: ICollectibleItem` to class declaration
    - Implement `Vector3 Position => transform.position`
    - Implement `bool IsActive => gameObject.activeSelf`
    - Implement `GameObject GameObject => gameObject`
    - Implement `CollectibleItemType ItemType => CollectibleItemType.Sword`
    - Implement `OnSpawn(Vector3 position)` — set position, activate, set state to `Dropped`
    - Implement `OnDespawn()` — deactivate game object
    - In `KnockOff()` sequence `OnComplete` callback, after setting `state = SwordState.Dropped`, call `ItemManager.Instance.RegisterDroppedSword(this)`
    - In `OnTriggerEnter2D` pickup path (when a player picks up a dropped sword), call `ItemManager.Instance.DeregisterDroppedSword(this)` before `AddSword`
    - _Requirements: 5.1, 5.2_

- [x] 9. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests use FsCheck for C#/.NET as specified in the design
- Checkpoints ensure incremental validation
- The SpatialGrid is built first since both managers depend on it
- CharacterBase and Sword integration comes last to wire everything together
