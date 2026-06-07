using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// maskImage trượt theo 3 vị trí: startPos → centerPos (dừng) → endPos.
/// logoImage counter-move để giữ nguyên vị trí thế giới.
///
/// Hierarchy:
///   MaskContainer  [Image + Mask(showMaskGraphic=false) + IconRevealEffect]
///       └── LogoImage  [Image]
/// </summary>
public class IconRevealEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform  maskImage;
    [SerializeField] private RectTransform  logoImage;
    [SerializeField] private Text playerNameText;

    [Header("Mask Positions (anchoredPosition)")]
    [SerializeField] private Vector2 maskStartPos;   // vị trí ban đầu  (trước khi trượt vào)
    [SerializeField] private Vector2 maskCenterPos;  // vị trí giữa     (dừng lại)
    [SerializeField] private Vector2 maskEndPos;     // vị trí sau      (trượt ra)

    [Header("Settings")]
    [SerializeField] private float duration         = 0.55f;
    [SerializeField] private float pauseDuration    = 1.5f;
    [SerializeField] private Ease  slideEase        = Ease.OutExpo;
    [SerializeField] private bool  autoPlayOnEnable = true;

    public float TotalDuration => duration * 2f + pauseDuration;

    private Vector2 logoOrigPos;
    private Tween   currentTween;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        maskImage ??= GetComponent<RectTransform>();
        if (logoImage == null && transform.childCount > 0)
            logoImage = transform.GetChild(0) as RectTransform;

        var mask = GetComponent<Mask>() ?? gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Cache vị trí thiết kế của logo (không đổi trong suốt vòng đời)
        logoOrigPos = logoImage != null ? logoImage.anchoredPosition : Vector2.zero;

        // Đặt mask về vị trí ban đầu
        maskImage.anchoredPosition = maskStartPos;
        if (logoImage) logoImage.anchoredPosition = LogoPosAt(maskStartPos);
    }

    private void OnEnable()
    {
        if (autoPlayOnEnable) Play();
    }

    private void OnDisable() => currentTween?.Kill();

    // ─────────────────────────────────────────────────────────────────
    public void SetLevelSprite(Sprite sprite)
    {
        var img = logoImage?.GetComponent<Image>();
        if (img) img.sprite = sprite;
    }

    /// <summary>Gọi sau khi đổi anchoredPosition root từ bên ngoài để reset về startPos.</summary>
    public void RecachePosition()
    {
        maskImage.anchoredPosition = maskStartPos;
        if (logoImage) logoImage.anchoredPosition = LogoPosAt(maskStartPos);
    }

    // ─────────────────────────────────────────────────────────────────
    public void Play(string playerName, System.Action onComplete = null)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        Play(onComplete);
    }

    public void Play(System.Action onComplete = null)
    {
        if (maskImage == null) return;
        currentTween?.Kill();

        maskImage.anchoredPosition = maskStartPos;
        if (logoImage) logoImage.anchoredPosition = LogoPosAt(maskStartPos);

        var seq = DOTween.Sequence();

        // Phase 1: trượt vào → giữa
        seq.Join(maskImage.DOAnchorPos(maskCenterPos, duration).SetEase(slideEase));
        if (logoImage)
            seq.Join(logoImage.DOAnchorPos(LogoPosAt(maskCenterPos), duration).SetEase(slideEase));

        // Phase 2: dừng tại chỗ
        seq.AppendInterval(pauseDuration);

        // Phase 3: trượt qua → biến mất
        seq.Append(maskImage.DOAnchorPos(maskEndPos, duration).SetEase(slideEase));
        if (logoImage)
            seq.Join(logoImage.DOAnchorPos(LogoPosAt(maskEndPos), duration).SetEase(slideEase));

        if (onComplete != null)
            seq.OnComplete(() => onComplete());

        currentTween = seq;
    }

    public void Hide(float hideDuration = 0.2f)
    {
        currentTween?.Kill();
        var seq = DOTween.Sequence();
        seq.Join(maskImage.DOAnchorPos(maskStartPos, hideDuration).SetEase(Ease.InBack));
        if (logoImage)
            seq.Join(logoImage.DOAnchorPos(LogoPosAt(maskStartPos), hideDuration).SetEase(Ease.InBack));
        currentTween = seq;
    }

    public void ResetInstant()
    {
        currentTween?.Kill();
        maskImage.anchoredPosition = maskStartPos;
        if (logoImage) logoImage.anchoredPosition = LogoPosAt(maskStartPos);
    }

    // ─────────────────────────────────────────────────────────────────
    // Counter-move: logo dịch ngược để giữ vị trí thế giới không đổi
    private Vector2 LogoPosAt(Vector2 maskPos)
        => logoOrigPos + (maskCenterPos - maskPos);

    // ─────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("▶ Preview Play")]
    private void PreviewPlay()  { if (Application.isPlaying) Play(); }
    [ContextMenu("■ Preview Reset")]
    private void PreviewReset() { if (Application.isPlaying) ResetInstant(); }
#endif
}
