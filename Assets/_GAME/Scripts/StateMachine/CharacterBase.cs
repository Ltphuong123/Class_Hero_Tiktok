using UnityEngine;
using System.Collections;

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
    [SerializeField] private CharacterAudioSource audioSource;

    [Header("Particles")]
    [SerializeField] private LevelParticleSet[] levelParticleSets;
    
    [Header("Magnet Booster")]
    [SerializeField] private float magnetRadius = 10f;
    [SerializeField] private float magnetDuration = 5f;
    [SerializeField] private float magnetPullSpeed = 50f;
    [SerializeField] private ParticleSystem magnetParticle;
    
    [Header("Shield Booster")]
    [SerializeField] private float shieldDuration = 5f;
    [SerializeField] private ParticleSystem shieldParticle;
    
    [Header("Freeze Booster")]
    [SerializeField] private float freezeDuration = 5f;
    [SerializeField] private ParticleSystem freezeParticle;
    
    [Header("Meteor Booster")]
    [SerializeField] private float meteorRadius = 23f;
    [SerializeField] private float meteorDamage = 1000f;
    [SerializeField] private ParticleSystem meteorChargeParticle;
    [SerializeField] private ParticleUnit meteorImpactParticlePrefab;
    
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
    private Vector3 lastPosition;
    private bool isMoving;
    private MapManager cachedMap;
    private bool isMagnetActive;
    private float magnetTimer;
    private bool isShieldActive;
    private float shieldTimer;
    private bool isFrozen;
    private float frozenTimer;

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
    public bool IsMagnetActive => isMagnetActive;
    public float MagnetTimeRemaining => isMagnetActive ? Mathf.Max(0f, magnetTimer) : 0f;
    public bool IsShieldActive => isShieldActive;
    public float ShieldTimeRemaining => isShieldActive ? Mathf.Max(0f, shieldTimer) : 0f;
    public bool IsFrozen => isFrozen;
    public float FrozenTimeRemaining => isFrozen ? Mathf.Max(0f, frozenTimer) : 0f;
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
    public CharacterAudioSource GetAudioSource() => audioSource;

    public void OnInit() => OnInit(characterId, characterName, avatar, characterLevel);

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
        lastPosition = TF.position;
        isMoving = false;
        lastKnockbackTime = -knockbackCooldown;
        lastSwordCount = 0;
        has10SwordsActive = false;
        has20SwordsActive = false;
        isDead = false;
        isMagnetActive = false;
        magnetTimer = 0f;
        isShieldActive = false;
        shieldTimer = 0f;
        isFrozen = false;
        frozenTimer = 0f;
        
        if (levelData != null)
            levelReserveTime = new float[levelData.GetMaxLevel() + 1];
        
        if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<CharacterAudioSource>();
        if (cachedMap == null) cachedMap = MapManager.Instance;
        
        swordOrbit?.OnInit();
        stateMachine?.OnInit();
        
        if (infoUI != null)
        {
            infoUI.Init(characterName, avatar, currentHp, maxHp);
            infoUI.SetCharacter(this);
        }
        
        animator?.SetTrigger("walk");
        
        UpdateLevelStats();
        CharacterManager.Instance.Register(this);
    }

    public void OnDespawn()
    {
        audioSource?.StopFootstep();
        
        if (magnetParticle != null)
            magnetParticle.Stop();
        
        if (shieldParticle != null)
            shieldParticle.Stop();
        
        if (freezeParticle != null)
            freezeParticle.Stop();
        
        swordOrbit?.OnDespawn();
        stateMachine?.OnDespawn();
        CharacterManager.Instance.Despawn(this);
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (isDead)
        {
            audioSource?.StopFootstep();
            stateMachine?.ManagedUpdate(deltaTime);
            return;
        }

        if (isFrozen)
        {
            frozenTimer -= deltaTime;
            if (frozenTimer <= 0f)
                Unfreeze();
            
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
                if (cachedMap != null)
                {
                    newPos = cachedMap.ClampToMap(newPos);
                    if (!cachedMap.IsBlockedWorld(newPos))
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
        UpdateMagnetBooster(deltaTime);
        UpdateShieldBooster(deltaTime);

        if (stateMachine != null && !isKnockedBack) 
            stateMachine.ManagedUpdate(deltaTime);
        
        UpdateFacing();
        UpdateFootstepSound();
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

        swordOrbit?.SetSwordType(levelData.GetSwordType(currentLevel));
        moveSpeed = levelData.GetSpeed(currentLevel);

        float scale = levelData.GetBodyScale(currentLevel);
        TF.localScale = Vector3.one * scale;
        
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

    private void UpdateFootstepSound()
    {
        if (audioSource == null) return;

        Vector3 currentPos = TF.position;
        float dx = currentPos.x - lastPosition.x;
        float dy = currentPos.y - lastPosition.y;
        float distanceSq = dx * dx + dy * dy;
        
        bool wasMoving = isMoving;
        isMoving = distanceSq > 0.0001f;
        
        if (isMoving != wasMoving)
        {
            if (isMoving)
                audioSource.PlayFootstep();
            else
                audioSource.StopFootstep();
        }
        
        lastPosition = currentPos;
    }

    private void UpdateMagnetBooster(float deltaTime)
    {
        if (!isMagnetActive) return;

        magnetTimer -= deltaTime;
        if (magnetTimer <= 0f)
        {
            isMagnetActive = false;
            
            if (magnetParticle != null)
                magnetParticle.Stop();
            
            return;
        }

        ItemManager itemMgr = ItemManager.Instance;
        if (itemMgr == null) return;

        var nearbySwords = new System.Collections.Generic.List<Sword>();
        itemMgr.GetNearbySwords(TF.position, magnetRadius, nearbySwords);

        foreach (var sword in nearbySwords)
        {
            if (sword.State != SwordState.Dropped) continue;

            Vector3 toCharacter = TF.position - sword.TF.position;
            float distanceSq = toCharacter.x * toCharacter.x + toCharacter.y * toCharacter.y;
            
            if (distanceSq < 1f)
            {
                sword.Collect(this);
                continue;
            }

            float distance = Mathf.Sqrt(distanceSq);
            Vector3 direction = toCharacter / distance;
            float moveAmount = Mathf.Min(magnetPullSpeed * deltaTime, distance);
            Vector3 newPos = sword.TF.position + direction * moveAmount;
            
            if (cachedMap != null)
                newPos = cachedMap.ClampToMap(newPos);
            
            sword.TF.position = newPos;
        }
    }

    public void ActivateMagnetBooster()
    {
        isMagnetActive = true;
        magnetTimer = magnetDuration;
        
        if (magnetParticle != null)
            magnetParticle.Play();
    }

    private void UpdateShieldBooster(float deltaTime)
    {
        if (!isShieldActive) return;

        shieldTimer -= deltaTime;
        if (shieldTimer <= 0f)
        {
            isShieldActive = false;
            
            if (shieldParticle != null)
                shieldParticle.Stop();
        }
    }

    public void ActivateShieldBooster()
    {
        isShieldActive = true;
        shieldTimer = shieldDuration;
        
        if (shieldParticle != null)
            shieldParticle.Play();
    }

    public void Freeze(float duration)
    {
        if (isFrozen) return;
        
        isFrozen = true;
        frozenTimer = duration;
        
        if (animator != null)
            animator.speed = 0f;
        
        if (swordOrbit != null)
            swordOrbit.SetPaused(true);
        
        if (audioSource != null)
            audioSource.StopFootstep();
        
        if (freezeParticle != null)
            freezeParticle.Play();
    }

    public void Unfreeze()
    {
        isFrozen = false;
        
        if (animator != null)
            animator.speed = 1f;
        
        if (swordOrbit != null)
            swordOrbit.SetPaused(false);
        
        if (freezeParticle != null)
            freezeParticle.Stop();
    }

    public void ActivateFreezeBooster()
    {
        CharacterManager charMgr = CharacterManager.Instance;
        if (charMgr == null) return;

        var nearbyCharacters = new System.Collections.Generic.List<CharacterBase>();
        charMgr.GetNearbyCharacters(TF.position, 1000f, nearbyCharacters);

        foreach (var character in nearbyCharacters)
        {
            if (character == this) continue;
            if (character.IsDead) continue;
            
            character.Freeze(freezeDuration);
        }
    }

    public void ActivateMeteorBooster()
    {
        StartCoroutine(MeteorBoosterSequence());
    }

    private IEnumerator MeteorBoosterSequence()
    {
        CameraController camController = Camera.main?.GetComponent<CameraController>();
        if (camController == null) yield break;

        Vector3 originalPosition = TF.position;
        Vector3 originalScale = TF.localScale;
        float originalBodyScale = levelData != null ? levelData.GetBodyScale(currentLevel) : 1f;

        TF.position = Vector3.zero;
        TF.localScale = Vector3.one * 3f;
        camController.SetTarget(TF);
        camController.SetZoom(30f);

        yield return new WaitForSeconds(0.1f);

        CharacterManager charMgr = CharacterManager.Instance;
        var enemies = new System.Collections.Generic.List<CharacterBase>();
        if (charMgr != null)
        {
            charMgr.GetNearbyCharacters(TF.position, 1000f, enemies);
            foreach (var enemy in enemies)
            {
                if (enemy == this) continue;
                if (enemy.IsDead) continue;
                enemy.Freeze(10f);
            }
        }

        if (meteorChargeParticle != null)
            meteorChargeParticle.Play();

        yield return new WaitForSeconds(0.5f);

        StartCoroutine(SpawnMeteorImpacts());

        yield return new WaitForSeconds(1f);

        var targets = new System.Collections.Generic.List<CharacterBase>();
        if (charMgr != null)
        {
            charMgr.GetCharactersInRadius(TF.position, meteorRadius, targets);
            foreach (var target in targets)
            {
                if (target == this) continue;
                if (target.IsDead) continue;
                target.TakeDamage(meteorDamage, this);
            }
        }

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            enemy.Unfreeze();
        }

        yield return new WaitForSeconds(1f);

        if (meteorChargeParticle != null)
            meteorChargeParticle.Stop();

        if (camController != null)
            camController.SetZoom(camController.GetTargetFollowZoom());

        TF.localScale = Vector3.one * originalBodyScale;
    }

    private IEnumerator SpawnMeteorImpacts()
    {
        if (meteorImpactParticlePrefab == null) yield break;

        int totalImpacts = 300;
        float duration = 2f;
        float interval = duration / totalImpacts;

        for (int i = 0; i < totalImpacts; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * meteorRadius;
            Vector3 spawnPos = TF.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            ParticlePool.Spawn(meteorImpactParticlePrefab.ParticleType, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(interval);
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
        {
            SwitchLevelParticles(oldLevel);
            if (currentLevel > oldLevel)
                audioSource?.PlayLevelUp();
        }
    }

    public int GetMaxLevel() => levelData?.GetMaxLevel() ?? 1;
    public CharacterLevelDataSO GetLevelData() => levelData;
    public float GetLevelDuration()
    {
        if (levelData == null || levelReserveTime == null || currentLevel >= levelReserveTime.Length) return 0f;
        return levelReserveTime[currentLevel];
    }

    private void CheckSwordCountParticles()
    {
        int swordCount = SwordCount;
        
        if (swordCount == lastSwordCount || currentParticleSet == null) return;

        if (swordCount >= 20 && !has20SwordsActive)
        {
            has20SwordsActive = true;
            currentParticleSet.sword20Particle?.Play();
        }
        else if (swordCount < 20 && has20SwordsActive)
        {
            has20SwordsActive = false;
            currentParticleSet.sword20Particle?.Stop();
        }
        
        if (swordCount >= 10 && !has10SwordsActive)
        {
            has10SwordsActive = true;
            currentParticleSet.sword10Particle?.Play();
        }
        else if (swordCount < 10 && has10SwordsActive)
        {
            has10SwordsActive = false;
            currentParticleSet.sword10Particle?.Stop();
        }
        
        lastSwordCount = swordCount;
    }

    private void SwitchLevelParticles(int oldLevel)
    {
        if (levelParticleSets == null || levelParticleSets.Length == 0) return;
        
        LevelParticleSet oldParticleSet = GetParticleSetForLevel(oldLevel);
        if (oldParticleSet != null)
        {
            oldParticleSet.levelParticle?.Stop();
            oldParticleSet.sword10Particle?.Stop();
            oldParticleSet.sword20Particle?.Stop();
        }
        
        currentParticleSet = GetParticleSetForLevel(currentLevel);
        
        if (currentParticleSet != null)
        {
            currentParticleSet.levelParticle?.Play();
            
            int swordCount = SwordCount;
            
            if (swordCount >= 10)
            {
                currentParticleSet.sword10Particle?.Play();
                has10SwordsActive = true;
            }
            
            if (swordCount >= 20)
            {
                currentParticleSet.sword20Particle?.Play();
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
        if (isDead || isShieldActive) return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        infoUI?.UpdateHp(currentHp, maxHp);
        
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
        infoUI?.UpdateHp(currentHp, maxHp);
    }

    public void MultiplySpeed(float multiplier) => moveSpeed *= multiplier;

    public void OnSwordInteraction(CharacterBase attacker)
    {
        if (isDead || isShieldActive || attacker == null) return;

        float currentTime = Time.time;
        if (isKnockedBack || currentTime - lastKnockbackTime < knockbackCooldown) return;
        
        Vector3 direction = (TF.position - attacker.TF.position).normalized;
        ApplyKnockback(direction, characterKnockbackMultiplier);
        lastKnockbackTime = currentTime;
        
        stateMachine?.OnUnderAttack(attacker);
    }

    public void OnSwordToSwordKnockback(CharacterBase attacker)
    {
        if (isDead || isShieldActive || attacker == null) return;

        float currentTime = Time.time;
        if (isKnockedBack || currentTime - lastKnockbackTime < knockbackCooldown) return;
        
        Vector3 direction = (TF.position - attacker.TF.position).normalized;
        ApplyKnockback(direction, 1f);
        lastKnockbackTime = currentTime;
        
        stateMachine?.OnUnderAttack(attacker);
    }

    private void ApplyKnockback(Vector3 direction, float multiplier)
    {
        isKnockedBack = true;
        knockbackVelocity = direction * knockbackForce * multiplier;
        knockbackTimer = knockbackDuration;
    }

    private void OnDeath()
    {
        isDead = true;

        audioSource?.PlayDeath();

        if (swordOrbit != null)
        {
            int count = swordOrbit.SwordCount;
            for (int i = count - 1; i >= 0; i--)
                swordOrbit.DropSword(i);
        }

        animator?.SetTrigger("die");

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
// viết 1 booster thả thiên thạch như sau kích hoạt bằng CharacterActionMenu thự hiện các bước như sau: 

// - bước 1: cho nó về vị trí 0,0  và cho nó TF.localScale lên 3 và cho camera target vào nó và cho camera zoom lên 30

// - bước 2: cho đóng băng tất cả kẻ địch  sau đó chạy 1 partical A chỉ cần chạy lên thôi tôi sẽ để sẵn.

//  -bước 3: sau 0.5 giây cho spawn  ranbom 300 partical B trong vòng 2 giây bằng hệ thống ParticlePool spawn random vị trí đảm bảo trong vùng bán kính 23 so với người 

// -bước 4: tìm các các kẻ địch xung quanh bán kính 23  sau 1 giây thì gây 1000 sát thương lên các kẻ địch đó và sau đó tắt đóng băng kertaats cả kẻ địch

// -bước 5: sau 1 giây tắt  partical A và  cho camera zoom về targetFollowZoom và cho TF.localScale về levelData.GetBodyScale(currentLevel) hiện tại gema chạy như bình thường