// using UnityEngine;

// /// <summary>
// /// Advanced test controller for player character with additional features.
// /// Controls:
// /// - WASD: Movement
// /// - Q: Cycle sword type forward
// /// - E: Cycle sword type backward
// /// - Shift: Sprint (2x speed)
// /// - Space: Dash
// /// - 1-8: Direct sword type selection
// /// - R: Add random sword
// /// - T: Remove last sword
// /// - F: Toggle AI mode
// /// </summary>
// public class CharacterTestAdvanced1 : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private CharacterBase character;
//     [SerializeField] private SwordOrbit swordOrbit;
//     [SerializeField] private GameObject swordPrefab;

//     [Header("Movement Settings")]
//     [SerializeField] private float moveSpeed = 5f;
//     [SerializeField] private float sprintMultiplier = 2f;
//     [SerializeField] private bool useRawInput = true;

//     [Header("Dash Settings")]
//     [SerializeField] private bool enableDash = true;
//     [SerializeField] private float dashDistance = 3f;
//     [SerializeField] private float dashDuration = 0.2f;
//     [SerializeField] private float dashCooldown = 1f;

//     [Header("Sword Settings")]
//     [SerializeField] private bool enableSwordCycling = true;
//     [SerializeField] private bool enableDirectSelection = true;
//     [SerializeField] private bool enableSwordManagement = true;

//     [Header("Debug")]
//     [SerializeField] private bool showDebugInfo = true;
//     [SerializeField] private bool showGizmos = true;

//     private Vector3 moveDirection;
//     private SwordType currentSwordType = SwordType.Default;
//     private MapManager map;
//     private ItemManager itemManager;
//     private bool isInitialized;

//     // Dash state
//     private bool isDashing;
//     private float dashTimer;
//     private float dashCooldownTimer;
//     private Vector3 dashDirection;

//     // AI toggle
//     private CharacterStateMachine stateMachine;
//     private bool aiEnabled;

//     private void Start()
//     {
//         Initialize();
//     }

//     private void Initialize()
//     {
//         if (character == null)
//             character = GetComponent<CharacterBase>();

//         if (swordOrbit == null && character != null)
//             swordOrbit = character.GetSwordOrbit();

//         map = MapManager.Instance;
//         itemManager = ItemManager.Instance;

//         if (character != null)
//         {
//             stateMachine = character.GetStateMachine();
//             if (stateMachine != null)
//             {
//                 stateMachine.enabled = false;
//                 aiEnabled = false;
//             }

//             if (swordOrbit != null)
//                 swordOrbit.IsPlayer = true;
//         }

//         isInitialized = true;
//         LogControls();
//     }

//     private void Update()
//     {
//         if (!isInitialized || character == null) return;

//         UpdateTimers();

//         if (!aiEnabled)
//         {
//             HandleMovementInput();
//             HandleDashInput();
//         }

//         HandleSwordCycling();
//         HandleDirectSwordSelection();
//         HandleSwordManagement();
//         HandleAIToggle();
//         HandleDebugInfo();
//     }

//     private void UpdateTimers()
//     {
//         if (dashCooldownTimer > 0f)
//             dashCooldownTimer -= Time.deltaTime;

//         if (isDashing)
//         {
//             dashTimer -= Time.deltaTime;
//             if (dashTimer <= 0f)
//             {
//                 isDashing = false;
//             }
//         }
//     }

//     private void HandleMovementInput()
//     {
//         if (isDashing)
//         {
//             PerformDash();
//             return;
//         }

//         // Get input
//         float horizontal = useRawInput ? Input.GetAxisRaw("Horizontal") : Input.GetAxis("Horizontal");
//         float vertical = useRawInput ? Input.GetAxisRaw("Vertical") : Input.GetAxis("Vertical");

//         // Calculate move direction
//         moveDirection = new Vector3(horizontal, vertical, 0f);

//         // Normalize diagonal movement
//         if (moveDirection.sqrMagnitude > 1f)
//             moveDirection.Normalize();

//         // Apply movement
//         if (moveDirection.sqrMagnitude > 0.01f)
//         {
//             float speed = moveSpeed;

//             // Sprint
//             if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
//                 speed *= sprintMultiplier;

//             Vector3 currentPos = transform.position;
//             Vector3 targetPos = currentPos + moveDirection * speed * Time.deltaTime;

//             // Validate move with map collision
//             if (map != null)
//                 targetPos = ValidateMove(currentPos, targetPos);

//             transform.position = targetPos;
//         }
//     }

//     private void HandleDashInput()
//     {
//         if (!enableDash || isDashing || dashCooldownTimer > 0f) return;

//         if (Input.GetKeyDown(KeyCode.Space) && moveDirection.sqrMagnitude > 0.01f)
//         {
//             StartDash();
//         }
//     }

