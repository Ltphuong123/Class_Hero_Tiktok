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

    [Header("References")]
    [SerializeField] private SwordOrbit swordOrbit;
    [SerializeField] private CharacterInfoUI infoUI;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private CharacterLevelDataSO levelData;
    
    private float currentHp;
    private float lastFrameX;
    private float fleeProtectionTimer;
    private int currentLevel;
    private float levelTimer;

    private const float FleeProtectionDuration = 3f;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public string CharacterId => characterId;
    public string CharacterName => characterName;
    public Sprite Avatar => avatar;
    public int SwordCount => swordOrbit != null ? swordOrbit.SwordCount : 0;
    public string CurrentStateName => stateMachine != null ? stateMachine.CurrentState.GetType().Name : "None";
    public bool IsFleeProtected => fleeProtectionTimer > 0f;
    public int CurrentLevel => currentLevel;
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
        fleeProtectionTimer = 0f;
        lastFrameX = TF.position.x;
        
        if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
        if (infoUI != null) infoUI.Init(characterName, avatar, currentHp, maxHp);
        
        UpdateLevelSwordType();
        CharacterManager.Instance?.Register(this);
    }

    public void OnDespawn()
    {
        if (swordOrbit != null)
        {
            for (int i = swordOrbit.SwordCount - 1; i >= 0; i--)
                swordOrbit.DropSword(i);
        }
        
        if (stateMachine != null)
            stateMachine.ChangeState(stateMachine.Wander);
        
        CharacterManager.Instance?.Despawn(this);
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (fleeProtectionTimer > 0f) 
            fleeProtectionTimer -= deltaTime;

        UpdateLevelTimer(deltaTime);

        if (stateMachine != null) 
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

    public void ActivateFleeProtection() => fleeProtectionTimer = FleeProtectionDuration;

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
        if (stateMachine != null) stateMachine.OnUnderAttack(attacker);
        if (currentHp <= 0f) OnDeath();
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
    }

    public void MultiplySpeed(float multiplier) => moveSpeed *= multiplier;

    public void OnSwordInteraction(CharacterBase other)
    {
        // TODO: Implement sword interaction logic
    }

    private void OnDeath()
    {
        if (stateMachine != null)
            stateMachine.ChangeState(stateMachine.Dead);
        else
            OnDespawn();
    }
}
