using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelParticleGroup
{
    public ParticleSystem[] particles;
}

public class CharacterBase : GameUnit, IManagedUpdate
{
    [Header("Character Info")]
    [SerializeField] private CharacterBaseConfigSO config;
    [SerializeField] private int characterNumericId;
    [SerializeField] private string characterId;
    [SerializeField] private string characterName = "Player";
    [SerializeField] private Sprite avatar;
    [SerializeField] private int characterLevel = 1;

    [Header("References")]
    [SerializeField] private SwordOrbit swordOrbit;
    [SerializeField] private CharacterInfoUI infoUI;
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private CharacterLevelDataSO levelData;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterAudioSource audioSource;
    [SerializeField] private SkillController skillController;
    [SerializeField] private MagicFX5_EnemyDisintegration disintegration;

    [Header("Level Particles")]
    [Tooltip("Mỗi phần tử = 1 level (index 0 = level 1). Mỗi group có thể chứa nhiều particle. Để trống nếu level không có particle.")]
    [SerializeField] private LevelParticleGroup[] levelParticles;

    [Header("Booster Particles")]
    [SerializeField] private ParticleSystem magnetParticle;
    [SerializeField] private ParticleSystem shieldParticle;
    [SerializeField] private ParticleSystem healParticle;

    private float lifestealCooldown = 0.5f;
    private float magnetRadius = 10f;
    private float magnetDuration = 5f;
    private float magnetPullSpeed = 50f;
    private float shieldDuration = 5f;
    private float freezeDuration = 5f;

    private float maxHp                      => config != null ? config.maxHp : 100f;
    private int   maxSwordCount              => config != null ? config.maxSwordCount : 30;
    private float overhealScalePerThreshold  => config != null ? config.overhealScalePerThreshold : 0.1f;
    private float overhealThreshold          => config != null ? config.overhealThreshold : 500f;
    private int   maxSwordQueue              => config != null ? config.maxSwordQueue : 5000;
    private float lifestealPercentConfig     => config != null ? config.lifestealPercent : 0.2f;

    private float moveSpeed = 5f;
    private float knockbackForce = 12f;
    private float knockbackDuration = 0.2f;
    private const float knockbackStunDuration = 0.2f;
    private float knockbackCooldown = 0.5f;
    private float characterKnockbackMultiplier = 1.5f;

    private float currentHp;
    private float currentMaxHp;
    private float lastFrameX;
    private int   currentLevel;
    private float levelTimer;
    private float[] levelReserveTime;
    private bool  isKnockedBack;
    private bool  knockbackHasStun;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;
    private float lastKnockbackTime;
    private bool  isDead;
    private Vector3 lastPosition;
    private bool  isMoving;
    private MapManager cachedMap;
    private bool  isMagnetActive;
    private float magnetTimer;
    private int   magnetStackCount;
    private float baseMaxHp;
    private float overhealScaleBonus;
    private float lastLifestealTime;
    private bool  isShieldActive;
    private float shieldTimer;
    private int   shieldStackCount;
    private bool  isFrozen;
    private float frozenTimer;
    private bool  isSlowed;
    private float slowTimer;
    private float slowMultiplier = 1f;
    private int   killPoints;
    private int   score;
    private int   swordQueue;
    private readonly int[] skillStacks = new int[11];
    private bool  isTargetLocked;
    private CharacterBase lockedTarget;
    private float castTimer;
    private bool  isInvulnerable;

    private readonly List<Sword> magnetSwordBuffer = new();
    private readonly List<CharacterBase> freezeCharBuffer = new();
    private static readonly Vector3[] altDirBuffer = new Vector3[6];

