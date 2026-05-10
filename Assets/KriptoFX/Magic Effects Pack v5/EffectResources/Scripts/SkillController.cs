// using System;
// using System.Collections;
// using System.Collections.Generic;
// using MagicFX5;
// using UnityEngine;

// public class SkillController : MonoBehaviour
// {
//     [Serializable]
//     public class SkillInfo
//     {
//         public string       skillName;
//         public GameObject   effectPrefab;
//         public Transform    spawnTransform;  // null = dùng vị trí player
//         public float        damage       = 100f;
//         public float        cooldown     = 3f;
//         public float        targetRadius = 10f;
//         public bool         singleTarget = false; // true = chỉ nhắm 1 quái (lockedTarget hoặc gần nhất)

//         [NonSerialized] public float lastUsedTime = float.MinValue;
//     }

//     [Header("References")]
//     [SerializeField] private CharacterBase owner;
//     [SerializeField] private SkillInfo[]   skills;

//     [Header("Test")]
//     [SerializeField] private bool        testMode       = false;
//     [SerializeField] private int         testSkillIndex = 0;
//     [SerializeField] private float       testDelay      = 1f;
//     [SerializeField] private Transform[] testTargets;   // gán trực tiếp từ Inspector

//     private void Awake()
//     {
//         if (owner == null) owner = GetComponent<CharacterBase>();
//     }

//     private void Start()
//     {
//         if (testMode) StartCoroutine(TestRoutine());
//     }

//     private IEnumerator TestRoutine()
//     {
//         yield return new WaitForSeconds(testDelay);
//         UseSkill(testSkillIndex);
//     }

//     public int SkillCount => skills?.Length ?? 0;

//     public bool CanUseSkill(int index)
//     {
//         if (index < 0 || index >= skills.Length) return false;
//         if (owner == null || owner.IsDead) return false;
//         return Time.time - skills[index].lastUsedTime >= skills[index].cooldown;
//     }

//     public float GetCooldownRemaining(int index)
//     {
//         if (index < 0 || index >= skills.Length) return 0f;
//         return Mathf.Max(0f, skills[index].cooldown - (Time.time - skills[index].lastUsedTime));
//     }

//     public void UseSkill(int index)
//     {
//         if (!CanUseSkill(index)) return;

//         var skill = skills[index];
//         skill.lastUsedTime = Time.time;

//         // testTargets ưu tiên hơn auto-find khi đang test
//         Transform[] targetTransforms;
//         if (testMode && testTargets != null && testTargets.Length > 0)
//         {
//             targetTransforms = testTargets;
//         }
//         else
//         {
//             var targets = GatherTargets(skill);
//             if (targets.Count == 0) return;
//             targetTransforms = new Transform[targets.Count];
//             for (int i = 0; i < targets.Count; i++)
//                 targetTransforms[i] = targets[i].TF;
//         }

//         var spawnPos = skill.spawnTransform != null ? skill.spawnTransform.position : OwnerTF.position;
//         var spawnRot = skill.spawnTransform != null ? skill.spawnTransform.rotation : OwnerTF.rotation;

//         var effectGO = Instantiate(skill.effectPrefab, spawnPos, spawnRot);
//         if (skill.spawnTransform != null)
//             effectGO.transform.localScale = skill.spawnTransform.lossyScale;
//         var settings  = effectGO.GetComponent<MagicFX5_EffectSettings>();
//         if (settings == null) return;

//         settings.Targets                          = targetTransforms;
//         settings.UseAnimatorTriggerAfterCollision = false;
//         settings.UseRagdollForce                  = false;

//         float         damage   = skill.damage;
//         CharacterBase attacker = owner;

//         settings.OnEffectCollisionEnter += hit =>
//         {
//             // TransformMotion không invoke OnEffectImpactActivated, phải forward thủ công
//             settings.OnEffectImpactActivated?.Invoke(hit);
//             hit.Target.GetComponent<CharacterBase>()?.TakeDamage(damage, attacker);
//         };
//     }

//     private List<CharacterBase> GatherTargets(SkillInfo skill)
//     {
//         var result = new List<CharacterBase>();

//         if (CharacterManager.Instance == null) return result;

//         if (skill.singleTarget)
//         {
//             var locked = owner.LockedTarget;
//             if (locked != null && !locked.IsDead)
//             {
//                 result.Add(locked);
//             }
//             else
//             {
//                 CharacterManager.Instance.GetCharactersInRadius(OwnerTF.position, skill.targetRadius, result);
//                 result.RemoveAll(c => c == owner || c.IsDead);
//                 result.Sort((a, b) =>
//                     (a.TF.position - OwnerTF.position).sqrMagnitude
//                     .CompareTo((b.TF.position - OwnerTF.position).sqrMagnitude));
//                 if (result.Count > 1)
//                     result.RemoveRange(1, result.Count - 1);
//             }
//         }
//         else
//         {
//             CharacterManager.Instance.GetCharactersInRadius(OwnerTF.position, skill.targetRadius, result);
//             result.RemoveAll(c => c == owner || c.IsDead);
//         }

//         return result;
//     }

//     private Transform OwnerTF => owner != null ? owner.TF : transform;
// }
