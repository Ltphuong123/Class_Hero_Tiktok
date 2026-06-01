using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GiftPickerItem : MonoBehaviour
{
    [SerializeField] private RawImage      icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI detailText; // "ID: x  |  x 💎"
    [SerializeField] private Button        selectButton;

    private static readonly Dictionary<string, Texture2D> imageCache = new();

    public static bool TryGetCached(string url, out Texture2D tex) => imageCache.TryGetValue(url, out tex);
    public static void AddToCache(string url, Texture2D tex) { if (tex != null) imageCache[url] = tex; }

    private System.Action<GiftInfo> onSelect;
    private GiftInfo gift;
    private Coroutine loadRoutine;

    public void Setup(GiftInfo data, System.Action<GiftInfo> callback)
    {
        gift     = data;
        onSelect = callback;

        if (nameText   != null) nameText.text   = data.name;
        if (detailText != null) detailText.text = $"ID: {data.id}  |  {data.diamond} 💎";

        selectButton?.onClick.AddListener(OnClick);

        if (icon != null)
        {
            icon.color   = new Color(0.35f, 0.35f, 0.4f); // placeholder grey
            icon.texture = null;

            if (!string.IsNullOrEmpty(data.imageUrl))
                loadRoutine = StartCoroutine(LoadIcon(data.imageUrl));
            else
                Debug.LogWarning($"[GiftPickerItem] imageUrl trống — gift id={data.id}");
        }
    }

    private void OnClick() => onSelect?.Invoke(gift);

    private IEnumerator LoadIcon(string url)
    {
        // Trả về từ cache
        if (imageCache.TryGetValue(url, out Texture2D cached))
        {
            ApplyTexture(cached);
            yield break;
        }

        // Unity không hỗ trợ WebP — TikTok CDN cho phép đổi format qua URL
        string loadUrl = ToJpegUrl(url);

        using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(loadUrl);
        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
            imageCache[url] = tex;   // cache theo URL gốc
            ApplyTexture(tex);
        }
        else
        {
            Debug.LogWarning($"[GiftPickerItem] Load thất bại\n  URL: {loadUrl}\n  Lỗi: {req.error}  (HTTP {req.responseCode})");
        }
    }

    // TikTok CDN: "...~tplv-obj.webp" → "...~tplv-obj.jpeg"
    // Các URL khác: giữ nguyên
    private static string ToJpegUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.EndsWith(".webp", System.StringComparison.OrdinalIgnoreCase))
            return url.Substring(0, url.Length - 5) + ".jpeg";
        return url;
    }

    private void ApplyTexture(Texture2D tex)
    {
        if (icon == null || tex == null) return;
        icon.texture = tex;
        icon.color   = Color.white;                   // bỏ grey tint
        icon.uvRect  = new Rect(0f, 0f, 1f, 1f);     // reset UV
    }

    private void OnDestroy()
    {
        if (loadRoutine != null) StopCoroutine(loadRoutine);
        selectButton?.onClick.RemoveAllListeners();
    }
}
