using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardRankRow : MonoBehaviour
{
    [Header("Rank slot")]
    [SerializeField] private Text  rankText;
    [SerializeField] private Image rankIcon;
    [SerializeField] private Sprite[] rankIcons; // index 0=rank1, 1=rank2, 2=rank3

    [Header("Info")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Text  nicknameText;
    [SerializeField] private Text  scoreText;
    [SerializeField] private Text  killsText;

    public void SetData(RankEntry data)
    {
        SetRankSlot(data.rank);

        if (nicknameText != null) nicknameText.text = data.nickname;
        if (scoreText    != null) scoreText.text    = FormatScore(data.score);
        if (killsText    != null) killsText.text    = data.totalKills.ToString();

        if (avatarImage != null)
        {
            if (!string.IsNullOrEmpty(data.avatarUrl))
                StartCoroutine(LoadAvatar(data.avatarUrl));
            else
                avatarImage.enabled = false;
        }
    }

    public void SetMatchData(TopCharacterData data)
    {
        SetRankSlot(data.rank);

        if (nicknameText != null) nicknameText.text = data.characterName;
        if (scoreText    != null) scoreText.text    = FormatScore(data.score);
        if (killsText    != null) killsText.text    = data.killPoints.ToString();

        if (avatarImage != null)
        {
            avatarImage.enabled = data.avatar != null;
            if (data.avatar != null) avatarImage.sprite = data.avatar;
        }
    }

    private void SetRankSlot(int rank)
    {
        bool useIcon = rank >= 1 && rank <= 3
                       && rankIcons != null && rankIcons.Length >= rank
                       && rankIcons[rank - 1] != null;

        rankText?.gameObject.SetActive(!useIcon);

        if (rankIcon != null)
        {
            rankIcon.gameObject.SetActive(useIcon);
            if (useIcon) rankIcon.sprite = rankIcons[rank - 1];
        }

        if (!useIcon && rankText != null) rankText.text = $"#{rank}";
    }

    private IEnumerator LoadAvatar(string url)
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success && avatarImage != null)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            avatarImage.sprite  = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            avatarImage.enabled = true;
        }
    }

    private static string FormatScore(int value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000f:0.#}M";
        if (value >= 1_000)     return $"{value / 1_000f:0.#}k";
        return value.ToString();
    }
}
