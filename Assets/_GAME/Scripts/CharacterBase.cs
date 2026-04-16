using UnityEngine;

public class CharacterBase : MonoBehaviour, IManagedUpdate
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private SwordOrbit swordOrbit;
    [SerializeField] private bool isPlayerControlled = false;

    [Header("Character Info")]
    [SerializeField] private string characterName = "Player";
    [SerializeField] private Sprite avatar;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private CharacterInfoUI infoUI;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.1f;

    [Header("Visual")]
    [SerializeField] private Transform visualTransform; // GameObject để flip scale X

    [Header("State Machine")]
    [SerializeField] private float visionRadius = 15f;
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float fleeSpeedMultiplier = 1.6f;
    [SerializeField] private float fleeSpeedDuration = 1.2f;
    [SerializeField] private float fleeSpeedCooldown = 5f;
    [SerializeField] private float stateMinDuration = 0.4f;

    private float currentHp;
    private SwordType currentSwordType = SwordType.Default;
    private int swordTypeCount;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private CharacterBase lastAttacker;
    private float lastAttackerTimer;

    private CharacterStateMachine stateMachine;
    private float lastFrameX;

    private const float AttackerMemoryDuration = 3f;

    public Vector3 Position => transform.position;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public bool IsPlayerControlled => isPlayerControlled;
    public bool IsKnockedBack => knockbackTimer > 0f;
    public CharacterBase LastAttacker => lastAttackerTimer > 0f ? lastAttacker : null;

    /// <summary>
    /// Tên state hiện tại (dùng cho debug).
    /// </summary>
    public string CurrentStateName => stateMachine?.CurrentState?.GetType().Name ?? "None";

    private void Start()
    {
        currentHp = maxHp;
        swordTypeCount = System.Enum.GetValues(typeof(SwordType)).Length;
        if (infoUI != null) infoUI.Init(characterName, avatar, currentHp, maxHp);

        if (!isPlayerControlled)
            EnsureStateMachine();
    }

    private void OnEnable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.Register(this);

        // Respawn: reset HP và state machine nếu đã chết
        if (!isPlayerControlled && stateMachine != null && stateMachine.CurrentState == stateMachine.Dead)
        {
            currentHp = maxHp;
            if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
            stateMachine.Start();
        }
    }

    private void OnDisable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.Deregister(this);
    }

    /// <summary>
    /// Đảm bảo state machine luôn được tạo. Gọi từ Start và ManagedUpdate.
    /// </summary>
    private void EnsureStateMachine()
    {
        if (stateMachine != null) return;

        stateMachine = new CharacterStateMachine(
            this,
            visionRadius,
            separationRadius,
            fleeSpeedMultiplier,
            fleeSpeedDuration,
            fleeSpeedCooldown,
            stateMinDuration
        );
        stateMachine.Start();
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (lastAttackerTimer > 0f) lastAttackerTimer -= deltaTime;

        // Knockback physics (áp dụng cho cả player và non-player)
        if (knockbackTimer > 0f)
        {
            float t = knockbackTimer / knockbackDuration;
            Vector3 nextPos = transform.position + knockbackVelocity * t * deltaTime;
            Vector3 curPos = transform.position;

            // Chặn knockback nếu vị trí mới hoặc đường đi nằm trong tường
            var map = MapManager.Instance;
            if (map != null)
            {
                Vector3 mid = new Vector3(
                    (curPos.x + nextPos.x) * 0.5f,
                    (curPos.y + nextPos.y) * 0.5f,
                    curPos.z
                );

                if (map.IsWall(nextPos) || map.IsWall(mid))
                {
                    // Thử chỉ di chuyển theo từng trục
                    Vector3 posX = new Vector3(nextPos.x, curPos.y, curPos.z);
                    Vector3 posY = new Vector3(curPos.x, nextPos.y, curPos.z);

                    if (!map.IsWall(posX))
                        transform.position = posX;
                    else if (!map.IsWall(posY))
                        transform.position = posY;
                    // Cả 2 trục đều tường → đứng yên, không đẩy vào tường
                }
                else
                {
                    transform.position = nextPos;
                }
            }
            else
            {
                transform.position = nextPos;
            }

            knockbackTimer -= deltaTime;

            // Non-player: state machine vẫn chạy (KnockbackState chờ hết timer)
            if (!isPlayerControlled)
            {
                EnsureStateMachine();
                stateMachine.Update(deltaTime);
            }

            return;
        }

        if (isPlayerControlled)
        {
            // Player input
            if (Input.GetKeyDown(KeyCode.Q) && swordOrbit != null)
                swordOrbit.IncreaseRadius(0.5f);

            if (Input.GetKeyDown(KeyCode.E) && swordOrbit != null)
            {
                currentSwordType = (SwordType)(((int)currentSwordType + 1) % swordTypeCount);
                swordOrbit.SetSwordType(currentSwordType);
            }
        }
        else
        {
            // State machine update — đảm bảo luôn có state machine
            if (!isPlayerControlled)
            {
                EnsureStateMachine();
                stateMachine.Update(deltaTime);
            }
        }

        // Flip visual dựa trên hướng di chuyển
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if (visualTransform == null) return;

        float currentX = transform.position.x;
        float delta = currentX - lastFrameX;

        if (delta > 0.01f)
        {
            // Di chuyển sang phải → scale X = -1
            Vector3 s = visualTransform.localScale;
            s.x = -Mathf.Abs(s.x);
            visualTransform.localScale = s;
        }
        else if (delta < -0.01f)
        {
            // Di chuyển sang trái → scale X = 1
            Vector3 s = visualTransform.localScale;
            s.x = Mathf.Abs(s.x);
            visualTransform.localScale = s;
        }

        lastFrameX = currentX;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, CharacterBase attacker)
    {
        currentHp = Mathf.Max(0f, currentHp - damage);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);

        if (attacker != null)
        {
            lastAttacker = attacker;
            lastAttackerTimer = AttackerMemoryDuration;
        }

        // Thông báo state machine
        if (!isPlayerControlled && stateMachine != null)
            stateMachine.OnTakeDamage(attacker);

        if (currentHp <= 0f) OnDeath();
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
    }

    private void OnDeath()
    {
        if (!isPlayerControlled && stateMachine != null)
        {
            // State machine xử lý death (rơi kiếm, disable)
            stateMachine.ChangeState(stateMachine.Dead);
        }
        else
        {
            // Player death — xử lý trực tiếp
            if (swordOrbit != null)
            {
                int count = swordOrbit.SwordCount;
                for (int i = count - 1; i >= 0; i--)
                    swordOrbit.DropSword(i);
            }
            gameObject.SetActive(false);
        }
    }

    public SwordOrbit GetSwordOrbit() => swordOrbit;

    public void ApplyKnockback(Vector2 direction, float force)
    {
        knockbackVelocity = (Vector3)(direction.normalized * force);
        knockbackTimer = knockbackDuration;

        // Thông báo state machine chuyển sang KnockbackState
        if (!isPlayerControlled && stateMachine != null)
            stateMachine.OnKnockback();
    }

    /// <summary>
    /// Lấy state machine (dùng cho debug hoặc external access).
    /// </summary>
    public CharacterStateMachine GetStateMachine() => stateMachine;
}
