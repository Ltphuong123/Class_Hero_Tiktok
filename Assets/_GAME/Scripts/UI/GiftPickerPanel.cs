using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GiftPickerPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Transform      listContent;
    [SerializeField] private GameObject     itemPrefab;
    [SerializeField] private Button         closeButton;

    private GiftDatabase              database;
    private System.Action<GiftInfo>   onPicked;
    private readonly List<GiftPickerItem> spawnedItems = new();

    private void Awake() => gameObject.SetActive(false);

    public void Open(GiftDatabase db, System.Action<GiftInfo> callback)
    {
        database = db;
        onPicked = callback;
        gameObject.SetActive(true);

        if (searchInput != null) { searchInput.text = ""; searchInput.onValueChanged.AddListener(Filter); }
        closeButton?.onClick.AddListener(Close);

        Populate(db?.gifts);
    }

    public void Close()
    {
        searchInput?.onValueChanged.RemoveListener(Filter);
        closeButton?.onClick.RemoveListener(Close);
        gameObject.SetActive(false);
    }

    private void Filter(string query)
    {
        if (database == null) return;
        var list = string.IsNullOrEmpty(query)
            ? database.gifts
            : database.gifts.FindAll(g =>
                g.name.ToLower().Contains(query.ToLower()) ||
                g.id.ToString().Contains(query));
        Populate(list);
    }

    private void Populate(List<GiftInfo> gifts)
    {
        foreach (var item in spawnedItems)
            if (item != null) Destroy(item.gameObject);
        spawnedItems.Clear();

        if (gifts == null || itemPrefab == null || listContent == null) return;

        foreach (var gift in gifts)
        {
            var go   = Instantiate(itemPrefab, listContent);
            var item = go.GetComponent<GiftPickerItem>();
            if (item == null) continue;
            item.Setup(gift, OnItemPicked);
            spawnedItems.Add(item);
        }
    }

    private void OnItemPicked(GiftInfo gift)
    {
        onPicked?.Invoke(gift);
        Close();
    }
}
