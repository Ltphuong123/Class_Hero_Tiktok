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
    [SerializeField] private float knockbackDuration = 0.3f;

    [Header("State Machine")]
    [SerializeField] private float visionRadius = 15f;
    [SerializeField] private float attackKeepDistance = 1f;
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

        // Khởi tạo state machine cho non-player characters
        if (!isPlayerControlled)
        {
            stateMachine = new CharacterStateMachine(
                this,
                visionRadius,
                attackKeepDistance,
                fleeSpeedMultiplier,
                fleeSpeedDuration,
                fleeSpeedCooldown,
                stateMinDuration
            );
            stateMachine.Start();
        }
    }

    private void OnEnable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.Deregister(this);
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (lastAttackerTimer > 0f) lastAttackerTimer -= deltaTime;

        // Knockback physics (áp dụng cho cả player và non-player)
        if (knockbackTimer > 0f)
        {
            float t = knockbackTimer / knockbackDuration;
            transform.position += knockbackVelocity * t * deltaTime;
            knockbackTimer -= deltaTime;

            // Non-player: state machine vẫn chạy (KnockbackState chờ hết timer)
            if (!isPlayerControlled && stateMachine != null)
                stateMachine.Update(deltaTime);

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
            // State machine update
            if (stateMachine != null)
                stateMachine.Update(deltaTime);
        }
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
