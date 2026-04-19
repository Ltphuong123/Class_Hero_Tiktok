using System.Collections.Generic;
using UnityEngine;

public class SwordOrbit : MonoBehaviour
{
    [SerializeField] private float radius = 1.2f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float flyAroundDuration = 0.6f;
    [SerializeField] private float flyStartRadius = 4f;
    [SerializeField] private Sword swordPrefab;
    [SerializeField] private int initialSwordCount = 0;
    [SerializeField] private SwordType currentSwordType = SwordType.Default;
    
    private readonly List<Sword> swords = new();
    private const float TWO_PI = Mathf.PI * 2f;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;

    public float RotateSpeed => rotateSpeed;
    public float Radius => radius;
    public int SwordCount => swords.Count;

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
    public bool IsPlayer { get; set; }

    public void SetSwordType(SwordType type)
    {
        currentSwordType = type;
        
        int count = swords.Count;
        for (int i = 0; i < count; i++)
        {
            swords[i].SetSwordType(type);
        }

        Debug.Log($"[SwordOrbit] Đã đổi {count} kiếm sang type {type} và reset HP");
    }
    private void Start()
    {
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
    }

    public void RemoveSword(Sword sword)
    {
        swords.Remove(sword);
    }

    /// <summary>
    /// Rơi kiếm tại index ra map (dùng khi chết).
    /// </summary>
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
    }
}
