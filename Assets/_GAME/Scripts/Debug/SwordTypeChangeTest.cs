using UnityEngine;

/// <summary>
/// Test script để verify rằng khi đổi sword type, HP được reset đúng.
/// Attach vào bất kỳ GameObject nào và gọi methods từ Inspector hoặc Console.
/// </summary>
public class SwordTypeChangeTest : MonoBehaviour
{
    [Header("Test Target")]
    [SerializeField] private CharacterBase testCharacter;

    [Header("Test Settings")]
    [SerializeField] private SwordType targetType = SwordType.Fire;
    [SerializeField] private float damageAmount = 50f;

    [ContextMenu("1. Damage All Swords")]
    public void DamageAllSwords()
    {
        if (testCharacter == null)
        {
            Debug.LogError("[Test] Test Character chưa được gán!");
            return;
        }

        SwordOrbit orbit = testCharacter.GetSwordOrbit();
        if (orbit == null)
        {
            Debug.LogError("[Test] Character không có SwordOrbit!");
            return;
        }

        int count = 0;
        foreach (Transform child in orbit.transform)
        {
            Sword sword = child.GetComponent<Sword>();
            if (sword != null)
            {
                float hpBefore = sword.CurrentHp;
                sword.TakeDamage(damageAmount);
                Debug.Log($"[Test] Sword {count}: HP {hpBefore} → {sword.CurrentHp} (Max: {sword.MaxHp})");
                count++;
            }
        }

        Debug.Log($"[Test] Đã damage {count} kiếm, mỗi kiếm -{damageAmount} HP");
    }

    [ContextMenu("2. Change Sword Type (Should Reset HP)")]
    public void ChangeSwordType()
    {
        if (testCharacter == null)
        {
            Debug.LogError("[Test] Test Character chưa được gán!");
            return;
        }

        SwordOrbit orbit = testCharacter.GetSwordOrbit();
        if (orbit == null)
        {
            Debug.LogError("[Test] Character không có SwordOrbit!");
            return;
        }

        Debug.Log($"[Test] Đổi sang type: {targetType}");
        orbit.SetSwordType(targetType);

        // Verify HP đã được reset
        int count = 0;
        foreach (Transform child in orbit.transform)
        {
            Sword sword = child.GetComponent<Sword>();
            if (sword != null)
            {
                bool isFullHp = Mathf.Approximately(sword.CurrentHp, sword.MaxHp);
                string status = isFullHp ? "✅ FULL HP" : "❌ NOT FULL";
                Debug.Log($"[Test] Sword {count}: HP {sword.CurrentHp}/{sword.MaxHp} - {status}");
                count++;
            }
        }
    }

    [ContextMenu("3. Show All Sword HP")]
    public void ShowAllSwordHP()
    {
        if (testCharacter == null)
        {
            Debug.LogError("[Test] Test Character chưa được gán!");
            return;
        }

        SwordOrbit orbit = testCharacter.GetSwordOrbit();
        if (orbit == null)
        {
            Debug.LogError("[Test] Character không có SwordOrbit!");
            return;
        }

        Debug.Log($"[Test] === Sword HP Status for {testCharacter.CharacterName} ===");
        
        int count = 0;
        float totalHp = 0f;
        float totalMaxHp = 0f;

        foreach (Transform child in orbit.transform)
        {
            Sword sword = child.GetComponent<Sword>();
            if (sword != null)
            {
                totalHp += sword.CurrentHp;
                totalMaxHp += sword.MaxHp;
                
                string bar = GenerateHpBar(sword.HpRatio);
                Debug.Log($"[Test] Sword {count} ({sword.SwordType}): {bar} {sword.CurrentHp:F0}/{sword.MaxHp:F0}");
                count++;
            }
        }

        float avgRatio = totalMaxHp > 0f ? (totalHp / totalMaxHp) * 100f : 0f;
        Debug.Log($"[Test] Total: {count} swords, Average HP: {avgRatio:F1}%");
    }

    [ContextMenu("4. Full Test Sequence")]
    public void FullTestSequence()
    {
        Debug.Log("[Test] ========== STARTING FULL TEST ==========");
        
        Debug.Log("\n[Test] Step 1: Show initial HP");
        ShowAllSwordHP();

        Debug.Log("\n[Test] Step 2: Damage all swords");
        DamageAllSwords();

        Debug.Log("\n[Test] Step 3: Show damaged HP");
        ShowAllSwordHP();

        Debug.Log("\n[Test] Step 4: Change sword type (should reset HP)");
        ChangeSwordType();

        Debug.Log("\n[Test] Step 5: Verify HP reset");
        ShowAllSwordHP();

        Debug.Log("\n[Test] ========== TEST COMPLETE ==========");
    }

    private string GenerateHpBar(float ratio)
    {
        int barLength = 10;
        int filled = Mathf.RoundToInt(ratio * barLength);
        string bar = "[";
        
        for (int i = 0; i < barLength; i++)
        {
            bar += i < filled ? "█" : "░";
        }
        
        bar += "]";
        return bar;
    }

    // Keyboard shortcuts
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[Test] F1: Damage All Swords");
            DamageAllSwords();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[Test] F2: Change Sword Type");
            ChangeSwordType();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[Test] F3: Show All Sword HP");
            ShowAllSwordHP();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("[Test] F4: Full Test Sequence");
            FullTestSequence();
        }
    }
}
