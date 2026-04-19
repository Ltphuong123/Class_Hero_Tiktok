using UnityEngine;

public class CharacterBase : MonoBehaviour, IManagedUpdate
{
    [Header("Character Info")]
    [SerializeField] private int characterId;
    [SerializeField] private string characterName = "Player";
    [SerializeField] private Sprite avatar;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private SwordOrbit swordOrbit;
    [SerializeField] private CharacterInfoUI infoUI;
    
    [Header("Visual")]
    [SerializeField] private Transform visualTransform;

    [Header("State Machine")]
    [SerializeField] private CharacterStateMachine stateMachine;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.1f;
    
    private float currentHp;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private CharacterBase lastAttacker;
    private float lastAttackerTimer;
    private float lastFrameX;

    private const float AttackerMemoryDuration = 3f;

    public Vector3 Position => transform.position;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public int CharacterId => characterId;
    public string CharacterName => characterName;
    public Sprite Avatar => avatar;
    public int SwordCount => swordOrbit != null ? swordOrbit.SwordCount : 0;
    public bool IsKnockedBack => knockbackTimer > 0f;
    public CharacterBase LastAttacker => lastAttackerTimer > 0f ? lastAttacker : null;
    public string CurrentStateName => stateMachine?.CurrentState?.GetType().Name ?? "None";

    private void Start()
    {
        currentHp = maxHp;
        if (infoUI != null) infoUI.Init(characterName, avatar, currentHp, maxHp);

        if (stateMachine == null)
            stateMachine = GetComponent<CharacterStateMachine>();
    }

    private void OnEnable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.Register(this);

        if (stateMachine != null && stateMachine.CurrentState == stateMachine.Dead)
        {
            currentHp = maxHp;
            if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
        }
    }

    private void OnDisable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.Deregister(this);
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (lastAttackerTimer > 0f) lastAttackerTimer -= deltaTime;

        Vector3 curPos = transform.position;

        if (knockbackTimer > 0f)
        {
            float t = knockbackTimer / knockbackDuration;
            float nextX = curPos.x + knockbackVelocity.x * t * deltaTime;
            float nextY = curPos.y + knockbackVelocity.y * t * deltaTime;
            float z = curPos.z;

            var map = MapManager.Instance;
            if (map != null)
            {
                float midX = (curPos.x + nextX) * 0.5f;
                float midY = (curPos.y + nextY) * 0.5f;

                // Check diagonal + midpoint
                if (map.IsBlockedWorld(new Vector3(nextX, nextY, z)) || 
                    map.IsBlockedWorld(new Vector3(midX, midY, z)))
                {
                    // Try X-axis only
                    if (!map.IsBlockedWorld(new Vector3(nextX, curPos.y, z)))
                    {
                        nextY = curPos.y;
                    }
                    // Try Y-axis only
                    else if (!map.IsBlockedWorld(new Vector3(curPos.x, nextY, z)))
                    {
                        nextX = curPos.x;
                    }
                    // Stuck - don't move
                    else
                    {
                        nextX = curPos.x;
                        nextY = curPos.y;
                    }
                }
            }

            transform.position = new Vector3(nextX, nextY, z);
            knockbackTimer -= deltaTime;

            if (stateMachine != null)
                stateMachine.ManagedUpdate(deltaTime);

            return;
        }

        if (stateMachine != null)
            stateMachine.ManagedUpdate(deltaTime);

        UpdateFacing(curPos.x);
    }

    private void UpdateFacing(float currentX)
    {
        if (visualTransform == null) return;

        float delta = currentX - lastFrameX;

        if (delta > 0.01f)
        {
            Vector3 s = visualTransform.localScale;
            s.x = -Mathf.Abs(s.x);
            visualTransform.localScale = s;
            lastFrameX = currentX;
        }
        else if (delta < -0.01f)
        {
            Vector3 s = visualTransform.localScale;
            s.x = Mathf.Abs(s.x);
            visualTransform.localScale = s;
            lastFrameX = currentX;
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

        if (stateMachine != null)
            stateMachine.OnTakeDamage(attacker);

        if (currentHp <= 0f) OnDeath();
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
    }

    /// <summary>
    /// Nhân tốc độ di chuyển (dùng cho debug/cheat).
    /// </summary>
    public void MultiplySpeed(float multiplier)
    {
        moveSpeed *= multiplier;
    }

    /// <summary>
    /// Set tốc độ di chuyển trực tiếp.
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    private void OnDeath()
    {
        if (stateMachine != null)
        {
            stateMachine.ChangeState(stateMachine.Dead);
        }
        else
        {
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

        if (stateMachine != null)
            stateMachine.OnKnockback();
    }

    public CharacterStateMachine GetStateMachine() => stateMachine;
}
