using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterClickHandler : MonoBehaviour
{
    private Camera mainCam;
    private CameraController camController;

    private void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) camController = mainCam.GetComponent<CameraController>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || IsPointerOverUI()) return;
        if (mainCam == null || camController == null) return;

        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        if (hit != null)
        {
            CharacterBase character = hit.GetComponent<CharacterBase>();
            if (character != null && !character.IsDead)
            {
                camController.SetTarget(character.transform);
                return;
            }
        }

        camController.SetTarget(null);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
        return false;
    }
}
