using System.Collections.Generic;
using UnityEngine;

public class SwordOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private CharacterBase owner;
    [SerializeField] private float radius = 1.2f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float flyAroundDuration = 0.6f;
    [SerializeField] private float flyStartRadius = 4f;
    
    [Header("Sword Settings")]
    [SerializeField] private int initialSwordCount = 0;
    [SerializeField] private SwordType currentSwordType = SwordType.Default;
    
    private readonly List<Sword> swords = new();
    private const float TWO_PI = Mathf.PI * 2f;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;
    
    public float RotateSpeed => rotateSpeed;
    public float Radius => radius;
    public int SwordCount => swords.Count;
    public CharacterBase Owner => owner;

    public void IncreaseRadius(float amount)
    {
        radius += amount;
        int count = swords.Count;
        for (int i = 0; i < count; i++)
        {
            Sword s = swords[i];
            if (s.State == SwordState.Orbiting)
                PlaceSword(s.transform, s.CurrentAngle);
            else if (s.State == SwordState.FlyingIn)
                s.UpdateFlyOrbitRadius(radius);
            else if (s.State == SwordState.Sliding)
                s.UpdateSlideRadius(radius);
        }
    }

    public void SetSwordType(SwordType type)
    {
        if (currentSwordType == type) return;

        currentSwordType = type;
        int count = swords.Count;
        for (int i = 0; i < count; i++)
            swords[i].SetSwordType(type);
    }

    private void Start()
    {
        if (initialSwordCount <= 0) return;

        float step = TWO_PI / initialSwordCount;
        for (int i = 0; i < initialSwordCount; i++)
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            Sword sword = ItemManager.Instance.Spawn(pos, rot);
            
            if (sword != null)
            {
                ItemManager.Instance.Unregister(sword);
                
                float angle = step * i;
                sword.transform.SetParent(transform);
                sword.AttachToOrbit(this);
                sword.SetOrbiting();
                sword.CurrentAngle = angle;
                PlaceSword(sword.transform, angle);
                swords.Add(sword);
            }
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
        int count = swords.Count - 1;

        for (int i = 0; i < count; i++)
        {
            Sword s = swords[i];
            float target = step * i;

            if (s.State == SwordState.FlyingIn)
                s.UpdateFlyTarget(target);
            else if (s.State == SwordState.Sliding)
                s.UpdateSlideTarget(target, radius);
            else
                s.StartSlide(s.CurrentAngle, target, radius);
        }

        float startAngle = Mathf.Atan2(t.localPosition.y, t.localPosition.x);
        float targetAngle = step * count;
        sword.StartFlyIn(startAngle, targetAngle, flyStartRadius, radius, flyAroundDuration);
    }

    public void RemoveSword(Sword sword) => swords.Remove(sword);

    public void DropSword(int index)
    {
        if (index >= 0 && index < swords.Count)
            swords[index].KnockOff();
    }

    private void PlaceSword(Transform sw, float angle)
    {
        sw.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        sw.localRotation = Quaternion.Euler(0f, 0f, angle * RAD_TO_DEG - 90f);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}
