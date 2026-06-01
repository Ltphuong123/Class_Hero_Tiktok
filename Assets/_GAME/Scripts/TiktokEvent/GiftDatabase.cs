using UnityEngine;
using System.Collections.Generic;
using System.IO;

[CreateAssetMenu(fileName = "GiftDatabase", menuName = "Game/Gift Database")]
public class GiftDatabase : ScriptableObject
{
    [Tooltip("Đường dẫn file xlsx (để trống = dùng đường dẫn mặc định bên cạnh script)")]
    [SerializeField] private string xlsxPath = "";

    public List<GiftInfo> gifts = new List<GiftInfo>();

    private static string JsonCachePath =>
#if UNITY_EDITOR
        Path.Combine(Application.dataPath, "_GAME/So/GiftDatabase.json");
#else
        Path.Combine(Application.dataPath, "../GiftDatabase.json");
#endif

    private void OnEnable() => LoadFromJson();

    public GiftInfo GetById(int id) => gifts?.Find(g => g.id == id);

    [ContextMenu("Import from XLSX")]
    public void ImportFromXlsx()
    {
        string path = string.IsNullOrEmpty(xlsxPath)
            ? Path.Combine(Application.dataPath, "_GAME/Scripts/TiktokEvent/gifts_with_images.xlsx")
            : xlsxPath;
        LoadFromXlsx(path);
    }

    public void LoadFromXlsx(string path)
    {
        if (!File.Exists(path)) { Debug.LogError($"[GiftDB] File not found: {path}"); return; }
        try
        {
            gifts = XlsxParser.Parse(path);
            SaveToJson();
            Debug.Log($"[GiftDB] Loaded {gifts.Count} gifts");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        catch (System.Exception e) { Debug.LogError($"[GiftDB] {e.Message}"); }
    }

    public void SaveToJson()
    {
        try
        {
            string dir = Path.GetDirectoryName(JsonCachePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(JsonCachePath, JsonUtility.ToJson(new Wrapper { gifts = gifts }, true));
        }
        catch (System.Exception e) { Debug.LogWarning($"[GiftDB] SaveToJson: {e.Message}"); }
    }

    public void LoadFromJson()
    {
        if (!File.Exists(JsonCachePath)) return;
        try
        {
            var w = JsonUtility.FromJson<Wrapper>(File.ReadAllText(JsonCachePath));
            if (w?.gifts != null && w.gifts.Count > 0) gifts = w.gifts;
        }
        catch { }
    }

    [System.Serializable] private class Wrapper { public List<GiftInfo> gifts; }
}
