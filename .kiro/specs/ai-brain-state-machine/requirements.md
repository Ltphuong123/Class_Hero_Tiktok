# Requirements Document

## Introduction

Hệ thống AIBrain và State Machine cho game Unity 2D top-down, nơi các nhân vật xoay kiếm xung quanh và chiến đấu với nhau. Hệ thống cung cấp khả năng ra quyết định tự động cho tối đa 500 nhân vật AI đồng thời, sử dụng 4 trạng thái (Wander, Collect, Chase, Flee) với cơ chế scoring-based để chuyển đổi trạng thái. Tích hợp với CharacterManager và ItemManager đã có sẵn, sử dụng staggered evaluation để đảm bảo hiệu năng.

## Glossary

- **AIBrain**: Component gắn vào CharacterBase, chứa logic ra quyết định và quản lý trạng thái hiện tại của một nhân vật AI
- **AIManager**: Singleton manager trung tâm quản lý tất cả AIBrain instances, lập lịch staggered evaluation theo round-robin
- **AIState**: Enum định nghĩa 4 trạng thái: Wander, Collect, Chase, Flee
- **State_Score**: Giá trị float được tính toán cho mỗi trạng thái dựa trên các yếu tố môi trường, trạng thái có điểm cao nhất được chọn
- **Staggered_Evaluation**: Kỹ thuật phân bổ việc đánh giá AI qua nhiều frame, chỉ 10-20 nhân vật đánh giá mỗi frame
- **Evaluation_Interval**: Khoảng thời gian giữa các lần đánh giá lại trạng thái của một AIBrain (0.3–0.5 giây)
- **CharacterManager**: Singleton quản lý tất cả CharacterBase, cung cấp spatial queries (GetNearbyCharacters, GetNearestCharacter)
- **ItemManager**: Singleton quản lý collectible items và dropped swords, cung cấp spatial queries (GetNearbySwords, GetNearestSword)
- **CharacterBase**: MonoBehaviour cơ sở cho mọi nhân vật, implements IManagedUpdate, có HP, SwordOrbit, MoveSpeed
- **SwordOrbit**: Component quản lý danh sách kiếm xoay quanh nhân vật, có swords list, radius, rotateSpeed
- **Perception_Radius**: Bán kính mà AIBrain sử dụng để truy vấn spatial grid tìm enemies và items gần đó
- **Wall_Avoidance**: Cơ chế sử dụng Physics2D raycasts để phát hiện và tránh tường/chướng ngại vật

## Requirements

### Requirement 1: AIState Enum

**User Story:** As a developer, I want a clearly defined set of AI states, so that each AI character has a finite, well-structured set of behaviors to transition between.

#### Acceptance Criteria

1. THE AIState enum SHALL define exactly four values: Wander, Collect, Chase, Flee
2. THE AIState enum SHALL use integer backing values starting from 0

### Requirement 2: AIBrain Component — Initialization and Registration

**User Story:** As a developer, I want an AIBrain component that attaches to CharacterBase and self-registers with AIManager, so that AI characters are automatically managed.

#### Acceptance Criteria

1. THE AIBrain SHALL be a MonoBehaviour component that references a CharacterBase on the same GameObject
2. WHEN an AIBrain is enabled, THE AIBrain SHALL register itself with the AIManager
3. WHEN an AIBrain is disabled or destroyed, THE AIBrain SHALL deregister itself from the AIManager
4. THE AIBrain SHALL initialize with the Wander state as the default state
5. THE AIBrain SHALL store a reference to the current target (CharacterBase for Chase/Flee, or ICollectibleItem for Collect)
6. THE AIBrain SHALL expose read-only properties for CurrentState, CurrentTarget, and the owning CharacterBase

### Requirement 3: AIBrain — Scoring-Based State Evaluation

**User Story:** As a developer, I want AIBrain to use a scoring system to decide which state to enter, so that AI characters make context-aware decisions based on their environment.

#### Acceptance Criteria

1. WHEN the AIManager triggers an evaluation, THE AIBrain SHALL compute a State_Score for each of the four AIState values
2. THE AIBrain SHALL select the AIState with the highest State_Score as the new current state
3. THE Wander score function SHALL return a base score that decreases when enemies or collectible items are nearby within the Perception_Radius
4. THE Collect score function SHALL increase proportionally to the number of dropped swords and items within the Perception_Radius, and increase when the owning CharacterBase has fewer swords in its SwordOrbit
5. THE Chase score function SHALL increase when a nearby enemy within the Perception_Radius has fewer swords and lower HP than the owning CharacterBase
6. THE Flee score function SHALL increase when a nearby enemy within the Perception_Radius has more swords and higher HP than the owning CharacterBase, and increase when the owning CharacterBase HP is below 30% of max HP
7. WHEN two or more states have equal highest State_Score, THE AIBrain SHALL prefer the current state to avoid unnecessary transitions
8. THE AIBrain SHALL query CharacterManager and ItemManager spatial APIs to gather environment data for scoring

### Requirement 4: AIBrain — State Behavior Execution

**User Story:** As a developer, I want each AI state to have distinct movement behavior, so that AI characters act differently depending on their current state.

#### Acceptance Criteria

