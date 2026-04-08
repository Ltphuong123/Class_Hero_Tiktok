using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SwordOrbit : MonoBehaviour
{
    [SerializeField] private float radius = 1f, rotateSpeed = 180f;

    public float RotateSpeed => rotateSpeed;
    [SerializeField] private float pickupStartRadius = 4f, pickupSpinDuration = 0.6f;
    [SerializeField] private float redistributeDuration = 0.3f;
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private int initialSwordCount = 0;

    private readonly List<Transform> swords = new();
    private readonly List<SpinData> spinList = new();

    // Mỗi kiếm đã trong orbit có góc hiện tại (radian, local space)
    // và có thể đang tween về góc target
    private readonly Dictionary<Transform, float> currentAngles = new();
    private readonly HashSet<Transform> tweening = new();

    private float angleStep;
    private int count;

    public bool IsPlayer { get; set; }

    private struct SpinData
    {
        public Transform tf;
        public float startAngle; // radian, góc ban đầu khi nhặt
        public float elapsed;
    }

    private void Start()
    {
        for (int i = 0; i < initialSwordCount; i++)
        {
            var s = Instantiate(swordPrefab, transform);
            Setup(s);
            s.GetComponent<Sword>()?.SetOrbiting();
            swords.Add(s.transform);
        }
        if (swords.Count > 0)
        {
            Recalc();
            PlaceImmediate();
        }
    }

    private void Recalc()
    {
        count = swords.Count;
        angleStep = count > 0 ? 360f / count : 360f;
    }

    private void PlaceImmediate()
    {
        float stepRad = angleStep * Mathf.Deg2Rad;
        for (int i = 0; i < count; i++)
        {
            float a = stepRad * i;
            swords[i].localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            swords[i].localRotation = Quaternion.Euler(0f, 0f, angleStep * i - 90f);
            currentAngles[swords[i]] = a;
        }
    }

    public void AddSword(GameObject obj)
    {
        Setup(obj);
        obj.transform.SetParent(transform);
        var t = obj.transform;
        t.DOKill();
        t.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        t.DOScale(1f, pickupSpinDuration).SetEase(Ease.OutBack);

        float startAngle = Mathf.Atan2(t.localPosition.y, t.localPosition.x);
        spinList.Add(new SpinData
        {
            tf = t,
            startAngle = startAngle,
            elapsed = 0f
        });
    }

    public void RemoveSword(Transform sword)
    {
        sword.DOKill();
        swords.Remove(sword);
        currentAngles.Remove(sword);
        tweening.Remove(sword);
        for (int i = spinList.Count - 1; i >= 0; i--)
            if (spinList[i].tf == sword) { spinList.RemoveAt(i); break; }
        Recalc();
        Redistribute();
    }

    private void Setup(GameObject obj)
    {
        var s = obj.GetComponent<Sword>() ?? obj.AddComponent<Sword>();
        s.AttachToOrbit(this);
    }

    /// <summary>
    /// Sau khi kiếm mới vào orbit xong, chèn vào list rồi tween tất cả
    /// kiếm về góc mới dọc theo quỹ đạo tròn.
    /// </summary>
    private void FinishSpinIn(Transform tf, float currentAngle)
    {
        // Chèn kiếm mới vào cuối, gán góc hiện tại
        swords.Add(tf);
        currentAngles[tf] = currentAngle;
        Recalc();

        // Snap kiếm mới vào đúng radius (vừa xong spin-in)
        tf.localPosition = new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle), 0f) * radius;
        tf.localRotation = Quaternion.Euler(0f, 0f, currentAngle * Mathf.Rad2Deg - 90f);

        var sword = tf.GetComponent<Sword>();
        if (sword != null) sword.SetOrbiting();

        Redistribute();
    }

    /// <summary>
    /// Tween tất cả kiếm trượt dọc quỹ đạo tròn về vị trí đều nhau.
    /// Chọn hướng ngắn nhất (CW hoặc CCW) cho mỗi kiếm.
    /// </summary>
    private void Redistribute()
    {
        if (count == 0) return;

        // Sắp xếp swords theo góc hiện tại để giữ thứ tự tương đối
        swords.Sort((a, b) =>
        {
            float aa = currentAngles.ContainsKey(a) ? currentAngles[a] : 0f;
            float bb = currentAngles.ContainsKey(b) ? currentAngles[b] : 0f;
            return aa.CompareTo(bb);
        });
        Recalc();

        float stepRad = angleStep * Mathf.Deg2Rad;
        for (int i = 0; i < count; i++)
        {
            var sw = swords[i];
            float targetAngle = stepRad * i;
            float fromAngle = currentAngles.ContainsKey(sw) ? currentAngles[sw] : targetAngle;

            // Nếu đã đúng vị trí thì skip
            float delta = Mathf.DeltaAngle(fromAngle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg);
            if (Mathf.Abs(delta) < 0.1f)
            {
                currentAngles[sw] = targetAngle;
                PlaceSwordAt(sw, targetAngle);
                continue;
            }

            // Kill tween cũ nếu đang chạy
            if (tweening.Contains(sw))
            {
                sw.DOKill();
                tweening.Remove(sw);
            }

            TweenSwordToAngle(sw, fromAngle, targetAngle);
        }
    }

    private void TweenSwordToAngle(Transform sw, float fromAngle, float targetAngle)
    {
        tweening.Add(sw);
        float fromDeg = fromAngle * Mathf.Rad2Deg;
        float toDeg = targetAngle * Mathf.Rad2Deg;

        // Dùng DOTween để tween góc, di chuyển dọc quỹ đạo tròn
        DOTween.To(
            () => fromDeg,
            val =>
            {
                float rad = val * Mathf.Deg2Rad;
                currentAngles[sw] = rad;
                PlaceSwordAt(sw, rad);
            },
            toDeg,
            redistributeDuration
        )
        .SetEase(Ease.InOutSine)
        .SetTarget(sw)
        .OnComplete(() => tweening.Remove(sw));
    }

    private void PlaceSwordAt(Transform sw, float angleRad)
    {
        sw.localPosition = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * radius;
        sw.localRotation = Quaternion.Euler(0f, 0f, angleRad * Mathf.Rad2Deg - 90f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        transform.Rotate(0f, 0f, rotateSpeed * dt);

        // Xử lý spin-in cho kiếm mới
        float inv = 1f / pickupSpinDuration;
        float twoPi = Mathf.PI * 2f;

        for (int i = spinList.Count - 1; i >= 0; i--)
        {
            var d = spinList[i];
            d.elapsed += dt;
            float progress = Mathf.Clamp01(d.elapsed * inv);

            if (d.elapsed >= pickupSpinDuration)
            {
                // Spin-in xong → chèn vào orbit và redistribute
                spinList.RemoveAt(i);
                float finalAngle = d.startAngle + twoPi; // đã xoay đủ 1 vòng
                // Normalize về [0, 2PI)
                finalAngle = finalAngle % twoPi;
                if (finalAngle < 0f) finalAngle += twoPi;
                FinishSpinIn(d.tf, finalAngle);
            }
            else
            {
                // Xoay 1 vòng + thu bán kính từ pickupStartRadius → radius
                float a = d.startAngle + twoPi * progress;

                // Smoothstep cho bán kính
                float blend = progress * progress * (3f - 2f * progress);
                float currentRadius = Mathf.Lerp(pickupStartRadius, radius, blend);

                float rotZ = (a + Mathf.PI) * Mathf.Rad2Deg;
                // Nửa sau: bắt đầu xoay hướng kiếm về hướng orbit (-90°)
                if (progress > 0.5f)
                {
                    float orientBlend = (progress - 0.5f) / 0.5f;
                    orientBlend = orientBlend * orientBlend * (3f - 2f * orientBlend);
                    rotZ = Mathf.LerpAngle(rotZ, a * Mathf.Rad2Deg - 90f, orientBlend);
                }

                d.tf.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * currentRadius;
                d.tf.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                spinList[i] = d;
            }
        }
    }
}
