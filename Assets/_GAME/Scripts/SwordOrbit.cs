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
    [SerializeField] private float initialAngle = 0f;

    [Header("Sword Settings")]
    [SerializeField] private int initialSwordCount = 0;
    [SerializeField] private SwordType currentSwordType = SwordType.kiem1;

    private readonly List<Sword> swords = new();
    private const float TWO_PI = Mathf.PI * 2f;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;
    private const float SwordDropCooldown = 0.1f;

    private bool isPaused;
    private float orbitAngle;
    private float lastSwordDropTime = -1f;

    public float RotateSpeed => rotateSpeed;
    public float Radius => radius;
    public int SwordCount => swords.Count;
    public CharacterBase Owner => owner;

    public void OnInit()
    {
        orbitAngle = initialAngle;
        swords.Clear();
        isPaused = false;
        lastSwordDropTime = -1f;
    }

    public void OnDespawn() => swords.Clear();

    public void IncreaseRadius(float amount)
    {
        radius += amount;
        int count = swords.Count;
        for (int i = 0; i < count; i++)
        {
            Sword s = swords[i];
            switch (s.State)
            {
                case SwordState.Orbiting:  PlaceSword(s.transform, s.CurrentAngle); break;
                case SwordState.FlyingIn:  s.UpdateFlyOrbitRadius(radius);          break;
                case SwordState.Sliding:   s.UpdateSlideRadius(radius);             break;
            }
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
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        for (int i = 0; i < initialSwordCount; i++)
        {
            Sword sword = ItemManager.Instance.Spawn(pos, rot);
            if (sword == null) continue;

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

    public void AddSword(Sword sword)
    {
        sword.AttachToOrbit(this);
        sword.SetSwordType(currentSwordType);

        Transform t = sword.transform;
        t.SetParent(transform);
        t.localScale = Vector3.one;

        swords.Add(sword);
        owner?.GetAudioSource()?.PlayCollectSword();

        int total = swords.Count;
        float step = TWO_PI / total;
        int lastIndex = total - 1;

        for (int i = 0; i < lastIndex; i++)
        {
            Sword s = swords[i];
            float target = step * i;
            switch (s.State)
            {
                case SwordState.FlyingIn: s.UpdateFlyTarget(target);             break;
                case SwordState.Sliding:  s.UpdateSlideTarget(target, radius);   break;
                default:                  s.StartSlide(s.CurrentAngle, target, radius); break;
            }
        }

        float startAngle = Mathf.Atan2(t.localPosition.y, t.localPosition.x);
        sword.StartFlyIn(startAngle, step * lastIndex, flyStartRadius, radius, flyAroundDuration);
    }

    public void RemoveSword(Sword sword) => swords.Remove(sword);

    public Sword GetRandomSword()
    {
        if (swords.Count == 0) return null;
        return swords[UnityEngine.Random.Range(0, swords.Count)];
    }

    public void DropSword(int index)
    {
        if ((uint)index < (uint)swords.Count)
            swords[index].KnockOff();
    }

    public void DespawnAllSwords()
    {
        for (int i = swords.Count - 1; i >= 0; i--)
        {
            Sword s = swords[i];
            s.transform.SetParent(null);
            s.OnDespawn();
        }
        swords.Clear();
    }

    private void PlaceSword(Transform sw, float angle)
    {
        sw.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        sw.localRotation = Quaternion.Euler(0f, 0f, angle * RAD_TO_DEG - 90f);
    }

    private void Update()
    {
        if (isPaused) return;
        orbitAngle += rotateSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(-90f, 0f, orbitAngle);
    }

    public void SetPaused(bool paused) => isPaused = paused;
    public void SetRotateSpeed(float speed) => rotateSpeed = speed;

    public void SumNegativeDebuffs(ref float dr, ref float ls, ref float os)
    {
        foreach (var sword in swords)
        {
            if (sword == null) continue;
            sword.GetDebuffValues(out float sdr, out float sls, out float sos);
            dr += sdr; ls += sls; os += sos;
        }
    }

    public bool CanDropSword()
    {
        float t = Time.time;
        if (t - lastSwordDropTime >= SwordDropCooldown)
        {
            lastSwordDropTime = t;
            return true;
        }
        return false;
    }
}
