# Implementation Plan: AI Brain State Machine

## Overview

Implement a scoring-based AI state machine for Unity 2D. The system adds an `AIState` enum, a pure `AIScoring` static class, an `AIBrain` MonoBehaviour for per-character decision-making and movement, and an `AIManager` singleton for staggered scheduling. Existing `CharacterBase` and `SwordOrbit` classes get read-only accessors. All new files go under `Assets/_GAME/Scripts/AI/` and `Assets/_GAME/Scripts/Manager/`.

## Tasks

- [x] 1. Expose read-only accessors on existing classes
  - [x] 1.1 Add `CurrentHp`, `MaxHp`, and `MoveSpeed` public read-only properties to `CharacterBase`
    - Add `public float CurrentHp => currentHp;`
    - Add `public float MaxHp => maxHp;`
    - Add `public float MoveSpeed => moveSpeed;`
    - File: `Assets/_GAME/Scripts/CharacterBase.cs`
    - _Requirements: 9.1, 9.2, 3.5, 3.6_

  - [x] 1.2 Add `SwordCount` public read-only property to `SwordOrbit`
    - Add `public int SwordCount => swords.Count;`
    - File: `Assets/_GAME/Scripts/SwordOrbit.cs`
    - _Requirements: 3.4, 3.5, 3.6_

- [x] 2. Create AIState enum
  - [x] 2.1 Create `AIState.cs` with the four-value enum
    - Define `AIState` enum with values: `Wander = 0`, `Collect = 1`, `Chase = 2`, `Flee = 3`
    - File: `Assets/_GAME/Scripts/AI/AIState.cs`
    - _Requirements: 1.1, 1.2_

- [x] 3. Create AIScoring static class with pure scoring functions
  - [x] 3.1 Implement `AIScoring.ComputeWanderScore`
    - Static method: `public static float ComputeWanderScore(int nearbyEnemyCount, bool hasNearbyItem)`
    - Base score 0.5, subtract 0.2 if enemies > 0, subtract 0.2 if item nearby, clamp to min 0
    - File: `Assets/_GAME/Scripts/AI/AIScoring.cs`
    - _Requirements: 3.3_

  - [x] 3.2 Implement `AIScoring.ComputeCollectScore`
    - Static method: `public static float ComputeCollectScore(bool hasSword, bool hasItem, int currentSwordCount, int maxSwords)`
    - Return 0 if no sword and no item; base 0.3, add `(maxSwords - currentSwordCount) * 0.15f`, add 0.2 if sword nearby
    - File: `Assets/_GAME/Scripts/AI/AIScoring.cs`
    - _Requirements: 3.4_

  - [x] 3.3 Implement `AIScoring.ComputeChaseScore`
    - Static method: `public static float ComputeChaseScore(int ownSwords, float ownHp, int enemySwords, float enemyHp, float maxHp)`
    - Return 0 if no advantage (enemy swords >= own or enemy hp > own); otherwise 0.3 + sword advantage * 0.2 + hp advantage / maxHp * 0.3
    - File: `Assets/_GAME/Scripts/AI/AIScoring.cs`
    - _Requirements: 3.5_

  - [x] 3.4 Implement `AIScoring.ComputeFleeScore`
    - Static method: `public static float ComputeFleeScore(int ownSwords, float ownHp, int enemySwords, float enemyHp, float maxHp)`
    - Compute threat from enemy advantage; add 0.3 if own HP < 30% of max
    - File: `Assets/_GAME/Scripts/AI/AIScoring.cs`
    - _Requirements: 3.6_

  - [x] 3.5 Implement `AIScoring.SelectState`
    - Static method: `public static AIState SelectState(float[] scores, AIState currentState)`
    - Return the AIState with the highest score; on tie, prefer `currentState`
    - File: `Assets/_GAME/Scripts/AI/AIScoring.cs`
    - _Requirements: 3.1, 3.2, 3.7_

  - [ ]* 3.6 Write property test: Highest score wins (Property 1)
    - **Property 1: Highest score wins**
    - Generate random float[4] scores with a unique maximum, verify `SelectState` returns the correct AIState
    - **Validates: Requirements 3.1, 3.2**

  - [ ]* 3.7 Write property test: Tie-breaking preserves current state (Property 2)
    - **Property 2: Tie-breaking preserves current state**
    - Generate random float[4] with forced ties including current state, verify current state is returned
    - **Validates: Requirements 3.7**

  - [ ]* 3.8 Write property test: Wander score decreases with nearby entities (Property 3)
    - **Property 3: Wander score decreases with nearby entities**
    - Compare wander score with zero enemies/items vs with enemies/items added, verify monotonic decrease
    - **Validates: Requirements 3.3**

  - [ ]* 3.9 Write property test: Collect score monotonicity (Property 4)
    - **Property 4: Collect score monotonicity**
    - Generate random item counts and sword counts, verify collect score increases with more items or fewer own swords
    - **Validates: Requirements 3.4**

  - [ ]* 3.10 Write property test: Chase score increases with advantage (Property 5)
    - **Property 5: Chase score increases with advantage**
    - Generate random own/enemy stats, verify chase score is higher with greater advantage
    - **Validates: Requirements 3.5**

  - [ ]* 3.11 Write property test: Flee score increases with threat (Property 6)
    - **Property 6: Flee score increases with threat**
    - Generate random own/enemy stats + HP thresholds, verify flee score ordering and 30% HP bonus
    - **Validates: Requirements 3.6**

  - [ ]* 3.12 Write property test: Empty environment defaults to Wander (Property 13)
    - **Property 13: Empty environment defaults to Wander**
    - Generate random positions with no enemies/items in perception radius, verify Wander is selected
    - **Validates: Requirements 10.2**

