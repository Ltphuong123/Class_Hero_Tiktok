using System;
using UnityEngine;
using MagicFX5;

public class SkillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterBase owner;

    [Header("Spawn Points")]
    [Tooltip("Điểm spawn MainEffect và HandEffect (thường là tay phải)")]
    [SerializeField] private Transform rightHandPosition;
    [Tooltip("Điểm spawn CharacterEffect (thường là trung tâm người)")]
    [SerializeField] private Transform characterEffectPosition;

    [Header("Skills")]
    [SerializeField] private SkillData[] skills;

    private float[] cooldownTimers;
    private int currentSkillIndex = -1;
    private CharacterBase currentTarget;

    public event Action<CharacterBase, float> OnSkillHit;

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
    }

    public bool TryFireSkill(CharacterBase target, int skillIndex = 0)
    {
        if (!CanFire(skillIndex, target)) return false;

        currentSkillIndex = skillIndex;
        currentTarget     = target;
        cooldownTimers[skillIndex] = skills[skillIndex].cooldown;
        owner?.StartCast(skills[skillIndex].castDuration);
        StartCoroutine(ShakeAfterDelay(skills[skillIndex]));

        ApplySlowToTarget(skills[skillIndex], target);
        TriggerHandEffect();
        TriggerMainEffect();
        TriggerBuffEffect();
        return true;
    }

    public bool TryFireSkillNoTarget(int skillIndex = 0)
    {
        if (!CanFireSkillOnly(skillIndex)) return false;

        currentSkillIndex = skillIndex;
        currentTarget     = null;
        cooldownTimers[skillIndex] = skills[skillIndex].cooldown;
        owner?.StartCast(skills[skillIndex].castDuration);
        StartCoroutine(ShakeAfterDelay(skills[skillIndex]));

        TriggerHandEffect();
        TriggerBuffEffect();

        SkillData skill = skills[skillIndex];
        if (skill.mainEffect != null)
        {
            Transform spawnTF = rightHandPosition ?? transform;
            GameObject inst = Instantiate(skill.mainEffect, spawnTF.position, spawnTF.rotation);
            if (skill.effectScale != 1f) inst.transform.localScale = Vector3.one * skill.effectScale;
            Destroy(inst, skill.effectLifeTime);
        }

        return true;
    }

    public bool TryFireSkillMultiTarget(System.Collections.Generic.List<CharacterBase> targets, int skillIndex = 0)
    {
        if (targets == null || targets.Count == 0) return false;
        if (!CanFireSkillOnly(skillIndex)) return false;

        currentSkillIndex          = skillIndex;
        currentTarget              = targets[0];
        cooldownTimers[skillIndex] = skills[skillIndex].cooldown;
        owner?.StartCast(skills[skillIndex].castDuration);
        StartCoroutine(ShakeAfterDelay(skills[skillIndex]));

        ApplySlowToTargets(skills[skillIndex], targets);
        TriggerHandEffect();
        TriggerBuffEffect();
        SpawnMainEffectMultiTarget(targets, skills[skillIndex]);
        return true;
    }

    private void SpawnMainEffectMultiTarget(System.Collections.Generic.List<CharacterBase> targets, SkillData skill)
    {
        if (skill.mainEffect == null) return;

        Transform spawnTF = rightHandPosition != null ? rightHandPosition : transform;
        Vector3 dir = targets[0].transform.position - spawnTF.position;
        dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : spawnTF.rotation;

        GameObject inst = Instantiate(skill.mainEffect, spawnTF.position, rot);
        if (skill.effectScale != 1f) inst.transform.localScale = Vector3.one * skill.effectScale;

        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { Destroy(inst, skill.effectLifeTime); return; }

        var targetTransforms = new Transform[targets.Count];
        for (int i = 0; i < targets.Count; i++)
            targetTransforms[i] = targets[i].transform;
        settings.Targets = targetTransforms;

        float         dmg             = skill.damage;
        CharacterBase caster          = owner;
        var           capturedTargets = new System.Collections.Generic.List<CharacterBase>(targets);
        bool          hit             = false;

        settings.OnEffectCollisionEnter += col =>
        {
            if (hit) return;
            hit = true;
            foreach (CharacterBase t in capturedTargets)
            {
                if (t == null || t == caster || t.IsDead) continue;
                t.TakeSkillDamage(dmg, caster, skill);
                OnSkillHit?.Invoke(t, dmg);
            }
        };

        Destroy(inst, skill.effectLifeTime);
    }

    public bool TryFireSkillAtTargetWithAllTargets(CharacterBase primaryTarget, System.Collections.Generic.List<CharacterBase> targets, int skillIndex)
    {
        if (targets == null || targets.Count == 0) return false;
        if (!CanFire(skillIndex, primaryTarget)) return false;

        currentSkillIndex          = skillIndex;
        currentTarget              = primaryTarget;
        cooldownTimers[skillIndex] = skills[skillIndex].cooldown;
        owner?.StartCast(skills[skillIndex].castDuration);
        StartCoroutine(ShakeAfterDelay(skills[skillIndex]));

        ApplySlowToTargets(skills[skillIndex], targets);
        TriggerHandEffect();
        TriggerBuffEffect();
        SpawnMainEffectAtTargetWithAllTargets(primaryTarget, targets, skills[skillIndex]);
        return true;
    }

    private void SpawnMainEffectAtTargetWithAllTargets(CharacterBase primaryTarget, System.Collections.Generic.List<CharacterBase> targets, SkillData skill)
    {
        if (skill.mainEffect == null) return;

        Transform spawnTF = rightHandPosition != null ? rightHandPosition : transform;
        Vector3 dir = primaryTarget.transform.position - spawnTF.position;
        dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : spawnTF.rotation;

        GameObject inst = Instantiate(skill.mainEffect, primaryTarget.transform.position, rot);
        if (skill.effectScale != 1f) inst.transform.localScale = Vector3.one * skill.effectScale;

        var followTarget = inst.GetComponentInChildren<MagicFX5.MagicFX5_FollowTarget>();
        if (followTarget) followTarget.Target = primaryTarget.transform;

        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { Destroy(inst, skill.effectLifeTime); return; }

        var targetTransforms = new Transform[targets.Count];
        for (int i = 0; i < targets.Count; i++)
            targetTransforms[i] = targets[i].transform;
        settings.Targets = targetTransforms;

        float         dmg    = skill.damage;
        CharacterBase caster = owner;
        var           hitSet = new System.Collections.Generic.HashSet<CharacterBase>();

        settings.OnEffectCollisionEnter += col =>
        {
            CharacterBase hitChar = col.Target?.GetComponent<CharacterBase>();
            if (hitChar == null || hitChar == caster || hitChar.IsDead || !hitSet.Add(hitChar)) return;
            hitChar.TakeSkillDamage(dmg, caster, skill);
            OnSkillHit?.Invoke(hitChar, dmg);
        };

        Destroy(inst, skill.effectLifeTime);
    }

    public bool TryFireSkillSelfAoE(int skillIndex)
    {
        if (!CanFireSkillOnly(skillIndex)) return false;
        if (owner == null) return false;

        SkillData skill    = skills[skillIndex];
        currentSkillIndex  = skillIndex;
        currentTarget      = null;
        cooldownTimers[skillIndex] = skill.cooldown;
        owner.StartCast(skill.castDuration);
        StartCoroutine(ShakeAfterDelay(skill));

        Vector3 center = owner.TF.position;

        var nearby = new System.Collections.Generic.List<CharacterBase>();
        CharacterManager.Instance?.GetNearbyCharacters(center, skill.damageRadius, nearby);

        var targets = new System.Collections.Generic.List<CharacterBase>();
        foreach (CharacterBase t in nearby)
        {
            if (t == null || t == owner || t.IsDead) continue;
            targets.Add(t);
        }

        ApplySlowToTargets(skill, targets);

        TriggerHandEffect();
        TriggerBuffEffect();

        if (skill.mainEffect == null) return true;

        GameObject inst = Instantiate(skill.mainEffect, center, Quaternion.identity);
        if (skill.effectScale != 1f) inst.transform.localScale = Vector3.one * skill.effectScale;

        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { Destroy(inst, skill.effectLifeTime); return true; }

        var targetTransforms = new Transform[targets.Count];
        for (int i = 0; i < targets.Count; i++)
            targetTransforms[i] = targets[i].transform;
        settings.Targets = targetTransforms;

        float         dmg             = skill.damage;
        CharacterBase caster          = owner;
        var           capturedTargets = new System.Collections.Generic.List<CharacterBase>(targets);
        bool          hit             = false;

        settings.OnEffectCollisionEnter += col =>
        {
            if (hit) return;
            hit = true;
            foreach (CharacterBase t in capturedTargets)
            {
                if (t == null || t == caster || t.IsDead) continue;
                t.TakeSkillDamage(dmg, caster, skill);
                OnSkillHit?.Invoke(t, dmg);
            }
        };

        Destroy(inst, skill.effectLifeTime);
        return true;
    }

    private void ApplySlowToTarget(SkillData skill, CharacterBase target)
    {
        if (!skill.enableSlow || target == null || target.IsDead) return;
        target.ApplySlow(skill.slowFactor, skill.slowDuration);
    }

    private void ApplySlowToTargets(SkillData skill, System.Collections.Generic.List<CharacterBase> targets)
    {
        if (!skill.enableSlow) return;
        foreach (CharacterBase t in targets)
        {
            if (t == null || t.IsDead) continue;
            t.ApplySlow(skill.slowFactor, skill.slowDuration);
        }
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

    public Vector3 GetSpawnPosition() =>
        rightHandPosition != null ? rightHandPosition.position : transform.position;

    public bool TryFireSkillAoEAtTarget(CharacterBase target, int skillIndex = 2)
    {
        if (!CanFire(skillIndex, target)) return false;

        currentSkillIndex          = skillIndex;
        currentTarget              = target;
        cooldownTimers[skillIndex] = skills[skillIndex].cooldown;
        owner?.StartCast(skills[skillIndex].castDuration);
        StartCoroutine(ShakeAfterDelay(skills[skillIndex]));

        ApplySlowToTarget(skills[skillIndex], target);
        TriggerHandEffect();
        TriggerMainEffectAoEAtTarget(target, skills[skillIndex]);
        TriggerBuffEffect();
        return true;
    }

    private void TriggerMainEffectAoEAtTarget(CharacterBase target, SkillData skill)
    {
        if (skill.mainEffect == null) return;

        Transform spawnTF = rightHandPosition != null ? rightHandPosition : transform;
        Vector3 dir = target.transform.position - spawnTF.position;
        dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : spawnTF.rotation;

        GameObject inst = Instantiate(skill.mainEffect, target.transform.position, rot);
        if (skill.effectScale != 1f) inst.transform.localScale = Vector3.one * skill.effectScale;

        var followTarget = inst.GetComponentInChildren<MagicFX5.MagicFX5_FollowTarget>();
        if (followTarget) followTarget.Target = target.transform;

        MagicFX5_EffectSettings settings = inst.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) { Destroy(inst, skill.effectLifeTime); return; }

        settings.Targets = new[] { target.transform };

        float         dmg       = skill.damage;
        float         radius    = skill.damageRadius;
        int           maxCount  = skill.maxTargets;
        CharacterBase caster    = owner;
        CharacterBase hitTarget = target;
        bool          hit       = false;

        settings.OnEffectCollisionEnter += col =>
        {
            if (hit) return;
            hit = true;

            Vector3 aoeCenter = hitTarget != null && !hitTarget.IsDead
                ? hitTarget.TF.position
                : col.Position;

            if (radius > 0f)
            {
                var nearby = new System.Collections.Generic.List<CharacterBase>(maxCount);
                CharacterManager.Instance?.GetNearbyCharacters(aoeCenter, radius, nearby);

                int count = 0;
                for (int i = 0; i < nearby.Count && count < maxCount; i++)
                {
                    CharacterBase c = nearby[i];
                    if (c == caster || c.IsDead) continue;
                    c.TakeSkillDamage(dmg, caster, skill);
                    OnSkillHit?.Invoke(c, dmg);
                    count++;
                }
            }
            else
            {
                if (hitTarget == null || hitTarget.IsDead) return;
                hitTarget.TakeSkillDamage(dmg, caster, skill);
                OnSkillHit?.Invoke(hitTarget, dmg);
            }
        };

        Destroy(inst, skill.effectLifeTime);
    }

    private void TriggerMainEffect()
    {
        if (currentSkillIndex < 0 || currentTarget == null || currentTarget.IsDead) return;

        SkillData skill = skills[currentSkillIndex];
        if (skill.mainEffect == null) return;

        Transform spawnTF = rightHandPosition != null ? rightHandPosition : transform;
        Vector3 dir = currentTarget.transform.position - spawnTF.position;
        dir.y = 0f;
        Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : spawnTF.rotation;
        GameObject inst = Instantiate(skill.mainEffect, spawnTF.position, rot);
        if (skill.effectScale != 1f) inst.transform.localScale = Vector3.one * skill.effectScale;
        ApplyEffectSettings(inst, skill);
    }

    private void TriggerHandEffect()
    {
        if (currentSkillIndex < 0) return;

        SkillData skill = skills[currentSkillIndex];
        if (skill.handEffect == null) return;

        Transform parent = rightHandPosition != null ? rightHandPosition : transform;
        GameObject handInst = Instantiate(skill.handEffect, parent.position, Quaternion.identity, parent);
        if (skill.effectScale != 1f) handInst.transform.localScale = Vector3.one * skill.effectScale;
        Destroy(handInst, skill.effectLifeTime);
    }

    private void TriggerBuffEffect()
    {
        if (currentSkillIndex < 0) return;

        SkillData skill = skills[currentSkillIndex];
        if (skill.characterEffect == null) return;

        Transform spawnTF = characterEffectPosition != null ? characterEffectPosition : transform;
        GameObject charInst = Instantiate(skill.characterEffect, spawnTF.position, spawnTF.rotation);
        if (skill.effectScale != 1f) charInst.transform.localScale = Vector3.one * skill.effectScale;
        Destroy(charInst, skill.effectLifeTime);
    }

    private void ApplyEffectSettings(GameObject instance, SkillData skill)
    {
        MagicFX5_EffectSettings settings = instance.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null) return;

        if (currentTarget != null)
            settings.Targets = new[] { currentTarget.transform };

        float         dmg        = skill.damage;
        float         radius     = skill.damageRadius;
        int           maxTargets = skill.maxTargets;
        CharacterBase caster     = owner;
        bool          hit        = false;

        settings.OnEffectCollisionEnter += col =>
        {
            if (hit) return;
            hit = true;

            Vector3 hitPos = col.Position;

            if (radius > 0f && maxTargets > 1)
            {
                var nearby = new System.Collections.Generic.List<CharacterBase>(maxTargets);
                CharacterManager.Instance?.GetNearbyCharacters(hitPos, radius, nearby);

                int count = 0;
                for (int i = 0; i < nearby.Count && count < maxTargets; i++)
                {
                    CharacterBase c = nearby[i];
                    if (c == caster || c.IsDead) continue;
                    c.TakeSkillDamage(dmg, caster, skill);
                    OnSkillHit?.Invoke(c, dmg);
                    count++;
                }
            }
            else
            {
                CharacterBase hitChar = col.Target?.GetComponent<CharacterBase>();
                if (hitChar == null || hitChar == caster || hitChar.IsDead) return;
                hitChar.TakeSkillDamage(dmg, caster, skill);
                OnSkillHit?.Invoke(hitChar, dmg);
            }
        };

        Destroy(instance, skill.effectLifeTime);
    }

    private System.Collections.IEnumerator ShakeAfterDelay(SkillData skill)
    {
        if (skill.shakeOnFireDuration <= 0f) yield break;
        if (skill.shakeDelay > 0f) yield return new WaitForSeconds(skill.shakeDelay);
        CameraController.Instance?.Shake(skill.shakeOnFireDuration, skill.shakeOnFireMagnitude);
    }

    private bool CanFire(int skillIndex, CharacterBase target)
    {
        if (!CanFireSkillOnly(skillIndex)) return false;
        if (target == null || target.IsDead) return false;
        return true;
    }

    private bool CanFireSkillOnly(int skillIndex)
    {
        if (owner == null || owner.IsDead || owner.IsFrozen) return false;
        if (skills == null || (uint)skillIndex >= (uint)skills.Length) return false;
        if (cooldownTimers[skillIndex] > 0f) return false;
        return true;
    }
}
