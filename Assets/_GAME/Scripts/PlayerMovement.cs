using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private SwordOrbit swordOrbit;

    private Vector3 moveDirection;

    private SwordType currentSwordType = SwordType.Default;
    private int swordTypeCount;

    private void Start()
    {
        swordTypeCount = System.Enum.GetValues(typeof(SwordType)).Length;
        if (swordOrbit != null) swordOrbit.IsPlayer = true;
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

    public SwordOrbit GetSwordOrbit() => swordOrbit;
}
