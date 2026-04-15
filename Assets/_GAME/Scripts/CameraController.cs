using UnityEngine;

/// <summary>
/// Camera controller: follow target, zoom in/out (scroll/pinch), kéo camera (drag).
/// Gắn vào Main Camera.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float pinchZoomSpeed = 0.05f;

    [Header("Drag")]
    [SerializeField] private bool allowDrag = true;
    [SerializeField] private float dragSpeed = 1f;

    private Camera cam;
    private bool isDragging;
    private Vector3 dragOrigin;
    private float targetZoom;

    // Pinch zoom
    private float lastPinchDist;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize;
    }

    private void LateUpdate()
    {
        HandleZoom();
        HandleDrag();
        FollowTarget();
    }

    private void HandleZoom()
    {
        // Mouse scroll
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
            targetZoom -= scroll * zoomSpeed;

        // Touch pinch
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float pinchDist = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                lastPinchDist = pinchDist;
            }
            else
            {
                float delta = lastPinchDist - pinchDist;
                targetZoom += delta * pinchZoomSpeed;
                lastPinchDist = pinchDist;
            }
        }

        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * 10f);
    }

    private void HandleDrag()
    {
        if (!allowDrag) return;

        // Không drag khi đang pinch zoom
        if (Input.touchCount >= 2) { isDragging = false; return; }

        // Mouse hoặc 1 ngón tay
        bool pressed = Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began);
        bool held = Input.GetMouseButton(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved);
        bool released = Input.GetMouseButtonUp(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended);

        if (pressed)
        {
            isDragging = true;
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (held && isDragging)
        {
            Vector3 current = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = dragOrigin - current;
            transform.position += diff * dragSpeed;
        }

        if (released)
            isDragging = false;
    }

    private void FollowTarget()
    {
        if (target == null || isDragging) return;

        Vector3 desired = target.position + offset;
        desired.z = offset.z;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Đổi target follow runtime.
    /// </summary>
    public void SetTarget(Transform newTarget) => target = newTarget;

    /// <summary>
    /// Zoom đến giá trị cụ thể.
    /// </summary>
    public void SetZoom(float zoom) => targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
}