//     private void StartDash()
//     {
//         isDashing = true;
//         dashTimer = dashDuration;
//         dashCooldownTimer = dashCooldown;
//         dashDirection = moveDirection.normalized;

//         if (showDebugInfo)
//             Debug.Log("[CharacterTest] Dash!");
//     }

//     private void PerformDash()
//     {
//         float progress = 1f - (dashTimer / dashDuration);
//         float speed = dashDistance / dashDuration;

//         // Ease out
//         float easeSpeed = speed * (1f - progress * progress);

//         Vector3 currentPos = transform.position;
//         Vector3 targetPos = currentPos + dashDirection * easeSpeed * Time.deltaTime;

//         if (map != null)
//             targetPos = ValidateMove(currentPos, targetPos);

//         transform.position = targetPos;
//     }

//     private Vector3 ValidateMove(Vector3 from, Vector3 to)
//     {
//         if (map == null) return to;

//         float z = from.z;

//         // Check direct path + midpoint
//         if (!map.IsBlockedWorld(new Vector3(to.x, to.y, z)))
//         {
//             float midX = (from.x + to.x) * 0.5f;
//             float midY = (from.y + to.y) * 0.5f;
//             if (!map.IsBlockedWorld(new Vector3(midX, midY, z)))
//                 return new Vector3(to.x, to.y, z);
//         }

//         // Try X-axis only
//         if (!map.IsBlockedWorld(new Vector3(to.x, from.y, z)))
//         {
//             float midX = (from.x + to.x) * 0.5f;
//             if (!map.IsBlockedWorld(new Vector3(midX, from.y, z)))
//                 return new Vector3(to.x, from.y, z);
//         }

//         // Try Y-axis only
//         if (!map.IsBlockedWorld(new Vector3(from.x, to.y, z)))
//         {
//             float midY = (from.y + to.y) * 0.5f;
//             if (!map.IsBlockedWorld(new Vector3(from.x, midY, z)))
//                 return new Vector3(from.x, to.y, z);
//         }

//         return from;
//     }

//     private void HandleSwordCycling()
//     {
//         if (!enableSwordCycling || swordOrbit == null) return;

//         // Q: Cycle forward
//         if (Input.GetKeyDown(KeyCode.Q))
//         {
//             CycleSwordType(1);
//         }

//         // E: Cycle backward
//         if (Input.GetKeyDown(KeyCode.E))
//         {
//             CycleSwordType(-1);
//         }
//     }

//     private void CycleSwordType(int direction)
//     {
//         int currentIndex = (int)currentSwordType;
//         int maxIndex = System.Enum.GetValues(typeof(SwordType)).Length - 1;

//         currentIndex += direction;

//         if (currentIndex > maxIndex)
//             currentIndex = 0;
//         else if (currentIndex < 0)
//             currentIndex = maxIndex;

//         currentSwordType = (SwordType)currentIndex;
//         swordOrbit.SetSwordType(currentSwordType);

//         if (showDebugInfo)
//             Debug.Log($"[CharacterTest] Sword type: {currentSwordType}");
//     }

//     private void HandleDirectSwordSelection()
//     {
//         if (!enableDirectSelection || swordOrbit == null) return;

//         // 1-8: Direct selection
//         if (Input.GetKeyDown(KeyCode.Alpha1)) SetSwordType(SwordType.Default);
//         if (Input.GetKeyDown(KeyCode.Alpha2)) SetSwordType(SwordType.Fire);
//         if (Input.GetKeyDown(KeyCode.Alpha3)) SetSwordType(SwordType.Lightning);
//         if (Input.GetKeyDown(KeyCode.Alpha4)) SetSwordType(SwordType.Miasma);
//         if (Input.GetKeyDown(KeyCode.Alpha5)) SetSwordType(SwordType.Snow);
//     }

//     private void HandleSwordManagement()
//     {
//         if (!enableSwordManagement || swordOrbit == null) return;

//         // R: Add random sword
//         if (Input.GetKeyDown(KeyCode.R))
//         {
//             AddRandomSword();
//         }

//         // T: Remove last sword
//         if (Input.GetKeyDown(KeyCode.T))
//         {
//             RemoveLastSword();
//         }
//     }

//     private void AddRandomSword()
//     {
//         if (swordPrefab == null)
//         {
//             Debug.LogWarning("[CharacterTest] Sword prefab not assigned!");
//             return;
//         }

//         Vector2 randomOffset = Random.insideUnitCircle.normalized * 2f;
//         Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
        
//         if (itemManager != null)
//         {
//             itemManager.SpawnItem(swordPrefab, spawnPos);
//         }
//         else
//         {
//             GameObject go = Instantiate(swordPrefab, spawnPos, Quaternion.identity);
//             Sword sword = go.GetComponent<Sword>();
//             if (sword != null)
//                 sword.OnSpawn(spawnPos);
//         }

