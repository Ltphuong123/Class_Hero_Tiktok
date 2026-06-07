using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RankServerService : MonoBehaviour
{
    private const string BaseUrl           = "https://ranksever-production.up.railway.app";
    private const string SubmitPath        = "/api/rank/update";
    private const string WeeklyPath        = "/api/rank/weekly/point";
    private const string MonthlyPath       = "/api/rank/monthly/point";
    private const string WeeklyPlayerPath  = "/api/rank/weekly/point/";
    private const string MonthlyPlayerPath = "/api/rank/monthly/point/";

    // ── Submit kết quả trận ──────────────────────────────────────────────────
    public void SubmitResults(List<TopCharacterData> players)
    {
        StartCoroutine(PostResults(players));
    }

    private IEnumerator PostResults(List<TopCharacterData> players)
    {
        var payload = new PlayerListPayload
        {
            players = players.ConvertAll(p => new PlayerPayload
            {
                tikTokUserId = p.characterId,
                username     = p.characterName,
                avatar       = "",
                matchPoint   = p.score,
                killCount    = p.killPoints
            })
        };

        string json = JsonUtility.ToJson(payload);
        byte[] body  = System.Text.Encoding.UTF8.GetBytes(json);

        using UnityWebRequest req = new UnityWebRequest(BaseUrl + SubmitPath, "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("accept", "*/*");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[RankServer] Submit OK — {players.Count} players");
        else
            Debug.LogWarning($"[RankServer] Submit failed: {req.error}\n{req.downloadHandler.text}");
    }

    // ── Fetch bảng xếp hạng (danh sách) ─────────────────────────────────────
    public void FetchWeekly(Action<List<RankEntry>> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRanking(BaseUrl + WeeklyPath, onSuccess, onError));
    }

    public void FetchMonthly(Action<List<RankEntry>> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRanking(BaseUrl + MonthlyPath, onSuccess, onError));
    }

    private IEnumerator GetRanking(string url, Action<List<RankEntry>> onSuccess, Action<string> onError)
    {
        using UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("accept", "*/*");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string wrapped = $"{{\"items\":{req.downloadHandler.text}}}";
            RankEntryList list = JsonUtility.FromJson<RankEntryList>(wrapped);
            onSuccess?.Invoke(list?.items ?? new List<RankEntry>());
        }
        else
        {
            Debug.LogWarning($"[RankServer] Fetch failed: {req.error}");
            onError?.Invoke(req.error);
        }
    }

    // ── Kiểm tra rank cá nhân khi player vào game ────────────────────────────
    public static void CheckPlayerRanks(MonoBehaviour runner, string userId, string nickname)
    {
        runner.StartCoroutine(FetchAndCompareRanks(userId, nickname));
    }

    private static IEnumerator FetchAndCompareRanks(string userId, string nickname)
    {
        PlayerRankEntry weeklyEntry  = null;
        PlayerRankEntry monthlyEntry = null;

        yield return GetPlayerRankData(BaseUrl + WeeklyPlayerPath  + userId, e => weeklyEntry  = e);
        yield return GetPlayerRankData(BaseUrl + MonthlyPlayerPath + userId, e => monthlyEntry = e);

        // Log cả hai
        // if (weeklyEntry  != null && weeklyEntry.rank  > 0)
        //     Debug.Log($"[RankServer] {nickname} — BXH Tuần : Hạng #{weeklyEntry.rank}  | {weeklyEntry.warPoints:N0} pts | {weeklyEntry.totalKills} kills");
        // else
        //     Debug.Log($"[RankServer] {nickname} — chưa có hạng Tuần");

        // if (monthlyEntry != null && monthlyEntry.rank > 0)
        //     Debug.Log($"[RankServer] {nickname} — BXH Tháng: Hạng #{monthlyEntry.rank} | {monthlyEntry.warPoints:N0} pts | {monthlyEntry.totalKills} kills");
        // else
        //     Debug.Log($"[RankServer] {nickname} — chưa có hạng Tháng");

        int weeklyRank    = weeklyEntry  != null ? weeklyEntry.rank      : 0;
        int weeklyPoints  = weeklyEntry  != null ? weeklyEntry.warPoints  : 0;
        int monthlyRank   = monthlyEntry != null ? monthlyEntry.rank      : 0;
        int monthlyPoints = monthlyEntry != null ? monthlyEntry.warPoints : 0;
        PlayerIntroManager.Instance?.TryQueueBestIntro(nickname, weeklyRank, weeklyPoints, monthlyRank, monthlyPoints);
    }

    private static IEnumerator GetPlayerRankData(string url, Action<PlayerRankEntry> onDone)
    {
        using UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("accept", "*/*");
        yield return req.SendWebRequest();

        onDone?.Invoke(req.result == UnityWebRequest.Result.Success
            ? JsonUtility.FromJson<PlayerRankEntry>(req.downloadHandler.text)
            : null);
    }

    // ── Internal serializable types ──────────────────────────────────────────
    [System.Serializable] private class PlayerListPayload { public List<PlayerPayload> players; }

    [System.Serializable]
    private class PlayerPayload
    {
        public string tikTokUserId;
        public string username;
        public string avatar;
        public int    matchPoint;
        public int    killCount;
    }
}
