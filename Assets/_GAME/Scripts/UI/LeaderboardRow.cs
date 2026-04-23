using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class LeaderboardRow : MonoBehaviour, IPointerClickHandler
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI swordCountText;

    [Header("Images")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image levelIcon;
    [SerializeField] private Image levelTimeFill;

    [Header("Level Icons")]
    [SerializeField] private Sprite[] levelSprites;

    [Header("Background Colors")]
    [SerializeField] private Color rank1Color = new Color(1f, 0.84f, 0f, 1f);      // Vàng gold - Mạnh nhất
    [SerializeField] private Color rank2Color = new Color(0.75f, 0.75f, 0.75f, 1f); // Bạc silver
    [SerializeField] private Color rank3Color = new Color(0.8f, 0.5f, 0.2f, 1f);    // Đồng bronze
    [SerializeField] private Color rank4Color = new Color(0.4f, 0.8f, 1f, 1f);      // Xanh dương nhạt
    [SerializeField] private Color rank5Color = new Color(0.6f, 0.9f, 0.6f, 1f);    // Xanh lá nhạt
    [SerializeField] private Color normalRankColor = new Color(1f, 1f, 1f, 1f);     // Trắng
    [SerializeField] private int topRankCount = 5;

    [Header("Camera")]
    [SerializeField] private float cameraMoveSpeed = 0.5f;

    private float cachedHp = -1f;
    private int cachedSwordCount = -1;
    private int cachedLevel = -1;
    private CharacterBase currentCharacter;

    public static event System.Action<CharacterBase> OnRowClicked;

    public void SetData(CharacterRankData data)
    {
        currentCharacter = data.Character;

        if (nameText != null)
            nameText.text = data.Name;

        if (backgroundImage != null)
        {
            Color bgColor = data.Rank switch
            {
                1 => rank1Color,
                2 => rank2Color,
                3 => rank3Color,
                4 => rank4Color,
                5 => rank5Color,
                _ => normalRankColor
            };
            backgroundImage.color = bgColor;
        }

        if (levelIcon != null && cachedLevel != data.Level)
        {
            int index = data.Level - 1;
            if (levelSprites != null && index >= 0 && index < levelSprites.Length)
            {
                levelIcon.sprite = levelSprites[index];
                levelIcon.enabled = true;
            }
            else
            {
                levelIcon.enabled = false;
            }
            cachedLevel = data.Level;
        }

        if (levelTimeFill != null)
        {
            float duration = currentCharacter != null ? currentCharacter.GetLevelDuration() : 0f;
            
            if (duration > 0f && data.LevelTimeRemaining > 0f)
            {
                float ratio = 1 - data.LevelTimeRemaining / duration;
                levelTimeFill.fillAmount = ratio;
                levelTimeFill.enabled = true;
            }
            else
            {
                levelTimeFill.enabled = false;
            }
        }

        if (hpText != null && !Mathf.Approximately(cachedHp, data.CurrentHp))
        {
            hpText.text = $"{data.CurrentHp:F0}/{data.MaxHp:F0}";
            cachedHp = data.CurrentHp;
        }

        if (swordCountText != null && cachedSwordCount != data.SwordCount)
        {
            swordCountText.text = $"{data.SwordCount}";
            cachedSwordCount = data.SwordCount;
        }

        if (avatarImage != null && avatarImage.sprite != data.Avatar)
        {
            avatarImage.enabled = data.Avatar != null;
            if (data.Avatar != null) avatarImage.sprite = data.Avatar;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentCharacter != null)
        {
            OnRowClicked?.Invoke(currentCharacter);
            MoveCameraToCharacter();
        }
    }

    private void MoveCameraToCharacter()
    {
        if (currentCharacter == null) return;

        CameraController camController = Camera.main?.GetComponent<CameraController>();
        if (camController != null)
        {
            // Set target để camera follow character liên tục
            camController.SetTarget(currentCharacter.transform);
        }
        else
        {
            // Fallback: Di chuyển camera một lần nếu không có CameraController
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector3 characterPos = currentCharacter.transform.position;
            Vector3 targetPos = mainCam.transform.position;
            
            targetPos.x = characterPos.x;
            targetPos.y = characterPos.y;
            
            mainCam.transform.DOKill();
            mainCam.transform.DOMove(targetPos, cameraMoveSpeed).SetEase(Ease.OutQuad);
        }
    }
}