1. WHILE in Wander state, THE AIBrain SHALL move the CharacterBase in a random direction, changing direction at a configurable interval
2. WHILE in Collect state, THE AIBrain SHALL move the CharacterBase toward the nearest dropped sword or collectible item
3. WHILE in Collect state, WHEN the target item is collected or despawned, THE AIBrain SHALL immediately re-evaluate its state
4. WHILE in Chase state, THE AIBrain SHALL move the CharacterBase toward the current target enemy
5. WHILE in Chase state, WHEN the target enemy is destroyed or moves out of the Perception_Radius, THE AIBrain SHALL immediately re-evaluate its state
6. WHILE in Flee state, THE AIBrain SHALL move the CharacterBase in the direction opposite to the nearest threatening enemy
7. WHILE in Flee state, WHEN no threatening enemy is within the Perception_Radius, THE AIBrain SHALL immediately re-evaluate its state
8. THE AIBrain SHALL apply movement by setting the CharacterBase position using its moveSpeed and deltaTime each frame

### Requirement 5: Wall Avoidance

**User Story:** As a developer, I want AI characters to detect and avoid walls, so that they do not get stuck on obstacles.

#### Acceptance Criteria

1. WHILE the AIBrain is moving the CharacterBase, THE AIBrain SHALL cast a Physics2D raycast in the movement direction to detect obstacles within a configurable avoidance distance
2. WHEN a wall or obstacle is detected by the raycast, THE AIBrain SHALL adjust the movement direction away from the obstacle before applying movement
3. THE AIBrain SHALL use a configurable LayerMask to determine which colliders count as walls or obstacles
4. WHILE in Wander state, WHEN a wall is detected, THE AIBrain SHALL choose a new random direction that does not face the wall

### Requirement 6: AIManager — Centralized Scheduling

**User Story:** As a developer, I want a centralized AIManager that schedules AI evaluations in a staggered manner, so that the game maintains high performance with 500 AI characters.

#### Acceptance Criteria

1. THE AIManager SHALL be a Singleton MonoBehaviour that manages all registered AIBrain instances
2. THE AIManager SHALL evaluate a maximum of 20 AIBrain instances per frame using round-robin scheduling
3. THE AIManager SHALL ensure each AIBrain is evaluated at an interval between 0.3 and 0.5 seconds
4. WHEN an AIBrain registers with the AIManager, THE AIManager SHALL add the AIBrain to the scheduling queue
5. WHEN an AIBrain deregisters from the AIManager, THE AIManager SHALL remove the AIBrain from the scheduling queue without disrupting the round-robin order of remaining AIBrains
6. THE AIManager SHALL call each scheduled AIBrain evaluation method during its Update loop, passing the current deltaTime
7. THE AIManager SHALL support registration and deregistration during the update loop by deferring changes to the next frame

### Requirement 7: AIManager — Batch Movement Update

**User Story:** As a developer, I want AIManager to batch-update all AI movement every frame, so that movement remains smooth while only decision-making is staggered.

#### Acceptance Criteria

1. THE AIManager SHALL call the movement execution method on every registered AIBrain each frame
2. THE AIManager SHALL pass Time.deltaTime to each AIBrain movement execution call
3. THE movement execution method SHALL apply the current state behavior movement without re-evaluating the state

### Requirement 8: Configurable Parameters

**User Story:** As a developer, I want all AI tuning parameters exposed as serialized fields, so that designers can adjust AI behavior in the Unity Inspector without code changes.

#### Acceptance Criteria

1. THE AIBrain SHALL expose Perception_Radius as a serialized float field with a default value of 10
2. THE AIBrain SHALL expose Wander direction change interval as a serialized float field with a default value of 2 seconds
3. THE AIBrain SHALL expose Wall_Avoidance distance as a serialized float field with a default value of 1.5
4. THE AIBrain SHALL expose Wall_Avoidance LayerMask as a serialized field
5. THE AIManager SHALL expose the maximum evaluations per frame as a serialized int field with a default value of 20
6. THE AIManager SHALL expose the evaluation interval as a serialized float field with a default value of 0.3 seconds

### Requirement 9: Integration with Existing Systems

**User Story:** As a developer, I want the AI system to integrate seamlessly with CharacterManager and ItemManager, so that AI characters use the same spatial query infrastructure as the rest of the game.

#### Acceptance Criteria

1. THE AIBrain SHALL use CharacterManager.GetNearbyCharacters to find enemies within the Perception_Radius during state evaluation
2. THE AIBrain SHALL use ItemManager.GetNearestSword and ItemManager.GetNearestItem to find collectible targets during state evaluation
3. THE AIBrain SHALL use CharacterManager.GetNearestCharacter to identify the closest threat for Flee state calculations
4. WHEN CharacterManager or ItemManager is not available (null Instance), THE AIBrain SHALL default to Wander state and log a warning once
5. THE AIBrain SHALL not call CharacterManager or ItemManager APIs outside of the scheduled evaluation to avoid redundant spatial queries

### Requirement 10: Error Handling and Edge Cases

**User Story:** As a developer, I want the AI system to handle edge cases gracefully, so that the game remains stable under unexpected conditions.

#### Acceptance Criteria

1. IF the AIBrain target reference becomes null during execution, THEN THE AIBrain SHALL immediately re-evaluate its state
2. IF no items or enemies are within the Perception_Radius, THEN THE AIBrain SHALL default to Wander state
3. IF the owning CharacterBase is destroyed, THEN THE AIBrain SHALL deregister from the AIManager before destruction
4. WHEN the AIManager has zero registered AIBrains, THE AIManager SHALL skip the evaluation loop without errors
5. IF the AIBrain receives a deltaTime of zero or negative, THEN THE AIBrain SHALL skip movement for that frame