- [x] 4. Checkpoint — Verify scoring logic
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Create AIBrain MonoBehaviour
  - [x] 5.1 Create `AIBrain.cs` with serialized fields, internal state, and registration lifecycle
    - Serialized fields: `perceptionRadius` (default 10), `wanderChangeInterval` (default 2), `wallAvoidDistance` (default 1.5), `wallMask` LayerMask
    - Internal state: `currentState`, `chaseTarget`, `collectTarget`, `fleeTarget`, `wanderDirection`, `wanderTimer`, `scores` array, `nearbyEnemies` list, `hasLoggedManagerWarning`
    - `OnEnable` → register with `AIManager`, `OnDisable` → deregister
    - Expose read-only `CurrentState`, `CurrentTarget`, `Owner`
    - File: `Assets/_GAME/Scripts/AI/AIBrain.cs`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 8.1, 8.2, 8.3, 8.4_

  - [x] 5.2 Implement `AIBrain.Evaluate()` — scoring and state selection
    - Query `CharacterManager.GetNearbyCharacters` for enemies (exclude self)
    - Query `ItemManager.GetNearestSword` and `ItemManager.GetNearestItem`
    - Compute all 4 scores using `AIScoring` static methods, iterating enemies for best chase/flee
    - Call `AIScoring.SelectState` to pick new state, update target references
    - Handle null managers: default to Wander, log warning once
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 5.3 Implement `AIBrain.ExecuteMovement(float deltaTime)` — per-state movement
    - Guard: return immediately if `deltaTime <= 0`
    - Switch on `CurrentState`:
      - **Wander**: move in `wanderDirection`, decrement timer, pick new random direction when timer expires
      - **Collect**: move toward `collectTarget.Position`; if target null/inactive, re-evaluate
      - **Chase**: move toward `chaseTarget.Position`; if target null, re-evaluate
      - **Flee**: move away from `fleeTarget.Position`; if target null, re-evaluate
    - Apply wall avoidance before final position update
    - Apply `position += direction * moveSpeed * deltaTime`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 10.1, 10.5_

  - [x] 5.4 Implement wall avoidance in `AIBrain`
    - Raycast in movement direction using `Physics2D.Raycast(pos, dir, wallAvoidDistance, wallMask)`
    - If hit: try rotated directions ±45°, ±90°; fallback to reverse
    - In Wander state, pick a new random direction when wall detected
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [ ]* 5.5 Write property test: Target-seeking movement direction (Property 7)
    - **Property 7: Target-seeking movement direction**
    - Generate random owner/target positions, verify computed direction has positive dot product with (target - owner)
    - **Validates: Requirements 4.2, 4.4**

  - [ ]* 5.6 Write property test: Flee movement direction opposes threat (Property 8)
    - **Property 8: Flee movement direction opposes threat**
    - Generate random owner/threat positions, verify computed direction has negative dot product with (threat - owner)
    - **Validates: Requirements 4.6**

  - [ ]* 5.7 Write property test: Movement magnitude invariant (Property 9)
    - **Property 9: Movement magnitude invariant**
    - Generate random speed and deltaTime (positive and non-positive), verify position delta magnitude equals speed × dt (within tolerance), and no movement for dt ≤ 0
    - **Validates: Requirements 4.8, 10.5**

  - [ ]* 5.8 Write property test: Wall avoidance produces safe direction (Property 10)
    - **Property 10: Wall avoidance produces safe direction**
    - Generate random direction and wall normal, verify adjusted direction does not point into the wall (dot product with normal ≥ 0)
    - **Validates: Requirements 5.2**

