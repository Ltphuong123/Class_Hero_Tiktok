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

    private float currentHp;
    private Vector3 moveDirection;
    private SwordType currentSwordType = SwordType.Default;
    private int swordTypeCount;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private CharacterBase lastAttacker;
    private float lastAttackerTimer;

    private const float AttackerMemoryDuration = 3f; // nhớ kẻ tấn công trong 3 giây

    public Vector3 Position => transform.position;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public bool IsPlayerControlled => isPlayerControlled;
    public bool IsKnockedBack => knockbackTimer > 0f;
    public CharacterBase LastAttacker => lastAttackerTimer > 0f ? lastAttacker : null;

    private void Start()
    {
        currentHp = maxHp;
        swordTypeCount = System.Enum.GetValues(typeof(SwordType)).Length;
        if (swordOrbit != null) swordOrbit.IsPlayer = true;
        if (infoUI != null) infoUI.Init(characterName, avatar, currentHp, maxHp);
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
        // Giảm timer nhớ kẻ tấn công
        if (lastAttackerTimer > 0f) lastAttackerTimer -= deltaTime;

        // Knockback: đẩy nhân vật, giảm dần, không cho di chuyển
        if (knockbackTimer > 0f)
        {
            float t = knockbackTimer / knockbackDuration; // 1→0
            transform.position += knockbackVelocity * t * deltaTime;
            SyncZ();
            knockbackTimer -= deltaTime;
            return;
        }

        // Only process keyboard input for player-controlled characters
        if (!isPlayerControlled) return;

        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        if (Input.GetKey(KeyCode.D)) x = 1f;
        if (Input.GetKey(KeyCode.W)) y = 1f;
        if (Input.GetKey(KeyCode.S)) y = -1f;

        moveDirection = new Vector3(x, y, 0f).normalized;
        transform.position += moveDirection * moveSpeed * deltaTime;
        SyncZ();

        if (Input.GetKeyDown(KeyCode.Q) && swordOrbit != null)
            swordOrbit.IncreaseRadius(0.5f);

        if (Input.GetKeyDown(KeyCode.E) && swordOrbit != null)
        {
            currentSwordType = (SwordType)(((int)currentSwordType + 1) % swordTypeCount);
            swordOrbit.SetSwordType(currentSwordType);
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

        if (currentHp <= 0f) OnDeath();
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
    }

    private void OnDeath()
    {
        // Rơi tất cả kiếm đang orbit ra map
        if (swordOrbit != null)
        {
            int count = swordOrbit.SwordCount;
            for (int i = count - 1; i >= 0; i--)
                swordOrbit.DropSword(i);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Đẩy nhân vật theo hướng direction với lực force.
    /// Trong thời gian knockback, nhân vật không thể di chuyển.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        knockbackVelocity = (Vector3)(direction.normalized * force);
        knockbackTimer = knockbackDuration;
    }

    public SwordOrbit GetSwordOrbit() => swordOrbit;

    private void SyncZ()
    {
        Vector3 pos = transform.position;
        pos.z = pos.y + 25f;
        transform.position = pos;
    }
}