    public float CurrentHp        => currentHp;
    public float MaxHp             => currentMaxHp;
    public float MoveSpeed         => isSlowed ? moveSpeed * slowMultiplier : moveSpeed;
    public int   CharacterNumericId => characterNumericId;
    public string CharacterId      => characterId;
    public string CharacterName    => characterName;
    public Sprite Avatar           => avatar;
    public int   SwordCount        => swordOrbit?.SwordCount ?? 0;
    public int   MaxSwordCount     => maxSwordCount;
    public bool  IsSwordFull       => SwordCount >= maxSwordCount;
    public string CurrentStateName => stateMachine?.CurrentState.GetType().Name ?? "None";
    public int   CurrentLevel      => currentLevel;
    public bool  IsKnockedBack     => isKnockedBack;
    public bool  IsDead            => isDead;
    public bool  IsMagnetActive    => isMagnetActive;
    public float MagnetTimeRemaining => isMagnetActive ? Mathf.Max(0f, magnetTimer) : 0f;
    public int   MagnetStackCount  => magnetStackCount;
    public float MagnetDuration    => magnetDuration;
    public bool  IsShieldActive    => isShieldActive;
    public float ShieldTimeRemaining => isShieldActive ? Mathf.Max(0f, shieldTimer) : 0f;
    public int   ShieldStackCount  => shieldStackCount;
    public float ShieldDuration    => shieldDuration;
    public bool  IsFrozen          => isFrozen;
    public float FrozenTimeRemaining => isFrozen ? Mathf.Max(0f, frozenTimer) : 0f;
    public int   KillPoints        => killPoints;
    public int   Score             => score;
    public int   SwordQueue        => swordQueue;
    public int   Skill1StackCount  => skillStacks[0];
    public int   Skill2StackCount  => skillStacks[1];
    public int   Skill3StackCount  => skillStacks[2];
    public int   Skill4StackCount  => skillStacks[3];
    public int   Skill5StackCount  => skillStacks[4];
    public int   Skill6StackCount  => skillStacks[5];
    public int   Skill7StackCount  => skillStacks[6];
    public int   Skill8StackCount  => skillStacks[7];
    public int   Skill9StackCount  => skillStacks[8];
    public int   Skill10StackCount => skillStacks[9];
    public int   Skill11StackCount => skillStacks[10];
    public int   MaxSwordQueue     => maxSwordQueue;
    public bool  IsTargetLocked    => isTargetLocked;
    public CharacterBase LockedTarget => lockedTarget;
    public bool  IsCasting         => castTimer > 0f;
    public void StartCast(float duration)
    {
        if (duration <= 0f) return;
        castTimer = duration;
        if (animator != null) animator.speed = 0f;
        audioSource?.StopFootstep();
    }
    public SwordOrbit GetSwordOrbit()           => swordOrbit;
    public void TriggerDisintegration(float delay = 0f) => disintegration?.Disintegrate(delay);
    public bool UseSkill(CharacterBase target, int skillIndex = 0) =>
        skillController != null && skillController.TryFireSkill(target, skillIndex);

    public void AddSkill1Stack(int count = 1) { if (!isDead && count > 0) skillStacks[0] += count * 3; }
    public void AddSkill2Stack(int count = 1) { if (!isDead && count > 0) skillStacks[1] += count * 3; }
    public void AddSkill3Stack(int count = 1) { if (!isDead && count > 0) skillStacks[2] += count * 3; }
    public void AddSkill4Stack(int count = 1) { if (!isDead && count > 0) skillStacks[3] += count * 3; }
    public void AddSkill5Stack(int count = 1) { if (!isDead && count > 0) skillStacks[4] += count; }
    public void AddSkill6Stack(int count = 1) { if (!isDead && count > 0) skillStacks[5] += count; }
    public void AddSkill7Stack(int count = 1) { if (!isDead && count > 0) skillStacks[6] += count; }
    public void AddSkill8Stack(int count = 1) { if (!isDead && count > 0) skillStacks[7] += count; }
    public void AddSkill9Stack(int count = 1)  { if (!isDead && count > 0) skillStacks[8]  += count; }
    public void AddSkill10Stack(int count = 1) { if (!isDead && count > 0) skillStacks[9]  += count; }
    public void AddSkill11Stack(int count = 1) { if (!isDead && count > 0) skillStacks[10] += count; }

