using System;
using System.Collections.Generic;
using UnityEngine;
using MagicFX5;

public class SkillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterBase owner;

    [Header("Spawn Points")]
    [SerializeField] private Transform rightHandPosition;
    [SerializeField] private Transform characterEffectPosition;

    [Header("Skills")]
    [SerializeField] private SkillData[] skills;

    private float[] cooldownTimers;
    private int currentSkillIndex = -1;
    private CharacterBase currentTarget;

    private readonly List<GameObject> activeEffects = new();



    private Transform SpawnTF     => rightHandPosition       ?? transform;
    private Transform BuffSpawnTF => characterEffectPosition ?? transform;

    public bool IsSkillReady(int index)
    {
        if (cooldownTimers == null || (uint)index >= (uint)cooldownTimers.Length) return false;
        return cooldownTimers[index] <= 0f;
    }

    public float GetCooldownRemaining(int index)
    {
        if (cooldownTimers == null || (uint)index >= (uint)cooldownTimers.Length) return 0f;
        return Mathf.Max(0f, cooldownTimers[index]);
    }

    private void Awake()
    {
        if (owner == null) owner = GetComponent<CharacterBase>();
        cooldownTimers = skills != null ? new float[skills.Length] : Array.Empty<float>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < cooldownTimers.Length; i++)
            if (cooldownTimers[i] > 0f) cooldownTimers[i] -= dt;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
            if (activeEffects[i] == null) activeEffects.RemoveAt(i);
    }

    public void CancelAllEffects()
    {
        for (int i = 0; i < activeEffects.Count; i++)
            if (activeEffects[i] != null) Destroy(activeEffects[i]);
        activeEffects.Clear();
    }

    private void DestroyEffect(GameObject go, float lifetime)
    {
        if (go == null) return;
        activeEffects.Add(go);
        Destroy(go, lifetime);
    }

    private void SetupSkill(int skillIndex, CharacterBase target)
    {
        currentSkillIndex = skillIndex;
        currentTarget = target;
        cooldownTimers[skillIndex] = skills[skillIndex].cooldown;
        owner?.StartCast(skills[skillIndex].castDuration);
        StartCoroutine(ShakeAfterDelay(skills[skillIndex]));
    }

    public bool TryFireSkill(CharacterBase target, int skillIndex = 0)
    {
        if (!CanFire(skillIndex, target)) return false;
        SetupSkill(skillIndex, target);
        ApplySlowToTarget(skills[skillIndex], target);
        TriggerHandEffect();
        TriggerMainEffect();
        TriggerBuffEffect();
        return true;
    }

    public bool TryFireSkillNoTarget(int skillIndex = 0)
    {
        if (!CanFireSkillOnly(skillIndex)) return false;
        SetupSkill(skillIndex, null);
        TriggerHandEffect();
        TriggerBuffEffect();
        SkillData skill = skills[skillIndex];
        if (skill.mainEffect != null)
        {
            GameObject inst = Instantiate(skill.mainEffect, SpawnTF.position, SpawnTF.rotation);

            DestroyEffect(inst, skill.effectLifeTime);
        }
        return true;
    }

    public bool TryFireSkillMultiTarget(List<CharacterBase> targets, int skillIndex = 0)
    {
        if (targets == null || targets.Count == 0) return false;
        if (!CanFireSkillOnly(skillIndex)) return false;
        SetupSkill(skillIndex, targets[0]);
        ApplySlowToTargets(skills[skillIndex], targets);
        TriggerHandEffect();
        TriggerBuffEffect();
        SpawnMainEffectMultiTarget(targets, skills[skillIndex]);
        return true;
    }

    private void SpawnMainEffectMultiTarget(List<CharacterBase> targets, SkillData skill)
    {
        if (skill.mainEffect == null) return;
        Vector3 dir = targets[0].transform.position - SpawnTF.position; dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : SpawnTF.rotation;
        GameObject inst = Instantiate(skill.mainEffect, SpawnTF.position, rot);
        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { DestroyEffect(inst, skill.effectLifeTime); return; }
        settings.Targets = ToTransformArray(targets);
        SetupBurstHit(settings, targets, skill);
        DestroyEffect(inst, skill.effectLifeTime);
    }

    public bool TryFireSkillAtTargetWithAllTargets(CharacterBase primaryTarget, List<CharacterBase> targets, int skillIndex)
    {
        if (targets == null || targets.Count == 0) return false;
        if (!CanFire(skillIndex, primaryTarget)) return false;
        SetupSkill(skillIndex, primaryTarget);
        ApplySlowToTargets(skills[skillIndex], targets);
        TriggerHandEffect();
        TriggerBuffEffect();
        SpawnMainEffectAtTargetWithAllTargets(primaryTarget, targets, skills[skillIndex]);
        return true;
    }

    private void SpawnMainEffectAtTargetWithAllTargets(CharacterBase primaryTarget, List<CharacterBase> targets, SkillData skill)
    {
        if (skill.mainEffect == null) return;

        GameObject inst = Instantiate(skill.mainEffect, primaryTarget.transform.position, Quaternion.identity);


        MagicFX5_FollowTarget followTarget = inst.GetComponentInChildren<MagicFX5_FollowTarget>();

        if (followTarget) followTarget.Target = primaryTarget.transform;

        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();

        if (settings == null) { DestroyEffect(inst, skill.effectLifeTime); return; }

        settings.Targets = ToTransformArray(targets);
        SetupBurstHit(settings, targets, skill);
        DestroyEffect(inst, skill.effectLifeTime);
    }

    public bool TryFireSkillSelfAoE(int skillIndex)
    {
        if (!CanFireSkillOnly(skillIndex) || owner == null) return false;
        SkillData skill = skills[skillIndex];
        SetupSkill(skillIndex, null);
        List<CharacterBase> targets = CharacterManager.Instance?.GetEnemiesInRadius(owner.TF.position, skill.damageRadius, owner) ?? new List<CharacterBase>();
        ApplySlowToTargets(skill, targets);
        TriggerHandEffect();
        TriggerBuffEffect();
        if (skill.mainEffect == null) return true;
        GameObject inst = Instantiate(skill.mainEffect, owner.TF.position, Quaternion.identity);
        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { DestroyEffect(inst, skill.effectLifeTime); return true; }
        settings.Targets = ToTransformArray(targets);
        SetupBurstHit(settings, targets, skill);
        DestroyEffect(inst, skill.effectLifeTime);
        return true;
    }

    public bool TryFireSkillAoEAtTarget(CharacterBase target, int skillIndex = 2)
    {
        if (!CanFire(skillIndex, target)) return false;
        SetupSkill(skillIndex, target);
        ApplySlowToTarget(skills[skillIndex], target);
        TriggerHandEffect();
        TriggerMainEffectAoEAtTarget(target, skills[skillIndex]);
        TriggerBuffEffect();
        return true;
    }

    private void TriggerMainEffectAoEAtTarget(CharacterBase target, SkillData skill)
    {
        if (skill.mainEffect == null) return;
        Vector3 dir = target.transform.position - SpawnTF.position; dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : SpawnTF.rotation;
        GameObject inst = Instantiate(skill.mainEffect, target.transform.position, rot);
        MagicFX5_FollowTarget followTarget = inst.GetComponentInChildren<MagicFX5_FollowTarget>();
        if (followTarget) followTarget.Target = target.transform;
        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { DestroyEffect(inst, skill.effectLifeTime); return; }
        settings.Targets = new[] { target.transform };
        float dmg = skill.damage;
        float radius = skill.damageRadius;
        int maxCount = skill.maxTargets;
        CharacterBase caster = owner;
        CharacterBase hitTarget = target;
        bool hit = false;
        settings.OnEffectCollisionEnter += col =>
        {
            if (hit) return;
            hit = true;
            Vector3 aoeCenter = hitTarget != null && !hitTarget.IsDead ? hitTarget.TF.position : col.Position;
            if (radius > 0f)
            {
                List<CharacterBase> nearbyAoE = CharacterManager.Instance?.GetEnemiesInRadius(aoeCenter, radius, caster) ?? new List<CharacterBase>();
                for (int i = 0; i < nearbyAoE.Count && i < maxCount; i++)
                {
                    nearbyAoE[i].TakeSkillDamage(dmg, caster, skill);
                }
            }
            else
            {
                if (hitTarget == null || hitTarget.IsDead) return;
                hitTarget.TakeSkillDamage(dmg, caster, skill);
            }
        };
        DestroyEffect(inst, skill.effectLifeTime);
    }

    public bool TryFireSkillGlobalAoEAtPosition(int skillIndex, Vector3 spawnPosition, List<CharacterBase> targets)
    {
        if (!CanFireSkillOnly(skillIndex) || owner == null) return false;
        SkillData skill = skills[skillIndex];
        SetupSkill(skillIndex, null);
        ApplySlowToTargets(skill, targets);
        TriggerHandEffect();
        TriggerBuffEffect();
        if (skill.mainEffect == null) return true;
        GameObject inst = Instantiate(skill.mainEffect, spawnPosition, Quaternion.identity);
        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { DestroyEffect(inst, skill.effectLifeTime); return true; }
        settings.Targets = ToTransformArray(targets);
        SetupBurstHit(settings, targets, skill);
        DestroyEffect(inst, skill.effectLifeTime);
        return true;
    }

    private void TriggerMainEffect()
    {
        if (currentSkillIndex < 0 || currentTarget == null || currentTarget.IsDead) return;
        SkillData skill = skills[currentSkillIndex];
        if (skill.mainEffect == null) return;
        Vector3 dir = currentTarget.transform.position - SpawnTF.position; dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : SpawnTF.rotation;
        GameObject inst = Instantiate(skill.mainEffect, SpawnTF.position, rot);
        ApplyEffectSettings(inst, skill);
    }

    private void TriggerHandEffect()
    {
        if (currentSkillIndex < 0) return;
        SkillData skill = skills[currentSkillIndex];
        if (skill.handEffect == null) return;
        GameObject inst = Instantiate(skill.handEffect, SpawnTF.position, Quaternion.identity, SpawnTF);
        DestroyEffect(inst, skill.effectLifeTime);
    }

    private void TriggerBuffEffect()
    {
        if (currentSkillIndex < 0) return;
        SkillData skill = skills[currentSkillIndex];
        if (skill.characterEffect == null) return;
        GameObject inst = Instantiate(skill.characterEffect, BuffSpawnTF.position, BuffSpawnTF.rotation);
        DestroyEffect(inst, skill.effectLifeTime);
    }

    private void ApplyEffectSettings(GameObject instance, SkillData skill)
    {
        MagicFX5_EffectSettings settings = instance.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) return;
        if (currentTarget != null) settings.Targets = new[] { currentTarget.transform };
        float dmg = skill.damage;
        float radius = skill.damageRadius;
        int maxTargets = skill.maxTargets;
        CharacterBase caster = owner;
        bool hit = false;
        settings.OnEffectCollisionEnter += col =>
        {
            if (hit) return;
            hit = true;
            if (radius > 0f && maxTargets > 1)
            {
                List<CharacterBase> nearby = CharacterManager.Instance?.GetEnemiesInRadius(col.Position, radius, caster) ?? new List<CharacterBase>();
                for (int i = 0; i < nearby.Count && i < maxTargets; i++)
                {
                    nearby[i].TakeSkillDamage(dmg, caster, skill);
                }
            }
            else
            {
                CharacterBase hitChar = col.Target?.GetComponent<CharacterBase>();
                if (hitChar == null || hitChar == caster || hitChar.IsDead) return;
                hitChar.TakeSkillDamage(dmg, caster, skill);
            }
        };
        DestroyEffect(instance, skill.effectLifeTime);
    }

    private void SetupBurstHit(MagicFX5_EffectSettings settings, List<CharacterBase> targets, SkillData skill)
    {
        float dmg = skill.damage;
        CharacterBase caster = owner;
        bool hit = false;
        settings.OnEffectCollisionEnter += _ =>
        {
            if (hit) return;
            hit = true;
            StartCoroutine(ApplyDamageSpread(targets, dmg, caster, skill));
        };
    }

    private System.Collections.IEnumerator ApplyDamageSpread(List<CharacterBase> targets, float dmg, CharacterBase caster, SkillData skill)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CharacterBase t = targets[i];
            if (t == null || t.IsDead) continue;
            t.TakeSkillDamage(dmg, caster, skill);
            if (i % 20 == 19) yield return null;
        }
    }

    private static Transform[] ToTransformArray(List<CharacterBase> list)
    {
        Transform[] arr = new Transform[list.Count];
        for (int i = 0; i < list.Count; i++) arr[i] = list[i].transform;
        return arr;
    }

    private void ApplySlowToTarget(SkillData skill, CharacterBase target)
    {
        if (!skill.enableSlow || target == null || target.IsDead) return;
        target.ApplySlow(skill.slowFactor, skill.slowDuration);
    }

    private void ApplySlowToTargets(SkillData skill, List<CharacterBase> targets)
    {
        if (!skill.enableSlow) return;
        foreach (CharacterBase t in targets)
            if (t != null && !t.IsDead) t.ApplySlow(skill.slowFactor, skill.slowDuration);
    }

    public int GetSkillMaxTargets(int skillIndex)
    {
        if (skills == null || (uint)skillIndex >= (uint)skills.Length) return 1;
        return skills[skillIndex].maxTargets;
    }

    public float GetSkillDamageRadius(int skillIndex)
    {
        if (skills == null || (uint)skillIndex >= (uint)skills.Length) return 0f;
        return skills[skillIndex].damageRadius;
    }

    public Vector3 GetSpawnPosition() => SpawnTF.position;

    private System.Collections.IEnumerator ShakeAfterDelay(SkillData skill)
    {
        if (skill.shakeOnFireDuration <= 0f) yield break;
        if (skill.shakeDelay > 0f) yield return new WaitForSeconds(skill.shakeDelay);
        CameraController.Instance?.Shake(skill.shakeOnFireDuration, skill.shakeOnFireMagnitude);
    }

    private bool CanFire(int skillIndex, CharacterBase target)
        => CanFireSkillOnly(skillIndex) && target != null && !target.IsDead;

    private bool CanFireSkillOnly(int skillIndex)
    {
        if (owner == null || owner.IsDead || owner.IsFrozen) return false;
        if (skills == null || (uint)skillIndex >= (uint)skills.Length) return false;
        return cooldownTimers[skillIndex] <= 0f;
    }

    public bool UseSkill(int slot)
    {
        return slot switch
        {
            0            => UseSkillMultiTarget(0),
            1            => UseSkillAtThreeNearestTargets(1),
            2            => UseSkillAtThreeNearestTargets(2),
            3 or 4 or 5  => TryFireSkillSelfAoE(slot),
            6 or 7 or 8  => UseSkillGlobalAoE(slot),
            _            => false
        };
    }

    private bool UseSkillMultiTarget(int slot)
    {
        if (owner == null || owner.IsDead) return false;
        int maxTargets = GetSkillMaxTargets(slot);
        List<CharacterBase> targets = CharacterManager.Instance?.GetNearestEnemies(owner.TF.position, owner, maxTargets);
        if (targets == null || targets.Count == 0) return false;
        return TryFireSkillMultiTarget(targets, slot);
    }

    private bool UseSkillAtTargetAoE(int slot)
    {
        if (owner == null || owner.IsDead) return false;
        List<CharacterBase> nearestList = CharacterManager.Instance?.GetNearestEnemies(owner.TF.position, owner, 1);
        if (nearestList == null || nearestList.Count == 0) return false;
        CharacterBase nearest = nearestList[0];
        float radius = GetSkillDamageRadius(slot);
        List<CharacterBase> targets = CharacterManager.Instance?.GetEnemiesInRadius(nearest.TF.position, radius, owner) ?? new List<CharacterBase>();
        if (targets.Count == 0) targets.Add(nearest);
        return TryFireSkillAtTargetWithAllTargets(nearest, targets, slot);
    }

    private bool UseSkillAtThreeNearestTargets(int slot)
    {
        if (owner == null || owner.IsDead) return false;
        List<CharacterBase> targets = CharacterManager.Instance?.GetNearestEnemies(owner.TF.position, owner, 3);
        if (targets == null || targets.Count == 0) return false;
        if (!CanFireSkillOnly(slot)) return false;
        SetupSkill(slot, targets[0]);
        TriggerHandEffect();
        TriggerBuffEffect();
        StartCoroutine(SpawnSkillsWithDelay(new List<CharacterBase>(targets), skills[slot], 0.3f));
        return true;
    }

    private System.Collections.IEnumerator SpawnSkillsWithDelay(List<CharacterBase> targets, SkillData skill, float interval)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            SpawnSkillAtTarget(targets[i], skill);
            if (i < targets.Count - 1)
                yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnSkillAtTarget(CharacterBase target, SkillData skill)
    {
        if (skill.mainEffect == null || target == null || target.IsDead) return;
        if (skill.enableSlow)
            target.ApplySlow(skill.slowFactor, skill.slowDuration);
        GameObject inst = Instantiate(skill.mainEffect, target.TF.position, Quaternion.identity);
        MagicFX5_FollowTarget followTarget = inst.GetComponentInChildren<MagicFX5_FollowTarget>();
        if (followTarget != null) followTarget.Target = target.transform;
        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { DestroyEffect(inst, skill.effectLifeTime); return; }
        settings.Targets = new[] { target.transform };
        float dmg = skill.damage;
        CharacterBase caster = owner;
        CharacterBase hitTarget = target;
        bool hit = false;
        settings.OnEffectCollisionEnter += col =>
        {
            if (hit) return;
            hit = true;
            if (hitTarget == null || hitTarget.IsDead) return;
            hitTarget.TakeSkillDamage(dmg, caster, skill);
        };
        DestroyEffect(inst, skill.effectLifeTime);
    }

    private bool UseSkillGlobalAoE(int slot)
    {
        if (owner == null || owner.IsDead) return false;
        float radius = GetSkillDamageRadius(slot);
        List<CharacterBase> targets = CharacterManager.Instance?.GetEnemiesInRadius(Vector3.zero, radius, owner) ?? new List<CharacterBase>();
        return FireInvulnerableGlobalSkill(slot, targets);
    }

    private bool UseSkillGlobalTeleport(int slot)
    {
        if (owner == null || owner.IsDead) return false;
        owner.TF.position = FindSafePosition(new Vector3(3f, 0f, 10f));
        List<CharacterBase> targets = CharacterManager.Instance?.GetAllLivingEnemies(owner) ?? new List<CharacterBase>();
        return FireInvulnerableGlobalSkill(slot, targets);
    }

    private bool FireInvulnerableGlobalSkill(int slot, List<CharacterBase> targets)
    {
        if (!CanFireSkillOnly(slot)) return false;
        CameraController.Instance?.MoveTo(new Vector3(0f, 60f, 0f));
        bool fired = TryFireSkillGlobalAoEAtPosition(slot, Vector3.zero, targets);
        if (fired)
        {
            owner.SetInvulnerable(true);
            StartCoroutine(EndInvulnerable());
        }
        return fired;
    }

    private System.Collections.IEnumerator EndInvulnerable()
    {
        while (owner != null && owner.IsCasting && !owner.IsDead) yield return null;
        owner?.SetInvulnerable(false);
    }

    private Vector3 FindSafePosition(Vector3 target)
    {
        MapManager map = MapManager.Instance;
        if (map == null) return target;
        Vector3 clamped = map.ClampToMap(target);
        if (!map.IsBlockedWorld(clamped)) return clamped;
        for (float r = 1f; r <= 5f; r += 1f)
        {
            for (int i = 0; i < 8; i++)
            {
                float rad = i * Mathf.PI * 0.25f;
                Vector3 candidate = map.ClampToMap(
                    new Vector3(target.x + Mathf.Cos(rad) * r, target.y, target.z + Mathf.Sin(rad) * r));
                if (!map.IsBlockedWorld(candidate)) return candidate;
            }
        }
        return clamped;
    }
}
