using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private SwordOrbit swordOrbit;

    [Header("Character Info")]
    [SerializeField] private string characterName = "Player";
    [SerializeField] private Sprite avatar;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private CharacterInfoUI infoUI;

    private float currentHp;
    private Vector3 moveDirection;
    private SwordType currentSwordType = SwordType.Default;
    private int swordTypeCount;

    private void Start()
    {
        currentHp = maxHp;
        swordTypeCount = System.Enum.GetValues(typeof(SwordType)).Length;
        if (swordOrbit != null) swordOrbit.IsPlayer = true;
        if (infoUI != null) infoUI.Init(characterName, avatar, currentHp, maxHp);
    }

    private void Update()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        if (Input.GetKey(KeyCode.D)) x = 1f;
        if (Input.GetKey(KeyCode.W)) y = 1f;
        if (Input.GetKey(KeyCode.S)) y = -1f;

        moveDirection = new Vector3(x, y, 0f).normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Q) && swordOrbit != null)
            swordOrbit.IncreaseRadius(0.5f);

        if (Input.GetKeyDown(KeyCode.E) && swordOrbit != null)
        {
            currentSwordType = (SwordType)(((int)currentSwordType + 1) % swordTypeCount);
            swordOrbit.SetSwordType(currentSwordType);
        }
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