    private void UpdateSkillStacks()
    {
        if (skillController == null) return;
        for (int i = 0; i < skillStacks.Length; i++)
            if (skillStacks[i] > 0 && skillController.IsSkillReady(i) && skillController.UseSkill(i))
                skillStacks[i]--;
    }

    public void SetInvulnerable(bool value) => isInvulnerable = value;

    public CharacterStateMachine GetStateMachine() => stateMachine;
    public Animator GetAnimator()               => animator;
    public CharacterAudioSource GetAudioSource() => audioSource;

    public float LevelTimeRemaining
    {
        get
        {
            if (levelData == null || currentLevel == 1 || levelReserveTime == null) return 0f;
            return Mathf.Max(0f, levelReserveTime[currentLevel] - levelTimer);
        }
    }

    public void OnInit() => OnInit(characterId, characterName, avatar, characterLevel);

    public void OnInit(string id, string name, Sprite avatarSprite, int level = 1)
    {
        if (config == null) config = CharacterBaseConfigSO.Instance;

        TF.rotation = Quaternion.Euler(90f, 0f, 0f);

        characterId   = id;
        characterName = name;
        avatar        = avatarSprite;
        characterLevel = level;
        currentHp     = maxHp;
        currentMaxHp  = maxHp;
        baseMaxHp     = maxHp;
        overhealScaleBonus  = 0f;
        lastLifestealTime   = -lifestealCooldown;
        currentLevel  = 1;
        levelTimer    = 0f;
        lastFrameX    = TF.position.x;
        lastPosition  = TF.position;
        isMoving      = false;
        lastKnockbackTime = -knockbackCooldown;
        isDead        = false;
        isMagnetActive = false;
        magnetTimer   = 0f;
        magnetStackCount = 0;
        isShieldActive = false;
        shieldTimer   = 0f;
        shieldStackCount = 0;
        isFrozen      = false;
        frozenTimer   = 0f;
        isSlowed      = false;
        slowTimer     = 0f;
        slowMultiplier = 1f;
        killPoints       = 0;
        score            = 0;
        swordQueue       = 0;
        System.Array.Clear(skillStacks, 0, skillStacks.Length);
        isTargetLocked = false;
        lockedTarget  = null;
        castTimer     = 0f;
        isInvulnerable = false;
        StopAllLevelParticles();

        levelReserveTime = new float[levelData.GetMaxLevel() + 1];
        if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
        if (animator     == null) animator     = GetComponentInChildren<Animator>();
        if (audioSource  == null) audioSource  = GetComponent<CharacterAudioSource>();
        if (cachedMap    == null) cachedMap    = MapManager.Instance;

        swordOrbit.OnInit();
        stateMachine.OnInit();

        infoUI.Init(characterName, avatar, currentHp, currentMaxHp);
        infoUI.SetCharacter(this);

        animator.Rebind();
        animator.Update(0f);
        animator.SetTrigger("walk");

        if (bodyCollider) bodyCollider.enabled = true;
        infoUI?.gameObject.SetActive(true);

        UpdateLevelStats();
        CharacterManager.Instance.Register(this);

        StopBoosterParticles();
        ActivateShieldBooster();
    }

    public void OnDespawn()
    {
        audioSource.StopFootstep();

        swordOrbit.OnDespawn();
        stateMachine.OnDespawn();
        CharacterManager.Instance.Despawn(this);
    }

