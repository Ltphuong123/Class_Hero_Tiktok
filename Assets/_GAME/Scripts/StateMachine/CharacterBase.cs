using UnityEngine;

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
    [SerializeField] private Animator animator;

    [Header("Particles")]
    [SerializeField] private LevelParticleSet[] levelParticleSets;
    
    private float currentHp;
    private float lastFrameX;
    private int currentLevel;
    private float levelTimer;
    private float[] levelReserveTime;
    private bool isKnockedBack;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private float lastKnockbackTime;
    private int lastSwordCount;
    private bool has10SwordsActive;
    private bool has20SwordsActive;
    private LevelParticleSet currentParticleSet;
    private bool isDead;

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
    public bool IsDead => isDead;
    public float LevelTimeRemaining
    {
        get
        {
            if (levelData == null || currentLevel == 1 || levelReserveTime == null) return 0f;
            return Mathf.Max(0f, levelReserveTime[currentLevel] - levelTimer);
        }
    }
    public SwordOrbit GetSwordOrbit() => swordOrbit;
    public CharacterStateMachine GetStateMachine() => stateMachine;
    public Animator GetAnimator() => animator;

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
        currentLevel = 1;
        levelTimer = 0f;
        lastFrameX = TF.position.x;
        lastKnockbackTime = -knockbackCooldown;
        lastSwordCount = 0;
        has10SwordsActive = false;
        has20SwordsActive = false;
        isDead = false;
        
        if (levelData != null)
        {
            int maxLevel = levelData.GetMaxLevel();
            levelReserveTime = new float[maxLevel + 1];
        }
        
        if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (swordOrbit != null) swordOrbit.OnInit();
        if (stateMachine != null) stateMachine.OnInit();
        if (infoUI != null)
        {
            infoUI.Init(characterName, avatar, currentHp, maxHp);
            infoUI.SetCharacter(this);
        }
        
        if (animator != null)
            animator.SetTrigger("walk");
        
        UpdateLevelStats();
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
        if (isDead)
        {
            if (stateMachine != null)
                stateMachine.ManagedUpdate(deltaTime);
            return;
        }

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
        CheckSwordCountParticles();

        if (stateMachine != null && !isKnockedBack) 
            stateMachine.ManagedUpdate(deltaTime);
        
        UpdateFacing();
    }

    private void UpdateLevelTimer(float deltaTime)
    {
        if (levelData == null || currentLevel == 1) return;

        levelTimer += deltaTime;
        
        if (levelTimer >= levelReserveTime[currentLevel])
        {
            levelReserveTime[currentLevel] = 0f;
            levelTimer = 0f;
            
            int nextLevel = GetHighestAvailableLevel();
            if (nextLevel != currentLevel)
                SetLevel(nextLevel);
        }
    }

    private int GetHighestAvailableLevel()
    {
        if (levelReserveTime == null) return 1;
        
        for (int i = levelReserveTime.Length - 1; i >= 1; i--)
        {
            if (levelReserveTime[i] > 0f)
                return i;
        }
        
        return 1;
    }

    public void AddLevelReserveTime(int level, float time)
    {
        if (levelData == null || levelReserveTime == null) return;
        
        int maxLevel = levelData.GetMaxLevel();
        if (level < 1 || level > maxLevel) return;
        
        levelReserveTime[level] += time;
        
        int highestLevel = GetHighestAvailableLevel();
        if (highestLevel > currentLevel)
            SetLevel(highestLevel);
    }

    public float GetLevelReserveTime(int level)
    {
        if (levelReserveTime == null || level < 0 || level >= levelReserveTime.Length)
            return 0f;
        
        return levelReserveTime[level];
    }

    private void UpdateLevelStats()
    {
        if (levelData == null) return;

        if (swordOrbit != null)
            swordOrbit.SetSwordType(levelData.GetSwordType(currentLevel));

        moveSpeed = levelData.GetSpeed(currentLevel);

        float scale = levelData.GetBodyScale(currentLevel);
        
        // Scale toàn bộ character (bao gồm collider, orbit)
        TF.localScale = Vector3.one * scale;
        
        // Nếu có visualTransform riêng, reset về 1 để tránh double scale
        if (visualTransform != null && visualTransform != TF)
        {
            Vector3 currentScale = visualTransform.localScale;
            float signX = currentScale.x >= 0 ? 1f : -1f;
            visualTransform.localScale = new Vector3(signX, 1f, 1f);
        }
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
        UpdateLevelStats();
    }

    public void SetLevel(int level)
    {
        if (levelData == null) return;

        int oldLevel = currentLevel;
        currentLevel = Mathf.Clamp(level, 1, levelData.GetMaxLevel());
        levelTimer = 0f;
        
        has10SwordsActive = false;
        has20SwordsActive = false;
        
        UpdateLevelStats();
        
        if (oldLevel != currentLevel)
            SwitchLevelParticles(oldLevel);
    }

    public int GetMaxLevel()
    {
        if (levelData == null) return 1;
        return levelData.GetMaxLevel();
    }

    public CharacterLevelDataSO GetLevelData()
    {
        return levelData;
    }

    public float GetLevelDuration()
    {
        if (levelData == null || levelReserveTime == null || currentLevel >= levelReserveTime.Length) return 0f;
        return levelReserveTime[currentLevel];
    }

    private void CheckSwordCountParticles()
    {
        int swordCount = SwordCount;
        
        if (swordCount != lastSwordCount)
        {
            if (currentParticleSet != null)
            {
                // Check 20 swords
                if (swordCount >= 20 && !has20SwordsActive)
                {
                    has20SwordsActive = true;
                    if (currentParticleSet.sword20Particle != null)
                        currentParticleSet.sword20Particle.Play();
                }
                else if (swordCount < 20 && has20SwordsActive)
                {
                    has20SwordsActive = false;
                    if (currentParticleSet.sword20Particle != null)
                        currentParticleSet.sword20Particle.Stop();
                }
                
                // Check 10 swords
                if (swordCount >= 10 && !has10SwordsActive)
                {
                    has10SwordsActive = true;
                    if (currentParticleSet.sword10Particle != null)
                        currentParticleSet.sword10Particle.Play();
                }
                else if (swordCount < 10 && has10SwordsActive)
                {
                    has10SwordsActive = false;
                    if (currentParticleSet.sword10Particle != null)
                        currentParticleSet.sword10Particle.Stop();
                }
            }
            
            lastSwordCount = swordCount;
        }
    }

    private void SwitchLevelParticles(int oldLevel)
    {
        if (levelParticleSets == null || levelParticleSets.Length == 0) return;
        
        LevelParticleSet oldParticleSet = GetParticleSetForLevel(oldLevel);
        if (oldParticleSet != null)
        {
            if (oldParticleSet.levelParticle != null)
                oldParticleSet.levelParticle.Stop();
            if (oldParticleSet.sword10Particle != null)
                oldParticleSet.sword10Particle.Stop();
            if (oldParticleSet.sword20Particle != null)
                oldParticleSet.sword20Particle.Stop();
        }
        
        currentParticleSet = GetParticleSetForLevel(currentLevel);
        
        if (currentParticleSet != null)
        {
            if (currentParticleSet.levelParticle != null)
                currentParticleSet.levelParticle.Play();
            
            int swordCount = SwordCount;
            
            if (swordCount >= 10)
            {
                if (currentParticleSet.sword10Particle != null)
                    currentParticleSet.sword10Particle.Play();
                has10SwordsActive = true;
            }
            
            if (swordCount >= 20)
            {
                if (currentParticleSet.sword20Particle != null)
                    currentParticleSet.sword20Particle.Play();
                has20SwordsActive = true;
            }
        }
    }

    private LevelParticleSet GetParticleSetForLevel(int level)
    {
        if (levelParticleSets == null) return null;
        
        foreach (var set in levelParticleSets)
        {
            if (set.level == level)
                return set;
        }
        
        return null;
    }

    public void TakeDamage(float damage, CharacterBase attacker = null)
    {
        if (isDead) return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
        if (currentHp <= 0f)
        {
            OnKilledBy(attacker);
            OnDeath();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
    }

    public void MultiplySpeed(float multiplier) => moveSpeed *= multiplier;

    public void OnSwordInteraction(CharacterBase attacker)
    {
        if (isDead || attacker == null) return;

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
        if (isDead || attacker == null) return;

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
        isDead = true;

        if (swordOrbit != null)
        {
            int count = swordOrbit.SwordCount;
            for (int i = count - 1; i >= 0; i--)
                swordOrbit.DropSword(i);
        }

        if (animator != null)
        {
            animator.SetTrigger("die");
        }

        if (stateMachine != null)
            stateMachine.ChangeState(stateMachine.Dead);
        else
            OnDespawn();
    }

    public void OnKilledBy(CharacterBase killer)
    {
        if (killer != null && EventNotificationManager.Instance != null)
            EventNotificationManager.Instance.ShowKillNotification(killer.CharacterName, characterName);
    }
}
