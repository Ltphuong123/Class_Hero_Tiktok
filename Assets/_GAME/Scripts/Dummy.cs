using UnityEngine;

public class Dummy : MonoBehaviour
{
    [SerializeField] private SwordOrbit swordOrbit;

    [Header("Character Info")]
    [SerializeField] private string characterName = "Dummy";
    [SerializeField] private Sprite avatar;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private CharacterInfoUI infoUI;

    private float currentHp;

    private void Start()
    {
        currentHp = maxHp;
        if (infoUI != null) infoUI.Init(characterName, avatar, currentHp, maxHp);
    }

    public void TakeDamage(float damage)
    {
        currentHp = Mathf.Max(0f, currentHp - damage);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
        if (currentHp <= 0f) OnDeath();
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        if (infoUI != null) infoUI.UpdateHp(currentHp, maxHp);
    }

    private void OnDeath()
    {
        // TODO: xử lý chết
    }

    public SwordOrbit GetSwordOrbit() => swordOrbit;
}
