using System.Collections.Generic;
using UnityEngine;

public class LevelIntroController : Singleton<LevelIntroController>
{
    [Header("Level Intro Prefabs (index 0 = lv2 ... index 8 = lv10)")]
    [SerializeField] private IconRevealEffect[] levelPrefabs;

    [Header("Lv2–7: Slot Positions (ưu tiên từ trên xuống, tối đa 4 đồng thời)")]
    [SerializeField] private RectTransform[] slotPositions;

    [Header("Lv8–10: 1 vị trí duy nhất")]
    [SerializeField] private RectTransform vipSlotPosition;

    private const int MaxOverflow = 16;

    private struct IntroRequest
    {
        public int    level;
        public string playerName;
    }

    // ── Lv2–7 ────────────────────────────────────────────────────────────────
    private readonly bool[]              slotBusy     = new bool[4];
    private readonly IconRevealEffect[]  slotInstance = new IconRevealEffect[4];
    private readonly Queue<IntroRequest> overflow     = new();

    // ── Lv8–10 ───────────────────────────────────────────────────────────────
    private bool             vipSlotBusy;
    private IconRevealEffect vipSlotInstance;
    private readonly Queue<IntroRequest> vipOverflow = new();

    // ─────────────────────────────────────────────────────────────────────────
    public void EnqueueIntro(int level, string playerName)
    {
        int prefabIdx = level - 2;
        if (levelPrefabs == null || prefabIdx < 0 || prefabIdx >= levelPrefabs.Length || levelPrefabs[prefabIdx] == null)
            return;

        if (level >= 8)
            EnqueueVip(prefabIdx, level, playerName);
        else
            EnqueueNormal(prefabIdx, level, playerName);
    }

    // ── Normal (lv2–7) ────────────────────────────────────────────────────────
    private void EnqueueNormal(int prefabIdx, int level, string playerName)
    {
        int slot = FindFreeSlot();
        if (slot >= 0)
            PlayOnSlot(prefabIdx, slot, playerName);
        else if (overflow.Count < MaxOverflow)
            overflow.Enqueue(new IntroRequest { level = level, playerName = playerName });
    }

    private void PlayOnSlot(int prefabIdx, int slot, string playerName)
    {
        slotBusy[slot] = true;

        RectTransform parent  = slotPositions != null && slot < slotPositions.Length ? slotPositions[slot] : null;
        IconRevealEffect inst = Instantiate(levelPrefabs[prefabIdx], parent);
        inst.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        inst.RecachePosition();
        slotInstance[slot] = inst;

        int captured = slot;
        inst.Play(playerName, () =>
        {
            if (slotInstance[captured] != null) { Destroy(slotInstance[captured].gameObject); slotInstance[captured] = null; }
            slotBusy[captured] = false;
            TryDequeueNormal();
        });
    }

    private void TryDequeueNormal()
    {
        while (overflow.Count > 0)
        {
            int slot = FindFreeSlot();
            if (slot < 0) return;

            IntroRequest req       = overflow.Dequeue();
            int          prefabIdx = req.level - 2;
            if (levelPrefabs == null || prefabIdx < 0 || prefabIdx >= levelPrefabs.Length || levelPrefabs[prefabIdx] == null)
                continue;

            PlayOnSlot(prefabIdx, slot, req.playerName);
        }
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < slotBusy.Length; i++)
            if (!slotBusy[i]) return i;
        return -1;
    }

    // ── VIP (lv8–10) ─────────────────────────────────────────────────────────
    private void EnqueueVip(int prefabIdx, int level, string playerName)
    {
        if (!vipSlotBusy)
            PlayVipSlot(prefabIdx, playerName);
        else if (vipOverflow.Count < MaxOverflow)
            vipOverflow.Enqueue(new IntroRequest { level = level, playerName = playerName });
    }

    private void PlayVipSlot(int prefabIdx, string playerName)
    {
        vipSlotBusy = true;

        IconRevealEffect inst = Instantiate(levelPrefabs[prefabIdx], vipSlotPosition);
        inst.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        inst.RecachePosition();
        vipSlotInstance = inst;

        inst.Play(playerName, () =>
        {
            if (vipSlotInstance != null) { Destroy(vipSlotInstance.gameObject); vipSlotInstance = null; }
            vipSlotBusy = false;
            TryDequeueVip();
        });
    }

    private void TryDequeueVip()
    {
        if (vipOverflow.Count == 0 || vipSlotBusy) return;

        IntroRequest req       = vipOverflow.Dequeue();
        int          prefabIdx = req.level - 2;
        if (levelPrefabs == null || prefabIdx < 0 || prefabIdx >= levelPrefabs.Length || levelPrefabs[prefabIdx] == null)
        {
            TryDequeueVip();
            return;
        }

        PlayVipSlot(prefabIdx, req.playerName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void StopAll()
    {
        overflow.Clear();
        for (int i = 0; i < slotBusy.Length; i++)
        {
            if (slotInstance[i] != null) { Destroy(slotInstance[i].gameObject); slotInstance[i] = null; }
            slotBusy[i] = false;
        }

        vipOverflow.Clear();
        if (vipSlotInstance != null) { Destroy(vipSlotInstance.gameObject); vipSlotInstance = null; }
        vipSlotBusy = false;
    }
}