    public void ManagedUpdate(float deltaTime)
    {
        if (isDead)
        {
            audioSource.StopFootstep();
            stateMachine.ManagedUpdate(deltaTime);
            return;
        }

        if (castTimer > 0f)
        {
            castTimer -= deltaTime;
            if (castTimer <= 0f)
            {
                if (animator != null) animator.speed = 1f;
                animator?.SetTrigger("walk");
            }
        }

        if (isFrozen)
        {
            frozenTimer -= deltaTime;
            if (frozenTimer <= 0f) Unfreeze();
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
            else if (!knockbackHasStun || knockbackTimer > knockbackStunDuration)
            {
                Vector3 vel = knockbackVelocity;
                vel.y = 0f;
                Vector3 newPos = TF.position + vel * deltaTime;
                newPos.y = 0f;
                if (cachedMap != null)
                {
                    newPos = cachedMap.ClampToMap(newPos);
                    newPos.y = 0f;
                    if (cachedMap.IsBlockedWorld(newPos))
                    {
                        Vector3 altPos = TryAlternativeKnockbackDirection(TF.position, vel, deltaTime);
                        if (altPos != TF.position)
                        {
                            altPos.y = 0f;
                            TF.position = altPos;
                        }
                        else
                        {
                            knockbackVelocity = Vector3.zero;
                            knockbackTimer = knockbackHasStun ? knockbackStunDuration : 0f;
                        }
                    }
                    else
                    {
                        TF.position = newPos;
                    }
                }
                else
                {
                    TF.position = newPos;
                }
            }
        }

        UpdateLevelTimer(deltaTime);
        UpdateMagnetBooster(deltaTime);
        UpdateShieldBooster(deltaTime);
        UpdateSlow(deltaTime);
        ProcessSwordQueue();
        UpdateSkillStacks();

        stateMachine?.ManagedUpdate(deltaTime);

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
            if (nextLevel != currentLevel) SetLevel(nextLevel);
        }
    }

    private int GetHighestAvailableLevel()
    {
        if (levelReserveTime == null) return 1;
        for (int i = levelReserveTime.Length - 1; i >= 1; i--)
            if (levelReserveTime[i] > 0f) return i;
        return 1;
    }

    public void AddLevelReserveTime(int level, int count = 1)
    {
        if (levelData == null || levelReserveTime == null) return;
        int maxLevel = levelData.GetMaxLevel();
        if (level < 1 || level > maxLevel) return;
        if (count < 1) count = 1;

        levelReserveTime[level] += levelData.GetDuration(level) * count;

        int highestLevel = GetHighestAvailableLevel();
        if (highestLevel > currentLevel) SetLevel(highestLevel);
    }

    public float GetLevelReserveTime(int level)
    {
        if (levelReserveTime == null || level < 0 || level >= levelReserveTime.Length) return 0f;
        return levelReserveTime[level];
    }

    private void StopBoosterParticles()
    {
        if (magnetParticle != null) magnetParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (shieldParticle != null) shieldParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (healParticle   != null) healParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void StopAllLevelParticles()
    {
        if (levelParticles == null) return;
        for (int i = 0; i < levelParticles.Length; i++)
        {
            LevelParticleGroup group = levelParticles[i];
            if (group?.particles == null) continue;
            for (int j = 0; j < group.particles.Length; j++)
            {
                if (group.particles[j] != null)
                    group.particles[j].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void PlayLevelParticle(int level)
    {
        StopAllLevelParticles();
        int idx = level - 1;
        if (levelParticles == null || idx < 0 || idx >= levelParticles.Length) return;
        LevelParticleGroup group = levelParticles[idx];
        if (group?.particles == null) return;
        for (int j = 0; j < group.particles.Length; j++)
        {
            if (group.particles[j] != null)
                group.particles[j].Play();
        }
    }

    private void UpdateLevelStats()
    {
        if (levelData == null) return;

        swordOrbit.SetSwordType(levelData.GetSwordType(currentLevel));
        moveSpeed = levelData.GetSpeed(currentLevel);
        if (moveSpeed <= 1f) moveSpeed = 2f;
        float totalScale = levelData.GetBodyScale(currentLevel) + overhealScaleBonus;
        TF.localScale = Vector3.one * totalScale;

        if (visualTransform != null && visualTransform != TF)
        {
            Vector3 s = visualTransform.localScale;
            visualTransform.localScale = new Vector3(s.x >= 0 ? 1f : -1f, 1f, 1f);
        }
    }

    private void UpdateFootstepSound()
    {
        if (audioSource == null) return;
        Vector3 currentPos = TF.position;
        float dx = currentPos.x - lastPosition.x;
        float dz = currentPos.z - lastPosition.z;
        bool wasMoving = isMoving;
        isMoving = dx * dx + dz * dz > 0.0001f;

        if (isMoving != wasMoving)
        {
            if (isMoving) audioSource.PlayFootstep();
            else          audioSource.StopFootstep();
        }

        lastPosition = currentPos;
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

    private void UpdateMagnetBooster(float deltaTime)
    {
        if (!isMagnetActive) return;

        magnetTimer -= deltaTime;
        if (magnetTimer <= 0f)
        {
            if (magnetStackCount > 0)
            {
                magnetStackCount--;
                magnetTimer = magnetDuration;
            }
            else
            {
                isMagnetActive = false;
            }
            return;
        }

        if (IsSwordFull || swordQueue > 0) return;

        ItemManager itemMgr = ItemManager.Instance;
        if (itemMgr == null) return;

        magnetSwordBuffer.Clear();
        itemMgr.GetNearbySwords(TF.position, magnetRadius, magnetSwordBuffer);

        for (int i = 0; i < magnetSwordBuffer.Count; i++)
        {
            Sword sword = magnetSwordBuffer[i];
            if (sword.State != SwordState.Dropped) continue;

            Vector3 toCharacter = TF.position - sword.TF.position;
            float distanceSq = toCharacter.x * toCharacter.x + toCharacter.z * toCharacter.z;

            if (distanceSq < 1f)
            {
                sword.Collect(this);
                if (IsSwordFull) break;
                continue;
            }

            float distance = Mathf.Sqrt(distanceSq);
            Vector3 direction = toCharacter / distance;
            float moveAmount = Mathf.Min(magnetPullSpeed * deltaTime, distance);
            Vector3 newPos = sword.TF.position + direction * moveAmount;
            if (cachedMap != null) newPos = cachedMap.ClampToMap(newPos);
            sword.TF.position = newPos;
        }
    }

    public void ActivateMagnetBooster(int count = 1)
    {
        if (count < 1) count = 1;
        if (isMagnetActive)
        {
            magnetStackCount += count;
        }
        else
        {
            isMagnetActive = true;
            magnetTimer = magnetDuration;
            magnetStackCount = count - 1;
        }
        if (magnetParticle != null) magnetParticle.Play();
    }

    private void UpdateShieldBooster(float deltaTime)
    {
        if (!isShieldActive) return;

        shieldTimer -= deltaTime;
        if (shieldTimer <= 0f)
        {
            if (shieldStackCount > 0)
            {
                shieldStackCount--;
                shieldTimer = shieldDuration;
            }
            else
            {
                isShieldActive = false;
                shieldParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    public void ActivateShieldBooster(int count = 1)
    {
        if (count < 1) count = 1;
        if (isShieldActive)
        {
            shieldStackCount += count;
        }
        else
        {
            isShieldActive = true;
            shieldTimer = shieldDuration;
            shieldStackCount = count - 1;
        }
        if (shieldParticle != null) shieldParticle.Play();
    }

    public void Freeze(float duration)
    {
        if (isFrozen) return;
        isFrozen = true;
        frozenTimer = duration;
        if (animator    != null) animator.speed = 0f;
        if (swordOrbit  != null) swordOrbit.SetPaused(true);
        if (audioSource != null) audioSource.StopFootstep();
    }

    public void Unfreeze()
    {
        isFrozen = false;
        if (animator   != null) animator.speed = 1f;
        if (swordOrbit != null) swordOrbit.SetPaused(false);
    }

    public void ActivateFreezeBooster()
    {
        CharacterManager charMgr = CharacterManager.Instance;
        if (charMgr == null) return;

        freezeCharBuffer.Clear();
        charMgr.GetNearbyCharacters(TF.position, 10f, freezeCharBuffer);

        for (int i = 0; i < freezeCharBuffer.Count; i++)
        {
            CharacterBase c = freezeCharBuffer[i];
            if (c == this || c.IsDead) continue;
            c.Freeze(freezeDuration);
        }
    }

    public void ActivateHealBooster(float healAmount)
    {
        if (isDead) return;
        currentHp += healAmount;
        if (currentHp > currentMaxHp)
        {
            float overflow = currentHp - currentMaxHp;
            currentMaxHp += overflow;
            UpdateOverhealScale();
        }
        infoUI?.UpdateHp(currentHp, currentMaxHp);
        audioSource?.PlayLevelUp();
        if (healParticle != null) healParticle.Play();
    }

    private void UpdateOverhealScale()
    {
        float totalOverheal = currentMaxHp - baseMaxHp;
        if (totalOverheal <= 0f)
        {
            overhealScaleBonus = 0f;
            UpdateLevelStats();
            return;
        }

        float newScaleBonus = Mathf.FloorToInt(totalOverheal / overhealThreshold) * overhealScalePerThreshold;
        if (newScaleBonus != overhealScaleBonus)
        {
            overhealScaleBonus = newScaleBonus;
            UpdateLevelStats();
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
        PlayLevelParticle(currentLevel);
    }

    public void SetLevel(int level)
    {
        if (levelData == null) return;
        int oldLevel = currentLevel;
        currentLevel = Mathf.Clamp(level, 1, levelData.GetMaxLevel());
        levelTimer = 0f;
        UpdateLevelStats();
        if (currentLevel > oldLevel) audioSource?.PlayLevelUp();
        PlayLevelParticle(currentLevel);
    }

    public int GetMaxLevel() => levelData?.GetMaxLevel() ?? 1;
    public CharacterLevelDataSO GetLevelData() => levelData;

    public float GetLevelDuration()
    {
        if (levelData == null || levelReserveTime == null || currentLevel >= levelReserveTime.Length) return 0f;
        return levelReserveTime[currentLevel];
    }

    public void TakeDamage(float damage, CharacterBase attacker = null)
    {
        if (isDead || isShieldActive || isInvulnerable) return;

        if (levelData != null)
            damage *= 1f - levelData.GetDamageReduction(currentLevel);

        currentHp = Mathf.Max(0f, currentHp - damage);
        infoUI?.UpdateHp(currentHp, currentMaxHp);

        if (currentHp <= 0f)
        {
            OnKilledBy(attacker);
            OnDeath();
        }
    }

    public void TakeSkillDamage(float damage, CharacterBase caster = null, SkillData skillData = null)
    {
        if (isDead) return;

        if (isShieldActive)
        {
            if (shieldStackCount > 0)
            {
                shieldStackCount--;
                shieldTimer = shieldDuration;
            }
            else
            {
                isShieldActive = false;
                shieldParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (levelData != null)
            damage *= 1f - levelData.GetDamageReduction(currentLevel);

        currentHp = Mathf.Max(0f, currentHp - damage);
        infoUI?.UpdateHp(currentHp, currentMaxHp);

        if (currentHp > 0f && skillData != null)
        {
            switch (skillData.hitEffect)
            {
                case SkillHitEffect.Stun:
                    Stun(skillData.stunDuration);
                    break;
                case SkillHitEffect.KnockbackStun:
                    Vector3 dir = caster != null ? TF.position - caster.TF.position : Vector3.forward;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
                    ApplySkillKnockback(dir.normalized, skillData.skillKnockbackForce, skillData.skillKnockbackDuration, skillData.stunDuration);
                    break;
            }

            if (skillData.enableSlow)
                ApplySlow(skillData.slowFactor, skillData.slowDuration);
        }

        if (currentHp <= 0f)
        {
            OnKilledBy(caster);
            OnDeath();
        }
    }

    public void Stun(float duration)
    {
        if (isDead || duration <= 0f || isInvulnerable) return;
        isFrozen = true;
        frozenTimer = Mathf.Max(frozenTimer, duration);
        if (animator) animator.speed = 0f;
        swordOrbit?.SetPaused(true);
        audioSource?.StopFootstep();
    }

    public void ApplySlow(float factor, float duration)
    {
        if (isDead || duration <= 0f || factor >= 1f) return;
        isSlowed       = true;
        slowMultiplier = Mathf.Min(slowMultiplier, Mathf.Clamp01(factor));
        slowTimer      = Mathf.Max(slowTimer, duration);
    }

    private void UpdateSlow(float deltaTime)
    {
        if (!isSlowed) return;
        slowTimer -= deltaTime;
        if (slowTimer <= 0f)
        {
            isSlowed       = false;
            slowMultiplier = 1f;
        }
    }

    private void ApplySkillKnockback(Vector3 direction, float force, float duration, float stunDuration)
    {
        if (isInvulnerable) return;
        float currentTime = Time.time;
        if (isKnockedBack || currentTime - lastKnockbackTime < knockbackCooldown) return;

        float flyDuration = duration > 0f ? duration : knockbackDuration;
        isKnockedBack     = true;
        knockbackHasStun  = stunDuration > 0f;
        knockbackVelocity = direction * (force > 0f ? force : knockbackForce);
        knockbackTimer    = flyDuration + (knockbackHasStun ? stunDuration : 0f);
        lastKnockbackTime = currentTime;
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHp = Mathf.Min(currentMaxHp, currentHp + amount);
        infoUI.UpdateHp(currentHp, currentMaxHp);
    }

    public void OnLifesteal(float damageDealt)
    {
        if (isDead) return;
        float currentTime = Time.time;
        if (currentTime - lastLifestealTime < lifestealCooldown) return;

        float healAmount = damageDealt * lifestealPercentConfig;
        if (healAmount <= 0f) return;

        float oldHp = currentHp;
        currentHp = Mathf.Min(currentMaxHp, currentHp + healAmount);
        if (currentHp > oldHp)
        {
            infoUI.UpdateHp(currentHp, currentMaxHp);
            lastLifestealTime = currentTime;
        }
    }

    public void MultiplySpeed(float multiplier) => moveSpeed *= multiplier;

    public void OnSwordInteraction(CharacterBase attacker)
    {
        if (isDead || attacker == null || isInvulnerable) return;
        float currentTime = Time.time;
        if (isKnockedBack || currentTime - lastKnockbackTime < knockbackCooldown) return;

        ApplyKnockback((TF.position - attacker.TF.position).normalized, characterKnockbackMultiplier);
        lastKnockbackTime = currentTime;
        stateMachine?.OnUnderAttack(attacker);
        ApplyStunIfFlee();
    }

    public void OnSwordToSwordKnockback(CharacterBase attacker)
    {
        if (isDead || attacker == null || isInvulnerable) return;
        float currentTime = Time.time;
        if (isKnockedBack || currentTime - lastKnockbackTime < knockbackCooldown) return;

        ApplyKnockback((TF.position - attacker.TF.position).normalized, 1f);
        lastKnockbackTime = currentTime;
        stateMachine?.OnUnderAttack(attacker);
        ApplyStunIfFlee();
    }

    private void ApplyStunIfFlee()
    {
        if (!knockbackHasStun && stateMachine?.CurrentState is FleeState)
        {
            knockbackHasStun = true;
            knockbackTimer += knockbackStunDuration;
        }
    }

    private void ApplyKnockback(Vector3 direction, float multiplier)
    {
        isKnockedBack = true;
        knockbackHasStun = stateMachine?.CurrentState is FleeState;
        knockbackVelocity = new Vector3(direction.x, 0f, direction.z).normalized * knockbackForce * multiplier;
        knockbackTimer = knockbackDuration + (knockbackHasStun ? knockbackStunDuration : 0f);
    }

    private Vector3 TryAlternativeKnockbackDirection(Vector3 currentPos, Vector3 velocity, float deltaTime)
    {
        if (cachedMap == null) return currentPos;

        Vector3 perpLeft  = new Vector3(-velocity.z, 0f,  velocity.x);
        Vector3 perpRight = new Vector3( velocity.z, 0f, -velocity.x);
        float mag = velocity.magnitude;

        altDirBuffer[0] = perpLeft;
        altDirBuffer[1] = perpRight;
        altDirBuffer[2] = (velocity + perpLeft).normalized;
        altDirBuffer[3] = (velocity + perpRight).normalized;
        altDirBuffer[4] = perpLeft  * 0.5f;
        altDirBuffer[5] = perpRight * 0.5f;

        for (int i = 0; i < 6; i++)
        {
            Vector3 altVelocity = altDirBuffer[i].normalized * mag;
            Vector3 testPos = cachedMap.ClampToMap(currentPos + altVelocity * deltaTime);
            if (!cachedMap.IsBlockedWorld(testPos))
            {
                knockbackVelocity = altVelocity;
                return testPos;
            }
        }

        return currentPos;
    }

    private void OnDeath()
    {
        isDead = true;

        isFrozen          = false;
        frozenTimer       = 0f;
        isKnockedBack     = false;
        knockbackVelocity = Vector3.zero;
        knockbackTimer    = 0f;
        if (animator) animator.speed = 1f;
        swordOrbit?.SetPaused(false);

        CharacterManager.Instance?.ReleaseCharacterIdentity(this);
        characterId      = string.Empty;
        characterNumericId = 0;
        if (bodyCollider) bodyCollider.enabled = false;
        infoUI?.gameObject.SetActive(false);

        audioSource?.PlayDeath();

        if (swordOrbit != null)
        {
            int count = swordOrbit.SwordCount;
            for (int i = count - 1; i >= 0; i--)
                swordOrbit.DropSword(i);
        }

        skillController?.CancelAllEffects();
        StopBoosterParticles();
        StopAllLevelParticles();

        animator?.SetTrigger("die");

        if (stateMachine != null) stateMachine.ChangeState(stateMachine.Dead);
        else                      OnDespawn();
    }

    public void AddScore(int points) { if (!isDead && points > 0) score += points; }

    public void OnKilledBy(CharacterBase killer)
    {
        if (killer == null || killer.IsDead) return;
        killer.killPoints += 1;
        int killScore = 10 * killer.killPoints;
        killer.score += killScore;
        CharacterManager.Instance?.AddKillScore(killScore);
        if (EventNotificationManager.Instance != null)
            EventNotificationManager.Instance.ShowKillNotification(killer.CharacterName, characterName);
    }

    public bool AddToSwordQueue(int count)
    {
        if (count <= 0) return false;
        int spaceLeft = maxSwordQueue - swordQueue;
        if (spaceLeft <= 0) return false;
        swordQueue += Mathf.Min(count, spaceLeft);
        return true;
    }

    private void ProcessSwordQueue()
    {
        if (swordQueue <= 0 || IsSwordFull) return;
        int swordsNeeded = maxSwordCount - SwordCount;
        if (swordsNeeded <= 0) return;

        int swordsToAdd = Mathf.Min(swordQueue, swordsNeeded);
        for (int i = 0; i < swordsToAdd; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle.normalized * 2f;
            Vector3 spawnPos = TF.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
            Sword sword = ItemManager.Instance.Spawn(spawnPos);
            if (sword != null && sword.CollectFromQueue(this))
                swordQueue--;
            else
                break;
        }
    }

    public void LockTarget(CharacterBase target)
    {
        if (target == null || target == this) return;

        isTargetLocked = true;
        lockedTarget = target;

        if (stateMachine != null)
        {
            stateMachine.Attack.SetTarget(target);
            stateMachine.ChangeState(stateMachine.Attack);
        }
    }

    public void RestoreKillPoints(int points) => killPoints = points;
    public void RestoreScore(int points)      => score      = points;

    public void SetCharacterNumericId(int numericId)
    {
        characterNumericId = numericId;
        if (infoUI != null) infoUI.SetCharacterNumericId(characterNumericId);
    }

    public void UnlockTarget()
    {
        isTargetLocked = false;
        lockedTarget = null;
    }
}
