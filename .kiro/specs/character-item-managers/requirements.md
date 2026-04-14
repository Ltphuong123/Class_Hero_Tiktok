# Requirements Document

## Introduction

This feature introduces two centralized manager systems — CharacterManager and ItemManager — for a Unity 2D top-down game where characters orbit swords around them. The game targets ~500 autonomous CharacterBase instances fighting each other simultaneously. These managers provide centralized update loops, spatial partitioning via grid-based lookups, and registration/deregistration APIs. They serve as the foundational data layer for a future AI State Machine system that will query nearby characters and items for autonomous decision-making (target selection, item collection, threat avoidance).

## Glossary

- **CharacterManager**: A singleton manager responsible for registering, deregistering, updating, and spatially querying all CharacterBase instances in the scene.
- **ItemManager**: A singleton manager responsible for tracking all dropped swords and collectible items on the map, providing spatial queries, and managing item spawning/despawning with object pooling.
- **CharacterBase**: The existing MonoBehaviour representing a player or AI character with HP, movement, and a SwordOrbit reference.
- **Sword**: The existing MonoBehaviour representing an individual sword with states (Dropped, Orbiting, Animating, FlyingIn, Sliding).
- **SwordOrbit**: The existing MonoBehaviour that manages swords orbiting around a character.
- **Spatial_Grid**: A uniform grid data structure that partitions 2D world space into fixed-size cells, enabling O(1) cell lookup for neighbor and item queries.
- **Grid_Cell**: A single cell within the Spatial_Grid, identified by integer coordinates (cellX, cellY), containing references to entities located within that cell's world-space bounds.
- **Collectible_Item**: Any item on the map that a character can pick up, including dropped swords and other spawned items.
- **AI_State_Machine**: A future system (not implemented in this feature) that will use CharacterManager and ItemManager APIs to make autonomous decisions for characters.
- **Object_Pool**: A reuse pattern that pre-allocates and recycles game objects to avoid runtime allocation and garbage collection overhead.
- **Batch_Update**: A centralized update pattern where a single manager iterates over all registered entities each frame, replacing individual MonoBehaviour.Update() calls.

## Requirements

### Requirement 1: Character Registration

**User Story:** As a developer, I want to register and deregister CharacterBase instances with the CharacterManager, so that the manager always has an accurate list of active characters.

#### Acceptance Criteria

1. WHEN a CharacterBase is spawned, THE CharacterManager SHALL add the CharacterBase to its internal registry and place it in the correct Spatial_Grid cell based on the CharacterBase world position.
2. WHEN a CharacterBase is destroyed or removed from play, THE CharacterManager SHALL remove the CharacterBase from its internal registry and from the Spatial_Grid.
3. THE CharacterManager SHALL maintain a count of currently registered CharacterBase instances accessible via a public property.
4. IF a CharacterBase that is not registered is passed to the deregistration method, THEN THE CharacterManager SHALL ignore the call without throwing an exception.
5. IF a CharacterBase that is already registered is passed to the registration method again, THEN THE CharacterManager SHALL ignore the duplicate call without adding a second entry.

### Requirement 2: Character Spatial Grid

**User Story:** As a developer, I want the CharacterManager to maintain a grid-based spatial partition of all characters, so that spatial queries for nearby characters complete efficiently at scale.

#### Acceptance Criteria

1. THE CharacterManager SHALL partition world space into a uniform Spatial_Grid with a configurable cell size (default 5 units).
2. WHEN a registered CharacterBase moves to a different Grid_Cell between frames, THE CharacterManager SHALL update the Spatial_Grid by removing the CharacterBase from the old Grid_Cell and adding it to the new Grid_Cell.
3. WHEN GetNearbyCharacters(position, radius) is called, THE CharacterManager SHALL return all registered CharacterBase instances whose world position is within the specified radius of the given position, by querying only the Grid_Cell instances that overlap the search area.
4. WHEN GetNearbyCharacters is called with a radius that spans multiple Grid_Cell instances, THE CharacterManager SHALL check all overlapping Grid_Cell instances and return the union of matching CharacterBase instances.
5. WHEN GetNearbyCharacters is called and no CharacterBase instances are within range, THE CharacterManager SHALL return an empty collection without allocating new memory.

### Requirement 3: Character Batch Update

**User Story:** As a developer, I want the CharacterManager to provide a centralized update loop for all characters, so that individual MonoBehaviour.Update() calls are eliminated and performance scales to 500 characters.

#### Acceptance Criteria

1. THE CharacterManager SHALL iterate over all registered CharacterBase instances each frame in a single centralized update loop.
2. WHEN the centralized update loop runs, THE CharacterManager SHALL invoke a managed update method on each registered CharacterBase, passing the current Time.deltaTime.
3. WHEN a CharacterBase changes Grid_Cell during the update loop, THE CharacterManager SHALL update the Spatial_Grid cell assignment for that CharacterBase within the same frame.
4. THE CharacterManager SHALL process registration and deregistration requests that occur during the update loop by deferring them to a buffer and applying them after the current iteration completes.

### Requirement 4: Character Query API for AI

**User Story:** As a developer, I want the CharacterManager to expose query APIs suitable for AI decision-making, so that the future AI_State_Machine can efficiently find targets and assess threats.

