using UnityEngine;
using DG.Tweening;

public enum SwordState { Dropped, Orbiting, Animating }

[RequireComponent(typeof(Collider2D))]
public class Sword : MonoBehaviour
{
    [SerializeField] private float knockForce = 6f, fallDuration = 0.8f;

    private SwordOrbit orbit;
    private SwordState state = SwordState.Dropped;

    public SwordOrbit Orbit => orbit;
    public SwordState State => state;

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
            var player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                state = SwordState.Animating;
                player.GetSwordOrbit().AddSword(gameObject);
            }
            return;
        }

        if (state == SwordState.Orbiting && orbit != null && orbit.IsPlayer)
        {
            var otherSword = other.GetComponent<Sword>();
            if (otherSword == null || otherSword.state != SwordState.Orbiting) return;
            if (otherSword.orbit == null || otherSword.orbit.IsPlayer) return;
            KnockOff(this);
            KnockOff(otherSword);
        }
    }

    private static void KnockOff(Sword sword)
    {
        sword.state = SwordState.Animating;
        sword.transform.DOKill();

        SwordOrbit ownerOrbit = sword.orbit;
        ownerOrbit.RemoveSword(sword.transform);
        sword.orbit = null;

        Vector3 worldPos = sword.transform.position;
        Vector3 center = ownerOrbit.transform.position;
        Vector2 radial = ((Vector2)(worldPos - center)).normalized;
        if (radial == Vector2.zero) radial = Random.insideUnitCircle.normalized;

        // tiếp tuyến ngược chiều xoay
        float sign = ownerOrbit.RotateSpeed >= 0 ? -1f : 1f;
        Vector2 tangent = new Vector2(-radial.y, radial.x) * sign;

        // hướng văng = ngược tiếp tuyến (ngược hướng xoay)
        Vector2 dir = tangent.normalized;

        sword.transform.SetParent(null);
        Vector3 landPos = worldPos + (Vector3)(dir * sword.knockForce);
        landPos.z = 0f;

        sword.transform.DOMove(landPos, sword.fallDuration).SetEase(Ease.OutQuad)
            .OnComplete(() => sword.state = SwordState.Dropped);
        sword.transform.DOScale(0.7f, sword.fallDuration).SetEase(Ease.InQuad);
        sword.transform.DORotate(new Vector3(0f, 0f, Random.Range(0f, 360f)), sword.fallDuration, RotateMode.FastBeyond360);
    }
}