//         if (showDebugInfo)
//             Debug.Log($"[CharacterTest] Spawned sword at {spawnPos}");
//     }

//     private void RemoveLastSword()
//     {
//         if (swordOrbit.SwordCount > 0)
//         {
//             swordOrbit.DropSword(swordOrbit.SwordCount - 1);

//             if (showDebugInfo)
//                 Debug.Log($"[CharacterTest] Removed sword. Remaining: {swordOrbit.SwordCount}");
//         }
//     }

//     private void HandleAIToggle()
//     {
//         if (Input.GetKeyDown(KeyCode.F))
//         {
//             ToggleAI();
//         }
//     }

//     private void ToggleAI()
//     {
//         if (stateMachine == null) return;

//         aiEnabled = !aiEnabled;
//         stateMachine.enabled = aiEnabled;

//         if (showDebugInfo)
//             Debug.Log($"[CharacterTest] AI {(aiEnabled ? "Enabled" : "Disabled")}");
//     }

//     private void HandleDebugInfo()
//     {
//         if (Input.GetKeyDown(KeyCode.H))
//         {
//             LogControls();
//         }
//     }

//     public void SetSwordType(SwordType type)
//     {
//         currentSwordType = type;
//         if (swordOrbit != null)
//             swordOrbit.SetSwordType(type);

//         if (showDebugInfo)
//             Debug.Log($"[CharacterTest] Sword type: {type}");
//     }

//     public void SetControlEnabled(bool enabled)
//     {
//         this.enabled = enabled;
//     }

//     public SwordType GetCurrentSwordType() => currentSwordType;
//     public bool IsAIEnabled() => aiEnabled;
//     public bool IsDashing() => isDashing;

//     private void LogControls()
//     {
//         Debug.Log("=== CHARACTER TEST CONTROLS ===\n" +
//                   "WASD - Move\n" +
//                   "Shift - Sprint\n" +
//                   "Space - Dash\n" +
//                   "Q/E - Cycle sword type\n" +
//                   "1-8 - Direct sword selection\n" +
//                   "R - Add random sword\n" +
//                   "T - Remove last sword\n" +
//                   "F - Toggle AI\n" +
//                   "H - Show controls\n" +
//                   "===============================");
//     }

//     private void OnValidate()
//     {
//         moveSpeed = Mathf.Max(0.1f, moveSpeed);
//         sprintMultiplier = Mathf.Max(1f, sprintMultiplier);
//         dashDistance = Mathf.Max(0.1f, dashDistance);
//         dashDuration = Mathf.Max(0.05f, dashDuration);
//         dashCooldown = Mathf.Max(0f, dashCooldown);
//     }

//     private void OnDrawGizmos()
//     {
//         if (!showGizmos || !Application.isPlaying || !isInitialized) return;

//         // Draw movement direction
//         if (!aiEnabled && moveDirection.sqrMagnitude > 0.01f)
//         {
//             Gizmos.color = isDashing ? Color.yellow : Color.cyan;
//             Gizmos.DrawLine(transform.position, transform.position + moveDirection * 2f);
//         }

//         // Draw player indicator
//         Gizmos.color = aiEnabled ? Color.red : Color.green;
//         Gizmos.DrawWireSphere(transform.position, 0.5f);

//         // Draw dash cooldown indicator
//         if (dashCooldownTimer > 0f)
//         {
//             Gizmos.color = Color.yellow;
//             float radius = 0.7f * (dashCooldownTimer / dashCooldown);
//             Gizmos.DrawWireSphere(transform.position, radius);
//         }
//     }

//     private void OnGUI()
//     {
//         if (!showDebugInfo || !isInitialized) return;

//         GUILayout.BeginArea(new Rect(10, 10, 300, 200));
//         GUILayout.BeginVertical("box");

//         GUILayout.Label($"<b>Character Test</b>", new GUIStyle(GUI.skin.label) { richText = true });
//         GUILayout.Label($"Mode: {(aiEnabled ? "AI" : "Player")}");
//         GUILayout.Label($"Sword Type: {currentSwordType}");
//         GUILayout.Label($"Sword Count: {(swordOrbit != null ? swordOrbit.SwordCount : 0)}");
//         GUILayout.Label($"HP: {(character != null ? $"{character.CurrentHp:F0}/{character.MaxHp:F0}" : "N/A")}");
        
//         if (isDashing)
//             GUILayout.Label("<color=yellow>DASHING!</color>", new GUIStyle(GUI.skin.label) { richText = true });
//         else if (dashCooldownTimer > 0f)
//             GUILayout.Label($"Dash CD: {dashCooldownTimer:F1}s");

//         GUILayout.Label("\nPress H for controls");

//         GUILayout.EndVertical();
//         GUILayout.EndArea();
//     }
// }