#### Acceptance Criteria

1. WHEN GetNearestCharacter(position, radius, excludeSelf) is called, THE CharacterManager SHALL return the single closest registered CharacterBase within the specified radius, excluding the specified CharacterBase if excludeSelf is provided.
2. WHEN GetCharactersInRadius(position, radius) is called, THE CharacterManager SHALL return all registered CharacterBase instances within the specified radius, sorted by distance from the given position in ascending order.
3. WHEN GetCharacterCount() is called, THE CharacterManager SHALL return the total number of currently registered CharacterBase instances.
4. IF no CharacterBase instances exist within the specified radius, THEN THE CharacterManager SHALL return null for single-result queries and an empty collection for multi-result queries.

### Requirement 5: Item Registration and Tracking

**User Story:** As a developer, I want the ItemManager to track all dropped swords and collectible items on the map, so that the game has a centralized registry of all pickupable items.

#### Acceptance Criteria

1. WHEN a Sword enters the Dropped state, THE ItemManager SHALL register the Sword as a Collectible_Item and place it in the correct Spatial_Grid cell.
2. WHEN a Collectible_Item is picked up by a CharacterBase, THE ItemManager SHALL deregister the Collectible_Item and remove it from the Spatial_Grid.
3. THE ItemManager SHALL maintain separate tracking collections for dropped swords and other Collectible_Item types.
4. WHEN a new Collectible_Item type is spawned into the world, THE ItemManager SHALL register the Collectible_Item and place it in the correct Spatial_Grid cell.
5. IF a Collectible_Item that is not registered is passed to the deregistration method, THEN THE ItemManager SHALL ignore the call without throwing an exception.

### Requirement 6: Item Spatial Grid

**User Story:** As a developer, I want the ItemManager to maintain a grid-based spatial partition of all items, so that spatial queries for nearby items complete efficiently.

#### Acceptance Criteria

1. THE ItemManager SHALL partition world space into a uniform Spatial_Grid with a configurable cell size (default 5 units).
2. WHEN GetNearbyItems(position, radius) is called, THE ItemManager SHALL return all registered Collectible_Item instances whose world position is within the specified radius of the given position, by querying only the overlapping Grid_Cell instances.
3. WHEN GetNearbySwords(position, radius) is called, THE ItemManager SHALL return only dropped Sword instances within the specified radius.
4. WHEN GetNearestItem(position, radius) is called, THE ItemManager SHALL return the single closest Collectible_Item within the specified radius.
5. IF no Collectible_Item instances exist within the specified radius, THEN THE ItemManager SHALL return null for single-result queries and an empty collection for multi-result queries.

### Requirement 7: Item Spawning and Despawning

**User Story:** As a developer, I want the ItemManager to handle item spawning and despawning with object pooling, so that runtime allocation is minimized during gameplay.

#### Acceptance Criteria

1. WHEN SpawnItem(prefab, position) is called, THE ItemManager SHALL retrieve an inactive instance from the Object_Pool if available, or instantiate a new instance if the pool is empty, and place the item at the specified position.
2. WHEN DespawnItem(item) is called, THE ItemManager SHALL deactivate the Collectible_Item, deregister it from the Spatial_Grid, and return it to the Object_Pool for reuse.
3. THE ItemManager SHALL pre-allocate a configurable number of Collectible_Item instances in the Object_Pool during initialization.
4. IF the Object_Pool exceeds a configurable maximum size, THEN THE ItemManager SHALL destroy excess instances instead of returning them to the pool.

### Requirement 8: Item Query API for AI

**User Story:** As a developer, I want the ItemManager to expose query APIs suitable for AI decision-making, so that the future AI_State_Machine can efficiently find items to collect.

#### Acceptance Criteria

1. WHEN GetNearestSword(position, radius) is called, THE ItemManager SHALL return the single closest dropped Sword within the specified radius.
2. WHEN GetNearestItemOfType(position, radius, itemType) is called, THE ItemManager SHALL return the single closest Collectible_Item of the specified type within the specified radius.
3. WHEN GetItemCount() is called, THE ItemManager SHALL return the total number of currently registered Collectible_Item instances.
4. WHEN GetDroppedSwordCount() is called, THE ItemManager SHALL return the total number of currently registered dropped Sword instances.
5. IF no matching items exist within the specified radius, THEN THE ItemManager SHALL return null.

### Requirement 9: Singleton Integration

**User Story:** As a developer, I want both managers to integrate with the existing Singleton pattern, so that they are globally accessible and consistent with the project architecture.

#### Acceptance Criteria

1. THE CharacterManager SHALL extend the existing Singleton base class and be accessible via CharacterManager.Instance.
2. THE ItemManager SHALL extend the existing Singleton base class and be accessible via ItemManager.Instance.
3. WHEN either manager needs to execute initialization logic in Awake, THE Singleton base class SHALL provide a virtual or protected Awake method that derived classes can override without breaking the singleton pattern.
4. THE CharacterManager SHALL persist across scene loads only if configured to do so via a serialized field.
5. THE ItemManager SHALL persist across scene loads only if configured to do so via a serialized field.
