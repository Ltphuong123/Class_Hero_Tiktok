using UnityEngine;
using UnityEngine.UI;

public class GameEndRow : MonoBehaviour
{
    [SerializeField] private Text rankText;
    [SerializeField] private Text nameTMPText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Text killPointsText;
    [SerializeField] private Text scoreText;

    public void SetData(TopCharacterData data)
    {
        if (rankText       != null) rankText.text       = $"#{data.rank}";
        if (nameTMPText    != null) nameTMPText.text    = data.characterName;
        if (killPointsText != null) killPointsText.text = $"{data.killPoints:N0}";
        if (scoreText != null)
        {
            int baseScore = data.score - data.bonusScore;
            scoreText.text = data.bonusScore > 0
                ? $"{FormatScore(baseScore)} (+{FormatScore(data.bonusScore)})"
                : FormatScore(data.score);
        }

        if (avatarImage != null)
        {
            avatarImage.enabled = data.avatar != null;
            if (data.avatar != null) avatarImage.sprite = data.avatar;
        }
    }

    private static string FormatScore(int value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000f:0.#}M";
        if (value >= 1_000)     return $"{value / 1_000f:0.#}k";
        return value.ToString();
    }
}