- [x] 6. Checkpoint — Verify AIBrain
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Create AIManager singleton
  - [x] 7.1 Create `AIManager.cs` with registration, deferred add/remove, and round-robin scheduling
    - Extend `Singleton<AIManager>`
    - Serialized fields: `maxEvaluationsPerFrame` (default 20), `evaluationInterval` (default 0.3f)
    - Internal state: `brains` list, `pendingAdd`/`pendingRemove` lists, `brainSet` HashSet, `isUpdating` flag, `roundRobinIndex`
    - `Register(AIBrain)` / `Deregister(AIBrain)` with deferred support during update
    - `BrainCount` read-only property
    - File: `Assets/_GAME/Scripts/Manager/AIManager.cs`
    - _Requirements: 6.1, 6.4, 6.5, 6.7, 8.5, 8.6_

  - [x] 7.2 Implement `AIManager.Update()` — flush pending, staggered evaluate, batch movement
    - Flush `pendingAdd` and `pendingRemove` at start of Update
    - Round-robin evaluate up to `maxEvaluationsPerFrame` brains per frame
    - Call `ExecuteMovement(Time.deltaTime)` on every registered brain each frame
    - Handle zero brains gracefully (skip loop)
    - _Requirements: 6.2, 6.3, 6.6, 7.1, 7.2, 7.3, 10.4_

  - [ ]* 7.3 Write property test: Evaluation cap per frame (Property 11)
    - **Property 11: Evaluation cap per frame**
    - Generate random brain counts 0–500, verify a single Update invokes Evaluate on at most min(N, maxEvaluationsPerFrame) brains
    - **Validates: Requirements 6.2**

  - [ ]* 7.4 Write property test: Deregistration preserves relative order (Property 12)
    - **Property 12: Deregistration preserves relative order**
    - Generate random brain lists, remove a random element, verify remaining brains keep original relative order
    - **Validates: Requirements 6.5**

- [x] 8. Integration wiring and final verification
  - [x] 8.1 Wire AIBrain onto AI character prefabs
    - Ensure AI character GameObjects have both `CharacterBase` and `AIBrain` components
    - Remove or disable `ManagedUpdate` input handling for AI characters (they should not read keyboard input)
    - Verify `AIBrain` references the `CharacterBase` on the same GameObject
    - _Requirements: 2.1, 9.1, 9.2_

  - [x] 8.2 Add AIManager to the scene
    - Create or attach `AIManager` component to a manager GameObject in the scene
    - Configure `maxEvaluationsPerFrame` and `evaluationInterval` in Inspector
    - _Requirements: 6.1, 8.5, 8.6_

  - [ ]* 8.3 Write integration tests for registration lifecycle
    - Test: Enable AIBrain → registered in AIManager; Disable → deregistered
    - Test: Register during update → deferred to next frame
    - Test: Zero brains → AIManager Update runs without errors
    - _Requirements: 2.2, 2.3, 6.4, 6.5, 6.7, 10.4_

- [x] 9. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Scoring functions are extracted as pure static methods in `AIScoring` for testability without MonoBehaviour lifecycle
- Property tests should use Unity Test Framework (Edit Mode) targeting `AIScoring` static methods
- Checkpoints ensure incremental validation between major implementation phases
