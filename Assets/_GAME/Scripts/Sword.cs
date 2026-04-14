using UnityEngine;
using DG.Tweening;

public enum SwordState { Dropped, Orbiting, Animating, FlyingIn, Sliding }

public class Sword : MonoBehaviour, ICollectibleItem
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float knockForce = 6f;
    [SerializeField] private float fallDuration = 0.8f;
    [SerializeField] private GameObject collisionParticle;
    [SerializeField] private SwordDataSO swordData;
    [SerializeField] private float damage = 10f;

    private SwordOrbit orbit;
    private SwordState state = SwordState.Dropped;
    private float currentAngle;
    private SwordType swordType = SwordType.Default;

    public SwordType SwordType => swordType;

    public void SetSwordType(SwordType type)
    {
        swordType = type;
        if (spriteRenderer != null && swordData != null)
        {
            Sprite sprite = swordData.GetSprite(type);
            if (sprite != null) spriteRenderer.sprite = sprite;
        }
    }

    private float flyStartAngle, flyTargetAngle, flyStartRadius, flyOrbitRadius;
    private float flyDuration, flyInvDuration, flyElapsed;

    private float slideFromAngle, slideDiff, slideTargetAngle, slideRadius;
    private float slideDuration, slideInvDuration, slideElapsed;

    private const float TWO_PI = Mathf.PI * 2f;
    private const float PI = Mathf.PI;
    private const float RAD_TO_DEG = Mathf.Rad2Deg;

    // ICollectibleItem implementation
    public Vector3 Position => transform.position;
    public bool IsActive => gameObject.activeSelf;
    public new GameObject GameObject => gameObject;
    public CollectibleItemType ItemType => CollectibleItemType.Sword;

    public void OnSpawn(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
        state = SwordState.Dropped;
        LogSwordCell("Spawned");
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
    }

    public SwordOrbit Orbit => orbit;
    public SwordState State => state;
    public float CurrentAngle { get => currentAngle; set => currentAngle = value; }

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

    private void Update()
    {
        if (state == SwordState.FlyingIn) UpdateFlyIn();
        else if (state == SwordState.Sliding) UpdateSlide();
    }

    private void UpdateFlyIn()
    {
        flyElapsed += Time.deltaTime;
        float p = flyElapsed * flyInvDuration;
        if (p > 1f) p = 1f;

        float sweep = (flyTargetAngle - flyStartAngle) % TWO_PI;
        if (sweep < 0f) sweep += TWO_PI;
        if (sweep < PI) sweep += TWO_PI;

        float angle = flyStartAngle + sweep * p;
        float s = Smooth(p);
        float r = flyStartRadius + (flyOrbitRadius - flyStartRadius) * s;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        transform.localPosition = new Vector3(cos * r, sin * r, -0.2f);
        float deg = angle * RAD_TO_DEG;
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(deg + 180f, deg - 90f, s));

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
            float angle = slideFromAngle + slideDiff * Smooth(slideElapsed * slideInvDuration);
            currentAngle = angle;
            PlaceAt(angle, slideRadius);
        }
    }

    private void PlaceAt(float a, float r)
    {
        transform.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, a * RAD_TO_DEG - 90f);
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

        if (state == SwordState.Dropped)
        {
            CharacterBase player = other.GetComponent<CharacterBase>();
            if (player == null) return;
            state = SwordState.Animating;
            if (ItemManager.Instance != null)
                ItemManager.Instance.DeregisterDroppedSword(this);
            player.GetSwordOrbit().AddSword(this);
            return;
        }

        if (state != SwordState.Orbiting && state != SwordState.Sliding) return;
        if (orbit == null) return;

        // kiếm đang xoay chạm vào Player khác (không phải chủ)
        CharacterBase hitPlayer = other.GetComponent<CharacterBase>();
        if (hitPlayer != null && hitPlayer.GetSwordOrbit() != orbit)
        {
            hitPlayer.TakeDamage(damage);
            SpawnParticle(other.ClosestPoint(transform.position));
            return;
        }

        // kiếm chạm kiếm đối thủ → knock off cả hai
        Sword otherSword = other.GetComponent<Sword>();
        if (otherSword == null) return;
        if (otherSword.state != SwordState.Orbiting && otherSword.state != SwordState.Sliding) return;
        if (otherSword.orbit == null || otherSword.orbit == orbit) return;

        SpawnParticle((transform.position + otherSword.transform.position) * 0.5f);
        KnockOff();
        otherSword.KnockOff();
    }

    public void KnockOff()
    {
        if (orbit == null) return;

        state = SwordState.Animating;
        transform.DOKill();

        SwordOrbit owner = orbit;
        owner.RemoveSword(this);
        orbit = null;

        Vector3 worldPos = transform.position;
        Vector2 radial = ((Vector2)(worldPos - owner.transform.position)).normalized;
        if (radial == Vector2.zero) radial = Random.insideUnitCircle.normalized;

        float sign = owner.RotateSpeed >= 0 ? -1f : 1f;
        Vector2 dir = new Vector2(-radial.y, radial.x) * sign;

        transform.SetParent(null);
        Vector3 landPos = worldPos + (Vector3)(dir.normalized * knockForce);
        landPos.z = 1f;

        var seq = DOTween.Sequence();
        seq.Join(transform.DOMove(landPos, fallDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(0.7f, fallDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DORotate(new Vector3(0f, 0f, Random.Range(0f, 360f)), fallDuration, RotateMode.FastBeyond360));
        seq.OnComplete(() =>
        {
            state = SwordState.Dropped;
            LogSwordCell("Dropped");
            if (ItemManager.Instance != null)
                ItemManager.Instance.RegisterDroppedSword(this);
        });
    }

    private void SpawnParticle(Vector3 pos)
    {
        if (collisionParticle != null) Instantiate(collisionParticle, pos, Quaternion.identity);
    }

    private void LogSwordCell(string action)
    {
        MapManager map = MapManager.Instance;
        if (map == null) return;
        Vector2Int cell = map.WorldToCell(transform.position);
        Debug.Log($"[Sword] {action} — {swordType} at row {cell.y}, col {cell.x} (world: {transform.position.x:F1}, {transform.position.y:F1})");
    }
}
