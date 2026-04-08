using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SwordOrbit : MonoBehaviour
{
    [SerializeField] private float radius = 1f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float pickupStartRadius = 4f;
    [SerializeField] private float pickupSpinDuration = 0.6f;
    [SerializeField] private Sword swordPrefab;
    [SerializeField] private int initialSwordCount = 0;

    private readonly List<Sword> swords = new();
    private readonly List<SpinData> spinList = new();

    private float invPickupDuration;
    private const float TWO_PI = Mathf.PI * 2f;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;
    private const float PI = Mathf.PI;

    public float RotateSpeed => rotateSpeed;
    public bool IsPlayer { get; set; }

    private struct SpinData
    {
        public Sword sword;
        public float startAngle;
        public float elapsed;
    }

    private void Start()
    {
        invPickupDuration = 1f / pickupSpinDuration;
        
        if (initialSwordCount == 0) return;
        
        float stepRad = TWO_PI / initialSwordCount;
        for (int i = 0; i < initialSwordCount; i++)
        {
            Sword sword = Instantiate(swordPrefab, transform);
            sword.AttachToOrbit(this);
            sword.SetOrbiting();
            Transform t = sword.transform;
            float a = stepRad * i;
            PlaceSwordAt(t, a);
            sword.CurrentAngle = a;
            swords.Add(sword);
        }
    }

    public void AddSword(Sword sword)
    {
        sword.AttachToOrbit(this);
        Transform t = sword.transform;
        t.SetParent(transform);
        
        Vector3 pos = t.localPosition;
        pos.z = -0.1f;
        t.localPosition = pos;
        
        t.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        t.DOScale(1f, pickupSpinDuration).SetEase(Ease.OutBack);

        spinList.Add(new SpinData
        {
            sword = sword,
            startAngle = Mathf.Atan2(pos.y, pos.x),
            elapsed = 0f
        });
    }

    public void RemoveSword(Sword sword)
    {
        swords.Remove(sword);
    }

    private void FinishSpinIn(Sword sword, float currentAngle)
    {
        swords.Add(sword);
        sword.CurrentAngle = currentAngle;
        
        Transform t = sword.transform;
        Vector3 pos = t.localPosition;
        pos.z = 0f;
        t.localPosition = pos;
        
        sword.SetOrbiting();
        Redistribute();
    }

    private void Redistribute()
    {
        int count = swords.Count;
        if (count == 0) return;

        float newStepRad = TWO_PI / count;

        for (int i = 0; i < count; i++)
        {
            Sword sword = swords[count - 1 - i];
            Transform sw = sword.transform;
            float targetAngle = newStepRad * i;
            float fromAngle = sword.CurrentAngle;

            float diff = targetAngle - fromAngle;
            while (diff < 0f) diff += TWO_PI;
            while (diff >= TWO_PI) diff -= TWO_PI;

            if (diff < 0.01f)
            {
                PlaceSwordAt(sw, targetAngle);
                sword.CurrentAngle = targetAngle;
                continue;
            }

            sw.DOKill();

            float duration = Mathf.Max(0.4f, diff * RAD_TO_DEG / 360f);

            DOTween.To(
                () => fromAngle,
                angle =>
                {
                    sword.CurrentAngle = angle;
                    PlaceSwordAt(sw, angle);
                },
                fromAngle + diff,
                duration
            )
            .SetEase(Ease.InOutSine)
            .SetTarget(sw)
            .OnComplete(() =>
            {
                sword.CurrentAngle = targetAngle;
                PlaceSwordAt(sw, targetAngle);
            });
        }
    }

    private void PlaceSwordAt(Transform sw, float angleRad)
    {
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);
        sw.localPosition = new Vector3(cos * radius, sin * radius, 0f);
        sw.localRotation = Quaternion.Euler(0f, 0f, angleRad * RAD_TO_DEG - 90f);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        int spinCount = spinList.Count;
        if (spinCount == 0) return;

        float dt = Time.deltaTime;

        for (int i = spinCount - 1; i >= 0; i--)
        {
            SpinData d = spinList[i];
            d.elapsed += dt;
            float progress = Mathf.Clamp01(d.elapsed * invPickupDuration);

            if (d.elapsed >= pickupSpinDuration)
            {
                spinList.RemoveAt(i);
                float finalAngle = (d.startAngle + TWO_PI) % TWO_PI;
                if (finalAngle < 0f) finalAngle += TWO_PI;
                FinishSpinIn(d.sword, finalAngle);
            }
            else
            {
                float a = d.startAngle + TWO_PI * progress;
                float blend = progress * progress * (3f - 2f * progress);
                float currentRadius = Mathf.Lerp(pickupStartRadius, radius, blend);

                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);
                float rotZ = (a + PI) * RAD_TO_DEG;
                
                if (progress > 0.5f)
                {
                    float orientBlend = (progress - 0.5f) * 2f;
                    orientBlend = orientBlend * orientBlend * (3f - 2f * orientBlend);
                    rotZ = Mathf.LerpAngle(rotZ, a * RAD_TO_DEG - 90f, orientBlend);
                }

                Transform tf = d.sword.transform;
                tf.localPosition = new Vector3(cos * currentRadius, sin * currentRadius, -0.1f);
                tf.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                spinList[i] = d;
            }
        }
    }
}
