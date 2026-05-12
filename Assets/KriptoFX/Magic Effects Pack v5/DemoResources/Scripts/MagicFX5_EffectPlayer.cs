using System;
using UnityEngine;
using MagicFX5;

/// <summary>
/// Gọi Play() để kích hoạt 1 chuỗi hiệu ứng hoàn chỉnh.
/// Gắn script này lên bất kỳ GameObject nào, điền các field trên Inspector rồi gọi Play().
/// </summary>
public class MagicFX5_EffectPlayer : MonoBehaviour
{
    [Header("Hiệu ứng cần spawn")]
    public GameObject EffectPrefab;
    public Transform  SpawnPoint;       // null = dùng vị trí của GameObject này

    [Header("Mục tiêu")]
    public Transform[] Targets;

    [Header("Tùy chọn")]
    public string[] EnemyDeadTriggerNames;
    public float    EnemyDisintegrationDelay = 5f;  // 0 = tắt tính năng tan rã
    public float    EffectLifeTime           = 10f;

    public Action<MagicFX5_EffectSettings.EffectCollisionHit> OnHit; // callback tuỳ chọn

    private GameObject _instance;

    // Dùng Targets được set sẵn trên Inspector
    public void Play()
    {
        Play(Targets);
    }

    // Truyền targets động từ code
    public void Play(Transform[] targets)
    {
        if (EffectPrefab == null)
        {
            Debug.LogWarning("[EffectPlayer] EffectPrefab chưa được gán.");
            return;
        }

        Stop(); // hủy instance cũ nếu còn

        var pos = SpawnPoint != null ? SpawnPoint.position : transform.position;
        var rot = SpawnPoint != null ? SpawnPoint.rotation : transform.rotation;

        _instance = Instantiate(EffectPrefab, pos, rot);

        var settings = _instance.GetComponent<MagicFX5_EffectSettings>();
        if (settings == null)
        {
            Debug.LogWarning("[EffectPlayer] Prefab không có MagicFX5_EffectSettings.");
            return;
        }

        settings.Targets = targets;

        if (EnemyDeadTriggerNames != null && EnemyDeadTriggerNames.Length > 0)
            settings.AnimatorRandomTriggerNames = EnemyDeadTriggerNames;

        settings.OnEffectCollisionEnter += HandleCollision;

        Destroy(_instance, EffectLifeTime);
    }

    public void Stop()
    {
        if (_instance == null) return;

        var settings = _instance.GetComponent<MagicFX5_EffectSettings>();
        if (settings != null) settings.OnEffectCollisionEnter -= HandleCollision;

        Destroy(_instance);
        _instance = null;
    }

    private void HandleCollision(MagicFX5_EffectSettings.EffectCollisionHit hit)
    {
        if (EnemyDisintegrationDelay > 0)
        {
            var dis = hit.Target.GetComponent<MagicFX5_EnemyDisintegration>();
            if (dis != null) dis.Disintegrate(EnemyDisintegrationDelay);
        }

        OnHit?.Invoke(hit);
    }
}
