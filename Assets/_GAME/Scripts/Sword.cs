using UnityEngine;
using DG.Tweening;

public enum SwordState { Dropped, Orbiting, Animating, FlyingIn, Sliding }

public class Sword : GameUnit
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SwordDataSO swordData;
    [SerializeField] private SwordType swordType = SwordType.Default;
    
    private const float defaultMaxHp = 100f;
    private const float defaultDamage = 15f;
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

    private const float TWO_PI = Mathf.PI * 2f;
    private const float PI = Mathf.PI;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;

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
        if (swordData != null)
        {
            maxHp = swordData.GetMaxHp(swordType);
            damage = swordData.GetDamage(swordType);
        }
        else
        {
            maxHp = defaultMaxHp;
            damage = defaultDamage;
        }
        currentHp = maxHp;
        state = SwordState.Dropped;
        orbit = null;
        
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    public void OnDespawn()
    {
        TF.DOKill();
        state = SwordState.Dropped;
        orbit = null;
        currentHp = maxHp;
        
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        
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

        if (swordData != null)
        {
            maxHp = swordData.GetMaxHp(type);
            damage = swordData.GetDamage(type);
        }
        else
        {
            maxHp = defaultMaxHp;
            damage = defaultDamage;
        }

        currentHp = maxHp;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    public bool Collect(CharacterBase collector)
    {
        if (state != SwordState.Dropped || collector == null) return false;

        SwordOrbit targetOrbit = collector.GetSwordOrbit();
        if (targetOrbit == null) return false;

        state = SwordState.Animating;
        ItemManager.Instance?.Unregister(this);
        
        currentHp = maxHp;
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

        TF.localPosition = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, -0.2f);
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (state == SwordState.Animating || state == SwordState.FlyingIn) return;

        CharacterBase character = other.GetComponent<CharacterBase>();

        if (state == SwordState.Dropped)
        {
            if (character != null)
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
                CharacterBase attacker = orbit.Owner;
                character.TakeDamage(damage, attacker);
                character.OnSwordInteraction(attacker);
            }
            return;
        }


        Sword otherSword = other.GetComponent<Sword>();
        
        if (otherSword == null || otherSword.orbit == null || otherSword.orbit == orbit) return;
        if (otherSword.state != SwordState.Orbiting && otherSword.state != SwordState.Sliding) return;
        if (GetInstanceID() > otherSword.GetInstanceID()) return;

        TakeDamage(otherSword.damage, otherSword);
        otherSword.TakeDamage(damage, this);

        CharacterBase myOwner = orbit.Owner;
        CharacterBase otherOwner = otherSword.orbit.Owner;

        if (myOwner != null && otherOwner != null)
        {
            myOwner.OnSwordInteraction(otherOwner);
            otherOwner.OnSwordInteraction(myOwner);
        }
    }

    public void TakeDamage(float dmg, Sword attackerSword = null)
    {
        currentHp -= dmg;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(Color.white, Color.red, 1f - HpRatio);

        if (orbit != null && attackerSword != null && attackerSword.orbit != null)
        {
            CharacterBase owner = orbit.Owner;
            CharacterBase attacker = attackerSword.orbit.Owner;
            
            if (owner != null && attacker != null)
                owner.GetStateMachine()?.OnUnderAttack(attacker);
        }

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            KnockOff();
        }
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    public void KnockOff()
    {
        if (orbit == null) return;

        state = SwordState.Animating;
        TF.DOKill();

        SwordOrbit owner = orbit;
        owner.RemoveSword(this);
        orbit = null;

        Vector3 worldPos = TF.position;
        Vector2 radial = ((Vector2)(worldPos - owner.transform.position)).normalized;
        if (radial == Vector2.zero) radial = Random.insideUnitCircle.normalized;

        float sign = owner.RotateSpeed >= 0 ? -1f : 1f;
        Vector2 dirNormalized = new Vector2(-radial.y, radial.x) * sign;

        TF.SetParent(null);
        
        Vector3 landPos = worldPos + (Vector3)(dirNormalized * knockForce);
        landPos.z = 1f;

        MapManager map = MapManager.Instance;
        if (map != null)
        {
            float stepSize = map.CellSize * 0.5f;
            Vector3 safeLand = worldPos;
            safeLand.z = 1f;

            for (float d = stepSize; d <= knockForce; d += stepSize)
            {
                Vector3 check = worldPos + (Vector3)(dirNormalized * d);
                check.z = 1f;
                check = map.ClampToMap(check);

                if (map.IsWall(check)) break;
                safeLand = check;
            }

            landPos = map.IsWall(safeLand) ? worldPos : safeLand;
            landPos.z = 1f;
            landPos = map.ClampToMap(landPos);
        }

        var seq = DOTween.Sequence();
        seq.Join(TF.DOMove(landPos, fallDuration).SetEase(Ease.OutQuad));
        seq.Join(TF.DOScale(0.7f, fallDuration).SetEase(Ease.InQuad));
        seq.Join(TF.DORotate(new Vector3(0f, 0f, Random.Range(0f, 360f)), fallDuration, RotateMode.FastBeyond360));
        seq.OnComplete(() =>
        {
            state = SwordState.Dropped;
            currentHp = maxHp;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            ItemManager.Instance?.Register(this);
        });
    }
}
