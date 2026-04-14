using UnityEngine;

/// <summary>
/// Pure static scoring functions for AI state selection.
/// Extracted from AIBrain for testability without MonoBehaviour lifecycle.
/// </summary>
public static class AIScoring
{
    /// <summary>
    /// Wander score: base score when nothing interesting is nearby.
    /// Decreases gradually with more enemies/items to allow smooth transitions.
    /// </summary>
    public static float ComputeWanderScore(int nearbyEnemyCount, bool hasNearbyItem)
    {
        float score = 0.4f;
        if (nearbyEnemyCount > 0) score -= Mathf.Min(nearbyEnemyCount * 0.1f, 0.3f);
        if (hasNearbyItem) score -= 0.15f;
        return Mathf.Max(score, 0.05f); // never fully zero — always a fallback
    }

    /// <summary>
    /// Collect score: prioritizes collecting when owner has few swords.
    /// Scales down as sword count increases to avoid hoarding behavior.
    /// </summary>
    public static float ComputeCollectScore(bool hasSword, bool hasItem, int currentSwordCount, int maxSwords)
    {
        if (!hasSword && !hasItem) return 0f;

        // Diminishing returns: strong urge at 0 swords, weak at 4+
        int need = Mathf.Max(0, maxSwords - currentSwordCount);
        float needFactor = Mathf.Clamp01(need / (float)maxSwords);

        float score = 0.2f + needFactor * 0.5f;
        if (hasSword) score += 0.1f; // slight preference for swords over generic items
        return score;
    }

    /// <summary>
    /// Chase score: only triggers when owner has a CLEAR advantage.
    /// Requires at least 1 sword to chase. Scales with advantage magnitude.
    /// </summary>
    public static float ComputeChaseScore(int ownSwords, float ownHp, int enemySwords, float enemyHp, float maxHp)
    {
        // Must have at least 1 sword to be aggressive
        if (ownSwords <= 0) return 0f;

        // Need clear advantage: more swords OR significantly more HP
        int swordAdv = ownSwords - enemySwords;
        float hpRatio = maxHp > 0f ? (ownHp - enemyHp) / maxHp : 0f;

        // No advantage at all → don't chase
        if (swordAdv <= 0 && hpRatio <= 0.1f) return 0f;

        float score = 0.25f;
        if (swordAdv > 0) score += swordAdv * 0.15f;
        if (hpRatio > 0f) score += hpRatio * 0.25f;

        // Bonus for overwhelming advantage
        if (swordAdv >= 2) score += 0.15f;

        return Mathf.Min(score, 1.2f); // cap to prevent runaway scores
    }

    /// <summary>
    /// Flee score: triggers when enemy is clearly stronger.
    /// Low HP panic makes fleeing more urgent.
    /// </summary>
    public static float ComputeFleeScore(int ownSwords, float ownHp, int enemySwords, float enemyHp, float maxHp)
    {
        int swordDisadv = enemySwords - ownSwords;
        float hpRatio = maxHp > 0f ? (enemyHp - ownHp) / maxHp : 0f;

        // No threat → don't flee
        if (swordDisadv <= 0 && hpRatio <= 0.1f) return 0f;

        float score = 0f;
        if (swordDisadv > 0) score += swordDisadv * 0.2f;
        if (hpRatio > 0f) score += hpRatio * 0.2f;

        // Low HP panic: strong urge to flee
        if (maxHp > 0f && ownHp / maxHp < 0.3f) score += 0.35f;
        // Critical HP: even stronger
        if (maxHp > 0f && ownHp / maxHp < 0.15f) score += 0.2f;

        return score;
    }

    /// <summary>
    /// Select the state with the highest score. On tie, prefer currentState.
    /// </summary>
    public static AIState SelectState(float[] scores, AIState currentState)
    {
        int bestIndex = (int)currentState;
        float bestScore = scores[bestIndex];

        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i] > bestScore)
            {
                bestScore = scores[i];
                bestIndex = i;
            }
        }

        return (AIState)bestIndex;
    }
}
