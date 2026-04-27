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
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI swordCountText;
    [SerializeField] private TextMeshProUGUI killPointsText;
    
    [Header("Booster Texts")]
    [SerializeField] private TextMeshProUGUI magnetCountText;
    [SerializeField] private TextMeshProUGUI shieldCountText;
    [SerializeField] private TextMeshProUGUI meteorCountText;
    
    [Header("Booster Fill Images")]
    [SerializeField] private Image magnetTimeFill;
    [SerializeField] private Image shieldTimeFill;
    [SerializeField] private Image meteorTimeFill;
    
    private TextMeshProUGUI magnetFillText;
    private TextMeshProUGUI shieldFillText;
    private TextMeshProUGUI meteorFillText;

    [Header("Images")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image levelIcon;
    [SerializeField] private Image levelTimeFill;

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
    private int cachedMagnetStack = -1;
    private int cachedShieldStack = -1;
    private int cachedMeteorStack = -1;
    private CharacterBase currentCharacter;

    public static event System.Action<CharacterBase> OnRowClicked;

    private void Awake()
    {
        if (magnetTimeFill != null)
            magnetFillText = magnetTimeFill.GetComponentInChildren<TextMeshProUGUI>();
        
        if (shieldTimeFill != null)
            shieldFillText = shieldTimeFill.GetComponentInChildren<TextMeshProUGUI>();
        
        if (meteorTimeFill != null)
            meteorFillText = meteorTimeFill.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetData(CharacterRankData data)
    {
        currentCharacter = data.Character;

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
            swordCountText.text = $"{totalSwords}";
            
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
            killPointsText.text = $"{data.KillPoints}";
            
            if (cachedKillPoints >= 0 && data.KillPoints > cachedKillPoints)
            {
                killPointsText.transform.DOKill();
                killPointsText.transform.localScale = Vector3.one;
                killPointsText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
            }
            
            cachedKillPoints = data.KillPoints;
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
        
        if (cachedMeteorStack != data.MeteorStackCount)
        {
            TextMeshProUGUI targetText = meteorCountText != null ? meteorCountText : meteorFillText;
            
            if (targetText != null)
            {
                if (data.MeteorStackCount > 0)
                {
                    targetText.text = $"{data.MeteorStackCount}";
                    targetText.enabled = true;
                }
                else
                {
                    targetText.text = " ";
                    targetText.enabled = true;
                }
            }
            
            cachedMeteorStack = data.MeteorStackCount;
        }
        
        if (meteorTimeFill != null && currentCharacter != null)
        {
            if ((currentCharacter.IsCastingMeteor || currentCharacter.IsMeteorOnCooldown) && data.MeteorCastTimeRemaining > 0f)
            {
                float duration = currentCharacter.MeteorCastDuration;
                float ratio = data.MeteorCastTimeRemaining / duration;
                meteorTimeFill.fillAmount = 1-ratio;
                meteorTimeFill.enabled = true;
            }
            else
            {
                meteorTimeFill.fillAmount = 1;
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
}
