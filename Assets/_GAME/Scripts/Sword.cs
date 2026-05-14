using UnityEngine;
using DG.Tweening;

public enum SwordState { Dropped, Orbiting, Animating, FlyingIn, Sliding }

public class Sword : GameUnit
{
    [Header("Sword Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SwordDataSO swordData;
    [SerializeField] private SwordType swordType = SwordType.kiem1;

    [Header("Combat Mode")]
    public static bool DestroyOnZeroHp = true;

    public static void SetDestroyMode(bool destroyMode) => DestroyOnZeroHp = destroyMode;
    public static void ToggleDestroyMode() => DestroyOnZeroHp = !DestroyOnZeroHp;

    private const float defaultMaxHp = 100f;
    private const float defaultDamage = 15f;
    private const float TWO_PI = Mathf.PI * 2f;
    private const float PI = Mathf.PI;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;

    private float knockForce = 6f;
    private float fallDuration = 0.6f;
    private SwordOrbit orbit;
    private SwordState state = SwordState.Dropped;
    private float currentAngle;
    private float currentHp;
    private float maxHp;
    private float damage;

    private float flyStartAngle, flyTargetAngle, flyStartRadius, flyOrbitRadius;
    private float flyDuration, flyInvDuration, flyElapsed;
    private float slideFromAngle, slideDiff, slideTargetAngle, slideRadius;
    private float slideDuration, slideInvDuration, slideElapsed;

    private int lastDamageFrame = -1;

    private static readonly float[] searchAnglesRad;
    static Sword()
    {
        searchAnglesRad = new float[8];
        for (int i = 0; i < 8; i++)
            searchAnglesRad[i] = i * 45f * Mathf.Deg2Rad;
    }

    public SwordType SwordType => swordType;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float HpRatio => maxHp > 0f ? currentHp / maxHp : 0f;
    public SwordOrbit Orbit => orbit;
    public SwordState State => state;
    public float CurrentAngle { get => currentAngle; set => currentAngle = value; }

    private void Update()
    {
        if (state == SwordState.FlyingIn) UpdateFlyIn();
        else if (state == SwordState.Sliding) UpdateSlide();
    }

    public void OnInit()
    {
        maxHp = swordData != null ? swordData.GetMaxHp(swordType) : defaultMaxHp;
        damage = swordData != null ? swordData.GetDamage(swordType) : defaultDamage;
        currentHp = maxHp;
        state = SwordState.Dropped;
        orbit = null;
        lastDamageFrame = -1;

        TF.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        TF.localScale = Vector3.one;

        Vector3 pos = TF.position;
        pos.y = 0f;
        TF.position = pos;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    public void OnDespawn()
    {
        TF.DOKill();
        state = SwordState.Dropped;
        orbit = null;
        currentHp = maxHp;
        lastDamageFrame = -1;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        ItemManager.Instance.Despawn(this);
    }

    public void SetSwordType(SwordType type)
    {
        swordType = type;

        if (spriteRenderer != null && swordData != null)
        {
            Sprite sprite = swordData.GetSprite(type);
            if (sprite != null) spriteRenderer.sprite = sprite;
        }

        maxHp = swordData != null ? swordData.GetMaxHp(type) : defaultMaxHp;
        damage = swordData != null ? swordData.GetDamage(type) : defaultDamage;
        currentHp = maxHp;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    public bool Collect(CharacterBase collector)
    {
        if (state != SwordState.Dropped || collector == null) return false;
        SwordOrbit targetOrbit = collector.GetSwordOrbit();
        if (targetOrbit == null || collector.IsSwordFull || collector.SwordQueue > 0) return false;

        state = SwordState.Animating;
        ItemManager.Instance?.Unregister(this);
        targetOrbit.AddSword(this);
        return true;
    }

    public bool CollectFromQueue(CharacterBase collector)
    {
        if (state != SwordState.Dropped || collector == null) return false;
        SwordOrbit targetOrbit = collector.GetSwordOrbit();
        if (targetOrbit == null || collector.IsSwordFull) return false;

        state = SwordState.Animating;
        ItemManager.Instance?.Unregister(this);
        targetOrbit.AddSword(this);
        return true;
    }

    public void AttachToOrbit(SwordOrbit newOrbit)
    {
        orbit = newOrbit;
        state = SwordState.Animating;
    }

    public void SetOrbiting() => state = SwordState.Orbiting;

    public void StartFlyIn(float startAngle, float targetAngle, float startRadius, float orbitRadius, float duration)
    {
        state = SwordState.FlyingIn;
        flyStartAngle = startAngle;
        flyTargetAngle = targetAngle;
        flyStartRadius = startRadius;
        flyOrbitRadius = orbitRadius;
        flyDuration = duration;
        flyInvDuration = 1f / duration;
        flyElapsed = 0f;
    }

    public void UpdateFlyTarget(float newTarget) => flyTargetAngle = newTarget;
    public void UpdateFlyOrbitRadius(float newRadius) => flyOrbitRadius = newRadius;
    public void UpdateSlideRadius(float newRadius) => slideRadius = newRadius;

    public void StartSlide(float fromAngle, float targetAngle, float radius)
    {
        float diff = targetAngle - fromAngle;
        while (diff < -PI) diff += TWO_PI;
        while (diff > PI) diff -= TWO_PI;

        if (Mathf.Abs(diff) < 0.01f)
        {
            currentAngle = targetAngle;
            PlaceAt(targetAngle, radius);
            return;
        }

        state = SwordState.Sliding;
        slideFromAngle = fromAngle;
        slideDiff = diff;
        slideTargetAngle = targetAngle;
        slideRadius = radius;
        slideDuration = Mathf.Max(0.4f, Mathf.Abs(diff) * RAD_TO_DEG / 360f);
        slideInvDuration = 1f / slideDuration;
        slideElapsed = 0f;
    }

    public void UpdateSlideTarget(float newTarget, float radius)
    {
        StartSlide(slideFromAngle + slideDiff * Smooth(slideElapsed * slideInvDuration), newTarget, radius);
    }

    private void UpdateFlyIn()
    {
        flyElapsed += Time.deltaTime;
        float p = Mathf.Min(flyElapsed * flyInvDuration, 1f);

        float sweep = (flyTargetAngle - flyStartAngle) % TWO_PI;
        if (sweep < 0f) sweep += TWO_PI;
        if (sweep < PI) sweep += TWO_PI;

        float angle = flyStartAngle + sweep * p;
        float s = Smooth(p);
        float r = flyStartRadius + (flyOrbitRadius - flyStartRadius) * s;

        TF.localPosition = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
        TF.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(angle * RAD_TO_DEG + 180f, angle * RAD_TO_DEG - 90f, s));

        if (flyElapsed >= flyDuration)
        {
            currentAngle = flyTargetAngle;
            state = SwordState.Orbiting;
            PlaceAt(flyTargetAngle, flyOrbitRadius);
        }
    }

    private void UpdateSlide()
    {
        slideElapsed += Time.deltaTime;
        if (slideElapsed >= slideDuration)
        {
            currentAngle = slideTargetAngle;
            state = SwordState.Orbiting;
            PlaceAt(slideTargetAngle, slideRadius);
        }
        else
        {
            currentAngle = slideFromAngle + slideDiff * Smooth(slideElapsed * slideInvDuration);
            PlaceAt(currentAngle, slideRadius);
        }
    }

    private void PlaceAt(float a, float r)
    {
        TF.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        TF.localRotation = Quaternion.Euler(0f, 0f, a * RAD_TO_DEG - 90f);
    }

    private static float Smooth(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return t * t * (3f - 2f * t);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (state == SwordState.Animating || state == SwordState.FlyingIn) return;

        CharacterBase character = other.GetComponent<CharacterBase>();

        if (state == SwordState.Dropped)
        {
            if (character != null && !character.IsSwordFull && character.SwordQueue <= 0)
            {
                state = SwordState.Animating;
                ItemManager.Instance?.Unregister(this);
                character.GetSwordOrbit().AddSword(this);
            }
            return;
        }

        if ((state != SwordState.Orbiting && state != SwordState.Sliding) || orbit == null) return;

        if (character != null)
        {
            SwordOrbit hitOrbit = character.GetSwordOrbit();
            if (hitOrbit != orbit)
            {
                ParticlePool.Spawn(ParticleType.SwordVsCharacter, other.ClosestPoint(TF.position));
                CharacterBase attacker = orbit.Owner;
                if (character.SwordCount <= 45)
                {
                    character.TakeDamage(damage, attacker);
                    character.OnSwordInteraction(attacker);
                    attacker?.OnLifesteal(damage);
                    attacker?.GetAudioSource()?.PlayAttack();
                }
            }
            return;
        }

        Sword otherSword = other.GetComponent<Sword>();
        if (otherSword == null || otherSword.orbit == null || otherSword.orbit == orbit) return;
        if (otherSword.state != SwordState.Orbiting && otherSword.state != SwordState.Sliding) return;
        if (GetInstanceID() > otherSword.GetInstanceID()) return;

        ParticlePool.Spawn(ParticleType.SwordVsSword, (TF.position + otherSword.TF.position) * 0.5f);

        CharacterBase myOwner = orbit.Owner;
        CharacterBase otherOwner = otherSword.orbit.Owner;
        float damageDealtToSword = Mathf.Min(otherSword.CurrentHp, damage);

        TakeDamage(otherSword.damage, otherSword);
        myOwner?.GetAudioSource()?.PlayAttack();
        otherSword.TakeDamage(damage, this);

        if (damageDealtToSword > 0f) myOwner?.OnLifesteal(damageDealtToSword);

        if (myOwner != null && otherOwner != null)
        {
            myOwner.OnSwordToSwordKnockback(otherOwner);
            otherOwner.OnSwordToSwordKnockback(myOwner);
        }
    }

    public void TakeDamage(float dmg, Sword attackerSword = null)
    {
        if (orbit != null && orbit.Owner != null && orbit.Owner.IsShieldActive) return;

        int currentFrame = Time.frameCount;
        if (lastDamageFrame == currentFrame) return;
        lastDamageFrame = currentFrame;

        if (orbit != null && !orbit.CanDropSword()) return;

        currentHp -= dmg;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(Color.white, Color.red, 1f - HpRatio);

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            if (DestroyOnZeroHp) DestroySword();
            else KnockOff();
        }
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    public void DestroySword()
    {
        if (orbit == null) return;
        if (orbit.Owner != null && orbit.Owner.IsShieldActive) return;

        state = SwordState.Animating;
        TF.DOKill();

        SwordOrbit owner = orbit;
        owner.RemoveSword(this);
        orbit = null;

        ParticlePool.Spawn(ParticleType.SwordVsSword, TF.position);
        OnDespawn();
    }

    public void KnockOff()
    {
        if (orbit == null) return;
        if (orbit.Owner != null && orbit.Owner.IsShieldActive) return;

        state = SwordState.Animating;
        TF.DOKill();

        SwordOrbit owner = orbit;
        owner.RemoveSword(this);
        orbit = null;

        Vector3 worldPos = TF.position;
        Vector3 diff = worldPos - owner.transform.position;
        Vector3 radial = new Vector3(diff.x, 0f, diff.z);
        if (radial.sqrMagnitude < 0.001f)
        {
            float a = Random.Range(0f, TWO_PI);
            radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
        }
        radial.Normalize();

        float sign = owner.RotateSpeed >= 0f ? -1f : 1f;
        Vector3 dir = new Vector3(-radial.z, 0f, radial.x) * sign;

        TF.SetParent(null);

        Vector3 landPos = worldPos;
        landPos.y = 0f;

        MapManager map = MapManager.Instance;
        if (map != null)
        {
            float stepSize = map.CellSize * 0.5f;
            Vector3 safeLand = worldPos;
            safeLand.y = 0f;

            for (float d = stepSize; d <= knockForce; d += stepSize)
            {
                Vector3 check = worldPos + dir * d;
                check.y = 0f;
                check = map.ClampToMap(check);
                if (map.IsWall(check)) break;
                safeLand = check;
            }

            if (map.IsWall(safeLand))
                safeLand = FindNearestOpenPosition(worldPos, map);

            landPos = safeLand;
            landPos.y = 0f;
        }

        var seq = DOTween.Sequence();
        seq.Join(TF.DOMove(landPos, fallDuration).SetEase(Ease.OutQuad));
        seq.Join(TF.DOScale(1f, fallDuration).SetEase(Ease.InQuad));
        seq.Join(TF.DORotate(new Vector3(90f, Random.Range(0f, 360f), 0f), fallDuration, RotateMode.FastBeyond360));
        seq.OnComplete(() =>
        {
            state = SwordState.Dropped;
            currentHp = maxHp;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            SetSwordType(SwordType.kiem1);

            Vector3 finalPos = TF.position;
            finalPos.y = 0f;
            TF.position = finalPos;
            TF.localScale = Vector3.one;

            ItemManager.Instance?.Register(this);
        });
    }

    private Vector3 FindNearestOpenPosition(Vector3 center, MapManager map)
    {
        float cellSize = map.CellSize;
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 checkPos = center + new Vector3(Mathf.Cos(searchAnglesRad[i]), 0f, Mathf.Sin(searchAnglesRad[i])) * (cellSize * radius);
                checkPos.y = 0f;
                checkPos = map.ClampToMap(checkPos);
                if (!map.IsWall(checkPos)) return checkPos;
            }
        }
        return map.ClampToMap(center);
    }
}
