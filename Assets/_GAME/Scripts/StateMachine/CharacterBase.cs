using UnityEngine;

public class CharacterBase : GameUnit, IManagedUpdate
{
    [Header("Character Info")]
    [SerializeField] private string characterId;
    [SerializeField] private string characterName = "Player";
    [SerializeField] private Sprite avatar;
    [SerializeField] private int characterLevel = 1;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private float knockbackCooldown = 0.5f;
    [SerializeField] private float characterKnockbackMultiplier = 2f;

    [Header("References")]
    [SerializeField] private SwordOrbit swordOrbit;
    [SerializeField] private CharacterInfoUI infoUI;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private CharacterLevelDataSO levelData;
    
    private float currentHp;
    private float lastFrameX;
    private int currentLevel;
    private float levelTimer;
    private bool isKnockedBack;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private float lastKnockbackTime;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public string CharacterId => characterId;
    public string CharacterName => characterName;
    public Sprite Avatar => avatar;
    public int SwordCount => swordOrbit?.SwordCount ?? 0;
    public string CurrentStateName => stateMachine?.CurrentState.GetType().Name ?? "None";
    public int CurrentLevel => currentLevel;
    public bool IsKnockedBack => isKnockedBack;
    public SwordOrbit GetSwordOrbit() => swordOrbit;
    public CharacterStateMachine GetStateMachine() => stateMachine;

    public void OnInit()
    {
        OnInit(characterId, characterName, avatar, characterLevel);
    }

    public void OnInit(string id, string name, Sprite avatarSprite, int level = 1)
    {
        characterId = id;
        characterName = name;
        avatar = avatarSprite;
        characterLevel = level;
        
        currentHp = maxHp;
        currentLevel = characterLevel;
        levelTimer = 0f;
        lastFrameX = TF.position.x;
        lastKnockbackTime = -knockbackCooldown;
        
        if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
        if (swordOrbit != null) swordOrbit.OnInit();
        if (stateMachine != null) stateMachine.OnInit();
        if (infoUI != null) infoUI.Init(characterName, avatar, currentHp, maxHp);
        
        UpdateLevelSwordType();
        CharacterManager.Instance.Register(this);
    }

    public void OnDespawn()
    {
        if (swordOrbit != null) swordOrbit.OnDespawn();
        if (stateMachine != null) stateMachine.OnDespawn();
        CharacterManager.Instance.Despawn(this);
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (isKnockedBack)
        {
            knockbackTimer -= deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                knockbackVelocity = Vector3.zero;
            }
            else
            {
                Vector3 newPos = TF.position + knockbackVelocity * deltaTime;
                if (MapManager.Instance != null)
                {
                    newPos = MapManager.Instance.ClampToMap(newPos);
                    if (!MapManager.Instance.IsBlockedWorld(newPos))
                        TF.position = newPos;
                    else
                        isKnockedBack = false;
                }
                else
                {
                    TF.position = newPos;
                }
            }
        }

        UpdateLevelTimer(deltaTime);

        if (stateMachine != null && !isKnockedBack) 
            stateMachine.ManagedUpdate(deltaTime);
        
        UpdateFacing();
    }

    private void UpdateLevelTimer(float deltaTime)
    {
        if (levelData == null) return;

        levelTimer += deltaTime;
        float duration = levelData.GetDuration(currentLevel);
        
        if (duration > 0f && levelTimer >= duration)
            LevelUp();
    }

    private void UpdateLevelSwordType()
    {
        if (levelData != null && swordOrbit != null)
            swordOrbit.SetSwordType(levelData.GetSwordType(currentLevel));
    }

    private void UpdateFacing()
    {
        if (visualTransform == null) return;

        float delta = TF.position.x - lastFrameX;

        if (Mathf.Abs(delta) > 0.01f)
        {
            Vector3 s = visualTransform.localScale;
            s.x = delta > 0f ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
            visualTransform.localScale = s;
            lastFrameX = TF.position.x;
        }
    }

    public void LevelUp()
    {
        if (levelData == null) return;

        int maxLevel = levelData.GetMaxLevel();
        if (currentLevel >= maxLevel) return;

        currentLevel++;
        levelTimer = 0f;
        UpdateLevelSwordType();
    }

    public void SetLevel(int level)
    {
        if (levelData == null) return;

        currentLevel = Mathf.Clamp(level, 1, levelData.GetMaxLevel());
        levelTimer = 0f;
        UpdateLevelSwordType();
    }

    public void TakeDamage(float damage, CharacterBase attacker = null)
    {
        currentHp = Mathf.Max(0f, currentHp - damage);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
        if (currentHp <= 0f) OnDeath();
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
    }

    public void MultiplySpeed(float multiplier) => moveSpeed *= multiplier;

    public void OnSwordInteraction(CharacterBase attacker)
    {
        if (attacker == null) return;

        float currentTime = Time.time;
        bool canKnockback = currentTime - lastKnockbackTime >= knockbackCooldown;
        
        if (!isKnockedBack && canKnockback)
        {
            Vector3 direction = (TF.position - attacker.TF.position).normalized;
            ApplyKnockback(direction, characterKnockbackMultiplier);
            lastKnockbackTime = currentTime;
            
            if (stateMachine != null)
                stateMachine.OnUnderAttack(attacker);
        }
    }

    public void OnSwordToSwordKnockback(CharacterBase attacker)
    {
        if (attacker == null) return;

        float currentTime = Time.time;
        bool canKnockback = currentTime - lastKnockbackTime >= knockbackCooldown;
        
        if (!isKnockedBack && canKnockback)
        {
            Vector3 direction = (TF.position - attacker.TF.position).normalized;
            ApplyKnockback(direction, 1f);
            lastKnockbackTime = currentTime;
            
            if (stateMachine != null)
                stateMachine.OnUnderAttack(attacker);
        }
    }

    private void ApplyKnockback(Vector3 direction, float multiplier = 1f)
    {
        isKnockedBack = true;
        knockbackVelocity = direction * knockbackForce * multiplier;
        knockbackTimer = knockbackDuration;
    }

    private void OnDeath()
    {
        if (stateMachine != null)
            stateMachine.ChangeState(stateMachine.Dead);
        else
            OnDespawn();
    }
}
