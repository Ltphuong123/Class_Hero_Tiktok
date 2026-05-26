using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class LeaderboardRow : MonoBehaviour, IPointerClickHandler
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField] private Text nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI swordCountText;
    [SerializeField] private TextMeshProUGUI killPointsText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Booster Texts")]
    [SerializeField] private TextMeshProUGUI magnetCountText;
    [SerializeField] private TextMeshProUGUI shieldCountText;
    [Header("Booster Fill Images")]
    [SerializeField] private Image magnetTimeFill;
    [SerializeField] private Image shieldTimeFill;

    private TextMeshProUGUI magnetFillText;
    private TextMeshProUGUI shieldFillText;

    [Header("Images")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image levelIcon;
    [SerializeField] private Image levelTimeFill;
    [SerializeField] private Image deadOverlay;

    [Header("Level Icons")]
    [SerializeField] private Sprite[] levelSprites;

    [Header("Background Colors")]
    [SerializeField] private Color rank1Color = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField] private Color rank2Color = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color rank3Color = new Color(0.8f, 0.5f, 0.2f, 1f);
    [SerializeField] private Color rank4Color = new Color(0.4f, 0.8f, 1f, 1f);
    [SerializeField] private Color rank5Color = new Color(0.6f, 0.9f, 0.6f, 1f);
    [SerializeField] private Color normalRankColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private int topRankCount = 5;

    [Header("Camera")]
    [SerializeField] private float cameraMoveSpeed = 0.5f;

    private float cachedHp = -1f;
    private int cachedSwordCount = -1;
    private int cachedSwordQueue = -1;
    private int cachedLevel = -1;
    private int cachedRank = -1;
    private int cachedKillPoints = -1;
    private int cachedScore = -1;
    private int cachedMagnetStack = -1;
    private bool cachedIsDead = false;
    private int cachedShieldStack = -1;
    private CharacterBase currentCharacter;

    public static event System.Action<CharacterBase> OnRowClicked;

    private void Awake()
    {
        if (magnetTimeFill != null)
            magnetFillText = magnetTimeFill.GetComponentInChildren<TextMeshProUGUI>();
        
        if (shieldTimeFill != null)
            shieldFillText = shieldTimeFill.GetComponentInChildren<TextMeshProUGUI>();
        
    }

    public void SetData(CharacterRankData data)
    {
        currentCharacter = data.Character;

        if (cachedIsDead != data.IsDead)
        {
            cachedIsDead = data.IsDead;
            SetDeadVisuals(data.IsDead);
        }

        if (data.IsDead)
        {
            SetDeadData(data);
            return;
        }

        if (rankText != null && cachedRank != data.Rank)
        {
            rankText.text = $"#{data.Rank}";
            cachedRank = data.Rank;
        }

        if (idText != null)
            idText.text = $"#{data.NumericId}";

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

        if (cachedLevel != data.Level)
        {
            int index = data.Level - 1;
            if (levelSprites != null && index >= 0 && index < levelSprites.Length)
            {
                if (levelIcon != null) { levelIcon.sprite = levelSprites[index]; levelIcon.enabled = true; }
                if (levelTimeFill != null) levelTimeFill.sprite = levelSprites[index];
            }
            else
            {
                if (levelIcon != null) levelIcon.enabled = false;
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
            hpText.text = $"{data.CurrentHp:N0}";
            
            if (cachedHp >= 0f && data.CurrentHp > cachedHp)
            {
                hpText.transform.DOKill();
                hpText.transform.localScale = Vector3.one;
                hpText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
            }
            
            cachedHp = data.CurrentHp;
        }

        if (swordCountText != null && (cachedSwordCount != data.SwordCount || cachedSwordQueue != data.SwordQueue))
        {
            int oldTotal = (cachedSwordCount >= 0 && cachedSwordQueue >= 0) ? (cachedSwordCount + cachedSwordQueue) : -1;
            int totalSwords = data.SwordCount + data.SwordQueue;
            swordCountText.text = $"{totalSwords:N0}";
            
            if (oldTotal >= 0 && totalSwords > oldTotal)
            {
                swordCountText.transform.DOKill();
                swordCountText.transform.localScale = Vector3.one;
                swordCountText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
            }
            
            cachedSwordCount = data.SwordCount;
            cachedSwordQueue = data.SwordQueue;
        }
        
        if (killPointsText != null && cachedKillPoints != data.KillPoints)
        {
            killPointsText.text = $"{data.KillPoints:N0}";

            if (cachedKillPoints >= 0 && data.KillPoints > cachedKillPoints)
            {
                killPointsText.transform.DOKill();
                killPointsText.transform.localScale = Vector3.one;
                killPointsText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
            }

            cachedKillPoints = data.KillPoints;
        }

        if (scoreText != null && cachedScore != data.Score)
        {
            scoreText.text = FormatScore(data.Score);

            if (cachedScore >= 0 && data.Score > cachedScore)
            {
                scoreText.transform.DOKill();
                scoreText.transform.localScale = Vector3.one;
                scoreText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
            }

            cachedScore = data.Score;
        }
        
        if (cachedMagnetStack != data.MagnetStackCount)
        {
            TextMeshProUGUI targetText = magnetCountText != null ? magnetCountText : magnetFillText;
            
            if (targetText != null)
            {
                if (data.MagnetStackCount > 0)
                {
                    targetText.text = $"{data.MagnetStackCount}";
                    targetText.enabled = true;
                }
                else
                {
                    targetText.text = " ";
                    targetText.enabled = true;
                }
            }
            
            cachedMagnetStack = data.MagnetStackCount;
        }
        
        if (magnetTimeFill != null && currentCharacter != null)
        {
            if (currentCharacter.IsMagnetActive && data.MagnetTimeRemaining > 0f)
            {
                float duration = currentCharacter.MagnetDuration;
                float ratio = data.MagnetTimeRemaining / duration;
                magnetTimeFill.fillAmount = 1-ratio;
                magnetTimeFill.enabled = true;
            }
            else
            {
                magnetTimeFill.fillAmount = 1;
            }
        }
        
        if (cachedShieldStack != data.ShieldStackCount)
        {
            TextMeshProUGUI targetText = shieldCountText != null ? shieldCountText : shieldFillText;
            
            if (targetText != null)
            {
                if (data.ShieldStackCount > 0)
                {
                    targetText.text = $"{data.ShieldStackCount}";
                    targetText.enabled = true;
                }
                else
                {
                    targetText.text = " ";
                    targetText.enabled = true;
                }
            }
            
            cachedShieldStack = data.ShieldStackCount;
        }
        
        if (shieldTimeFill != null && currentCharacter != null)
        {
            if (currentCharacter.IsShieldActive && data.ShieldTimeRemaining > 0f)
            {
                float duration = currentCharacter.ShieldDuration;
                float ratio = data.ShieldTimeRemaining / duration;
                shieldTimeFill.fillAmount = 1-ratio;
                shieldTimeFill.enabled = true;
            }
            else
            {
                shieldTimeFill.fillAmount = 1;
            }
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
            camController.SetTarget(currentCharacter.transform);
        }
        else
        {
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

    private static string FormatScore(int value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000f:0.#}M";
        if (value >= 1_000)     return $"{value / 1_000f:0.#}k";
        return value.ToString();
    }

    private void SetDeadVisuals(bool isDead)
    {
        if (deadOverlay != null) deadOverlay.gameObject.SetActive(isDead);

        if (!isDead)
        {
            levelIcon?.gameObject.SetActive(true);
            magnetTimeFill?.gameObject.SetActive(true);
            shieldTimeFill?.gameObject.SetActive(true);
            cachedLevel = -1;
            cachedMagnetStack = -1;
            cachedShieldStack = -1;
        }
    }

    private void SetDeadData(CharacterRankData data)
    {

        if (rankText != null && cachedRank != data.Rank)
        {
            rankText.text = $"#{data.Rank}";
            cachedRank = data.Rank;
        }

        if (idText != null) idText.text = $"#{data.NumericId}";
        if (nameText != null) nameText.text = data.Name;

        if (avatarImage != null)
        {
            avatarImage.enabled = data.Avatar != null;
            if (data.Avatar != null) avatarImage.sprite = data.Avatar;
        }

        // Level — hiển thị sprite level 1 khi chết
        if (levelIcon != null && levelSprites != null && levelSprites.Length > 0)
        {
            levelIcon.sprite = levelSprites[0];
            levelIcon.enabled = true;
            levelIcon.gameObject.SetActive(true);
        }
        if (levelTimeFill != null) levelTimeFill.gameObject.SetActive(false);

        // HP — hiển thị 0
        if (hpText != null && !Mathf.Approximately(cachedHp, data.CurrentHp))
        {
            hpText.text = $"{data.CurrentHp:N0}";
            cachedHp = data.CurrentHp;
        }

        // Sword
        if (swordCountText != null && (cachedSwordCount != data.SwordCount || cachedSwordQueue != data.SwordQueue))
        {
            swordCountText.text = $"{data.SwordCount + data.SwordQueue:N0}";
            cachedSwordCount = data.SwordCount;
            cachedSwordQueue = data.SwordQueue;
        }

        // KillPoints
        if (killPointsText != null && cachedKillPoints != data.KillPoints)
        {
            killPointsText.text = $"{data.KillPoints:N0}";
            cachedKillPoints = data.KillPoints;
        }

        // Score
        if (scoreText != null && cachedScore != data.Score)
        {
            scoreText.text = FormatScore(data.Score);
            cachedScore = data.Score;
        }

        // Booster — ẩn timer fill, chỉ hiện stack count
        if (magnetTimeFill != null) magnetTimeFill.gameObject.SetActive(false);
        if (shieldTimeFill != null) shieldTimeFill.gameObject.SetActive(false);

        if (cachedMagnetStack != data.MagnetStackCount)
        {
            if (magnetCountText != null)
            {
                magnetCountText.text = data.MagnetStackCount > 0 ? $"{data.MagnetStackCount}" : " ";
                magnetCountText.enabled = true;
            }
            cachedMagnetStack = data.MagnetStackCount;
        }

        if (cachedShieldStack != data.ShieldStackCount)
        {
            if (shieldCountText != null)
            {
                shieldCountText.text = data.ShieldStackCount > 0 ? $"{data.ShieldStackCount}" : " ";
                shieldCountText.enabled = true;
            }
            cachedShieldStack = data.ShieldStackCount;
        }
    }
}
