using UnityEngine;
using DG.Tweening;

public enum SwordState { Dropped, Orbiting, Animating }

public class Sword : MonoBehaviour
{
    [SerializeField] private float knockForce = 6f;
    [SerializeField] private float fallDuration = 0.8f;
    [SerializeField] private GameObject collisionParticle;

    private SwordOrbit orbit;
    private SwordState state = SwordState.Dropped;
    private float currentAngle;

    public SwordOrbit Orbit => orbit;
    public SwordState State => state;
    public float CurrentAngle { get => currentAngle; set => currentAngle = value; }

    public void AttachToOrbit(SwordOrbit newOrbit)
    {
        orbit = newOrbit;
        state = SwordState.Animating;
    }

    public void SetOrbiting() => state = SwordState.Orbiting;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (state == SwordState.Animating) return;

        if (state == SwordState.Dropped)
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                state = SwordState.Animating;
                player.GetSwordOrbit().AddSword(this);
            }
            return;
        }

        if (state == SwordState.Orbiting && orbit != null && orbit.IsPlayer)
        {
            Sword otherSword = other.GetComponent<Sword>();
            if (otherSword == null || otherSword.state != SwordState.Orbiting) return;
            if (otherSword.orbit == null || otherSword.orbit.IsPlayer) return;
            
            Vector3 collisionPoint = (transform.position + otherSword.transform.position) * 0.5f;
            SpawnCollisionParticle(collisionPoint);
            
            KnockOff();
            otherSword.KnockOff();
        }
    }

    public void KnockOff()
    {
        if (orbit == null) return;

        state = SwordState.Animating;
        transform.DOKill();

        SwordOrbit ownerOrbit = orbit;
        ownerOrbit.RemoveSword(this);
        orbit = null;

        Vector3 worldPos = transform.position;
        Vector3 center = ownerOrbit.transform.position;
        Vector2 radial = ((Vector2)(worldPos - center)).normalized;
        if (radial == Vector2.zero) radial = Random.insideUnitCircle.normalized;

        float sign = ownerOrbit.RotateSpeed >= 0 ? -1f : 1f;
        Vector2 tangent = new Vector2(-radial.y, radial.x) * sign;
        Vector2 dir = tangent.normalized;

        transform.SetParent(null);
        Vector3 landPos = worldPos + (Vector3)(dir * knockForce);
        landPos.z = 1f;

        var seq = DOTween.Sequence();
        seq.Join(transform.DOMove(landPos, fallDuration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(0.7f, fallDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DORotate(new Vector3(0f, 0f, Random.Range(0f, 360f)), fallDuration, RotateMode.FastBeyond360));
        seq.OnComplete(() => state = SwordState.Dropped);
    }

    private void SpawnCollisionParticle(Vector3 position)
    {
        if (collisionParticle == null) return;
        Instantiate(collisionParticle, position, Quaternion.identity);
    }
}
