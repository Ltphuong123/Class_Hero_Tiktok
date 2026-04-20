using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ParticleThreshold
{
    [Tooltip("Số kiếm cần để bật particle này")]
    public int swordCount;
    
    [Tooltip("Particle system sẽ chạy khi đủ số kiếm")]
    public ParticleSystem particle;
}

[System.Serializable]
public class SwordTypeParticles
{
    [Tooltip("Loại kiếm")]
    public SwordType swordType;
    
    [Tooltip("Danh sách particles với các mức kiếm khác nhau (VD: 5 kiếm, 10 kiếm, 15 kiếm, 25 kiếm...)")]
    public ParticleThreshold[] particleThresholds;
}

public class SwordOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private float radius = 1.2f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float flyAroundDuration = 0.6f;
    [SerializeField] private float flyStartRadius = 4f;
    
    [Header("Sword Settings")]
    [SerializeField] private Sword swordPrefab;
    [SerializeField] private int initialSwordCount = 0;
    [SerializeField] private SwordType currentSwordType = SwordType.Default;

    [Header("Particle Effects")]
    [Tooltip("Cấu hình particles cho mỗi loại kiếm. Mỗi loại có thể có nhiều mức kiếm khác nhau.")]
    [SerializeField] private SwordTypeParticles[] particlesByType;
    
    private readonly List<Sword> swords = new();
    private const float TWO_PI = Mathf.PI * 2f;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;

    private List<ParticleSystem> activeParticles = new();
    private int lastSwordCount = 0;

    public float RotateSpeed => rotateSpeed;
    public float Radius => radius;
    public int SwordCount => swords.Count;
    public bool IsPlayer { get; set; }

    public void IncreaseRadius(float amount)
    {
        radius += amount;
        RepositionAllSwords();
    }

    private void RepositionAllSwords()
    {
        for (int i = 0; i < swords.Count; i++)
        {
            Sword s = swords[i];
            switch (s.State)
            {
                case SwordState.Orbiting:
                    PlaceSword(s.transform, s.CurrentAngle);
                    break;
                case SwordState.FlyingIn:
                    s.UpdateFlyOrbitRadius(radius);
                    break;
                case SwordState.Sliding:
                    s.UpdateSlideRadius(radius);
                    break;
            }
        }
    }

    public void SetSwordType(SwordType type)
    {
        if (currentSwordType == type) return;

        SwordType oldType = currentSwordType;
        currentSwordType = type;
        
        int count = swords.Count;
        for (int i = 0; i < count; i++)
        {
            swords[i].SetSwordType(type);
        }

        UpdateParticleEffects();

        Debug.Log($"[SwordOrbit] Đổi từ {oldType} → {type} | Đã tắt tất cả particles cũ và bật particles mới");
    }

    private void Start()
    {
        InitializeParticles();

        if (initialSwordCount == 0) return;

        float step = TWO_PI / initialSwordCount;

        for (int i = 0; i < initialSwordCount; i++)
        {
            Sword sword = Instantiate(swordPrefab, transform);
            float angle = step * i;

            sword.AttachToOrbit(this);
            sword.SetOrbiting();
            sword.CurrentAngle = angle;
            PlaceSword(sword.transform, angle);

            swords.Add(sword);
        }

        lastSwordCount = swords.Count;
        UpdateParticleEffects();
    }

    public void AddSword(Sword sword)
    {
        sword.AttachToOrbit(this);
        sword.SetSwordType(currentSwordType);

        Transform t = sword.transform;
        t.SetParent(transform);
        t.localScale = Vector3.one;

        swords.Add(sword);

        float step = TWO_PI / swords.Count;

        RedistributeExistingSwords(step);
        FlyInNewSword(sword, t, step);

        CheckParticleThresholds();
    }

    public void RemoveSword(Sword sword)
    {
        swords.Remove(sword);
        CheckParticleThresholds();
    }

    public void DropSword(int index)
    {
        if (index < 0 || index >= swords.Count) return;
        Sword sword = swords[index];
        sword.KnockOff();
    }

    public void OnSwordFlyComplete(Sword sword) { }

    private void RedistributeExistingSwords(float step)
    {
        int count = swords.Count;

        for (int i = 0; i < count - 1; i++)
        {
            Sword s = swords[i];
            float target = step * i;

            switch (s.State)
            {
                case SwordState.FlyingIn:
                    s.UpdateFlyTarget(target);
                    break;

                case SwordState.Sliding:
                    s.UpdateSlideTarget(target, radius);
                    break;

                default:
                    s.StartSlide(s.CurrentAngle, target, radius);
                    break;
            }
        }
    }

    private void FlyInNewSword(Sword sword, Transform t, float step)
    {
        float startAngle = Mathf.Atan2(t.localPosition.y, t.localPosition.x);
        float targetAngle = step * (swords.Count - 1);

        sword.StartFlyIn(startAngle, targetAngle, flyStartRadius, radius, flyAroundDuration);
    }

    private void PlaceSword(Transform sw, float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        sw.localPosition = new Vector3(cos * radius, sin * radius, 0f);
        sw.localRotation = Quaternion.Euler(0f, 0f, angle * RAD_TO_DEG - 90f);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        if (lastSwordCount != swords.Count)
        {
            lastSwordCount = swords.Count;
            CheckParticleThresholds();
        }
    }

    #region Particle System

    private void InitializeParticles()
    {
        activeParticles = new List<ParticleSystem>();

        if (particlesByType != null)
        {
            foreach (var particleSet in particlesByType)
            {
                if (particleSet.particleThresholds != null)
                {
                    foreach (var threshold in particleSet.particleThresholds)
                    {
                        if (threshold.particle != null)
                        {
                            threshold.particle.Stop();
                            threshold.particle.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private void CheckParticleThresholds()
    {
        int count = swords.Count;

        SwordTypeParticles particleSet = GetParticleSetForType(currentSwordType);
        if (particleSet == null || particleSet.particleThresholds == null) return;

        foreach (var threshold in particleSet.particleThresholds)
        {
            if (threshold.particle == null) continue;

            bool shouldBeActive = count >= threshold.swordCount;
            UpdateParticleState(threshold.particle, shouldBeActive, threshold.swordCount);
        }
    }

    private void UpdateParticleEffects()
    {
        StopAllParticles();
        activeParticles.Clear();
        CheckParticleThresholds();
    }

    private void StopAllParticles()
    {
        int stoppedCount = 0;

        foreach (var particle in activeParticles)
        {
            if (particle != null && particle.gameObject.activeSelf)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.gameObject.SetActive(false);
                stoppedCount++;
            }
        }

        if (particlesByType != null)
        {
            foreach (var particleSet in particlesByType)
            {
                if (particleSet.particleThresholds != null)
                {
                    foreach (var threshold in particleSet.particleThresholds)
                    {
                        if (threshold.particle != null && threshold.particle.gameObject.activeSelf)
                        {
                            threshold.particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            threshold.particle.gameObject.SetActive(false);
                            stoppedCount++;
                        }
                    }
                }
            }
        }

        if (stoppedCount > 0)
        {
            Debug.Log($"[SwordOrbit] 🛑 Đã tắt {stoppedCount} particles");
        }
    }

    private void UpdateParticleState(ParticleSystem particle, bool shouldBeActive, int threshold)
    {
        if (particle == null) return;

        if (shouldBeActive)
        {
            if (!particle.gameObject.activeSelf)
            {
                particle.gameObject.SetActive(true);
                particle.Play();
                
                if (!activeParticles.Contains(particle))
                    activeParticles.Add(particle);

                Debug.Log($"[SwordOrbit] ✅ Bật particle: {particle.name} cho {currentSwordType} (Threshold: {threshold}, Swords: {swords.Count})");
            }
        }
        else
        {
            if (particle.gameObject.activeSelf)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.gameObject.SetActive(false);
                
                activeParticles.Remove(particle);

                Debug.Log($"[SwordOrbit] ❌ Tắt particle: {particle.name} (Threshold: {threshold})");
            }
        }
    }

    private SwordTypeParticles GetParticleSetForType(SwordType type)
    {
        if (particlesByType == null) return null;

        foreach (var particleSet in particlesByType)
        {
            if (particleSet.swordType == type)
                return particleSet;
        }

        return null;
    }

    #endregion

    #region Public API

    public void RefreshParticles()
    {
        UpdateParticleEffects();
    }

    public int GetActiveParticleCount()
    {
        return activeParticles.Count;
    }

    public List<ParticleSystem> GetActiveParticles()
    {
        return new List<ParticleSystem>(activeParticles);
    }

    public ParticleThreshold[] GetCurrentThresholds()
    {
        SwordTypeParticles particleSet = GetParticleSetForType(currentSwordType);
        return particleSet?.particleThresholds;
    }

    #endregion
}
