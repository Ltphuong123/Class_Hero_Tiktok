// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.EventSystems;

// public class LeaderboardRow : MonoBehaviour, IPointerClickHandler
// {
//     [Header("Texts")]
//     [SerializeField] private TextMeshProUGUI rankText;
//     [SerializeField] private TextMeshProUGUI nameText;
//     [SerializeField] private TextMeshProUGUI hpText;
//     [SerializeField] private TextMeshProUGUI swordCountText;
//     [SerializeField] private TextMeshProUGUI powerText;
//     [SerializeField] private TextMeshProUGUI stateText;

//     [Header("Images")]
//     [SerializeField] private Image avatarImage;
//     [SerializeField] private Image hpBar;
//     [SerializeField] private Image background;

//     private static readonly Color GoldColor = new(1f, 0.84f, 0f, 0.3f);
//     private static readonly Color SilverColor = new(0.75f, 0.75f, 0.75f, 0.25f);
//     private static readonly Color BronzeColor = new(0.8f, 0.5f, 0.2f, 0.2f);
//     private static readonly Color DefaultColor = new(1f, 1f, 1f, 0.1f);

//     private int cachedRank = -1;
//     private float cachedHp = -1f;
//     private int cachedSwordCount = -1;
//     private CharacterBase currentCharacter;

//     // Event để notify khi row được click
//     public static event System.Action<CharacterBase> OnRowClicked;

//     public void SetData(CharacterRankData data)
//     {
//         // Cache character reference
//         currentCharacter = data.Character;

//         // Update rank (always, vì có thể thay đổi vị trí)
//         if (rankText != null && cachedRank != data.Rank)
//         {
//             rankText.text = $"#{data.Rank}";
//             cachedRank = data.Rank;
//         }

//         // Update name (chỉ lần đầu)
//         if (nameText != null && string.IsNullOrEmpty(nameText.text))
//             nameText.text = data.Name;

//         // Update HP (chỉ khi thay đổi)
//         if (hpText != null && !Mathf.Approximately(cachedHp, data.CurrentHp))
//         {
//             hpText.text = $"{data.CurrentHp:F0}/{data.MaxHp:F0}";
//             cachedHp = data.CurrentHp;
//         }

//         // Update sword count (chỉ khi thay đổi)
//         if (swordCountText != null && cachedSwordCount != data.SwordCount)
//         {
//             swordCountText.text = data.SwordCount.ToString();
//             cachedSwordCount = data.SwordCount;
//         }

//         // Update power (luôn update vì phụ thuộc HP + swords)
//         if (powerText != null)
//             powerText.text = $"{data.Power:F0}";

//         // Update state (có thể thay đổi thường xuyên)
//         if (stateText != null)
//             stateText.text = data.StateName;

//         // Update avatar (chỉ lần đầu)
//         if (avatarImage != null && avatarImage.sprite != data.Avatar)
//         {
//             avatarImage.enabled = data.Avatar != null;
//             if (data.Avatar != null) avatarImage.sprite = data.Avatar;
//         }

//         // Update HP bar
//         if (hpBar != null)
//         {
//             float ratio = data.MaxHp > 0f ? data.CurrentHp / data.MaxHp : 0f;
//             hpBar.fillAmount = ratio;
//             hpBar.color = Color.Lerp(Color.red, Color.green, ratio);
//         }

//         // Update background color
//         if (background != null)
//         {
//             Color targetColor = data.Rank switch
//             {
//                 1 => GoldColor,
//                 2 => SilverColor,
//                 3 => BronzeColor,
//                 _ => DefaultColor
//             };
            
//             if (background.color != targetColor)
//                 background.color = targetColor;
//         }
//     }

//     // Implement IPointerClickHandler
//     public void OnPointerClick(PointerEventData eventData)
//     {
//         if (currentCharacter != null)
//         {
//             Debug.Log($"[LeaderboardRow] Clicked on {currentCharacter.CharacterName}");
//             OnRowClicked?.Invoke(currentCharacter);
//         }
//     }
// }
