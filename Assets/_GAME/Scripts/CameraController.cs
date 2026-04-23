using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float targetFollowZoom = 15f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float pinchZoomSpeed = 0.05f;

    [Header("Drag")]
    [SerializeField] private bool allowDrag = true;
    [SerializeField] private float dragSpeed = 1f;

    [Header("Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 boundsMax = new Vector2(50f, 50f);

    private Camera cam;
    private bool isDragging;
    private Vector3 dragOrigin;
    private float targetZoom;
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
        ClampCameraToBounds();
    }

    private void HandleZoom()
    {
        if (IsPointerOverUI()) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
        {
            if (target != null) target = null;
            targetZoom -= scroll * zoomSpeed;
        }

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float pinchDist = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
                lastPinchDist = pinchDist;
            else
            {
                float delta = lastPinchDist - pinchDist;
                if (delta != 0f && target != null) target = null;
                targetZoom += delta * pinchZoomSpeed;
                lastPinchDist = pinchDist;
            }
        }

        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        
        if (useBounds)
        {
            float boundsWidth = boundsMax.x - boundsMin.x;
            float boundsHeight = boundsMax.y - boundsMin.y;
            float maxZoomForBounds = Mathf.Min(boundsWidth / (2f * cam.aspect), boundsHeight / 2f);
            targetZoom = Mathf.Min(targetZoom, maxZoomForBounds);
        }
        
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * 10f);
    }

    private void HandleDrag()
    {
        if (!allowDrag || Input.touchCount >= 2) { isDragging = false; return; }

        bool pressed = Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began);
        bool held = Input.GetMouseButton(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved);
        bool released = Input.GetMouseButtonUp(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended);

        if (pressed && !IsPointerOverUI())
        {
            isDragging = true;
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (held && isDragging)
        {
            Vector3 diff = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            if (diff.magnitude > 0.01f && target != null) target = null;
            transform.position += diff * dragSpeed;
        }

        if (released) isDragging = false;
    }

    private void FollowTarget()
    {
        if (target == null || isDragging) return;

        Vector3 desired = target.position + offset;
        desired.z = offset.z;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        
        targetZoom = targetFollowZoom;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * 15f);
    }

    private void ClampCameraToBounds()
    {
        if (!useBounds) return;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float minX = boundsMin.x + camWidth;
        float maxX = boundsMax.x - camWidth;
        float minY = boundsMin.y + camHeight;
        float maxY = boundsMax.y - camHeight;

        if (minX > maxX) minX = maxX = (boundsMin.x + boundsMax.x) * 0.5f;
        if (minY > maxY) minY = maxY = (boundsMin.y + boundsMax.y) * 0.5f;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetZoom(float zoom) => targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    public float GetTargetFollowZoom() => targetFollowZoom;

    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
        useBounds = true;
    }

    public void SetBoundsFromMap()
    {
        MapManager map = MapManager.Instance;
        if (map != null)
        {
            boundsMin = map.MapMin;
            boundsMax = map.MapMax;
            useBounds = true;
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.yellow;
        Vector3 bl = new Vector3(boundsMin.x, boundsMin.y, 0f);
        Vector3 br = new Vector3(boundsMax.x, boundsMin.y, 0f);
        Vector3 tl = new Vector3(boundsMin.x, boundsMax.y, 0f);
        Vector3 tr = new Vector3(boundsMax.x, boundsMax.y, 0f);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        if (cam != null)
        {
            Gizmos.color = Color.cyan;
            float h = cam.orthographicSize;
            float w = h * cam.aspect;

            float minX = boundsMin.x + w;
            float maxX = boundsMax.x - w;
            float minY = boundsMin.y + h;
            float maxY = boundsMax.y - h;

            if (minX <= maxX && minY <= maxY)
            {
                Vector3 ibl = new Vector3(minX, minY, 0f);
                Vector3 ibr = new Vector3(maxX, minY, 0f);
                Vector3 itl = new Vector3(minX, maxY, 0f);
                Vector3 itr = new Vector3(maxX, maxY, 0f);

                Gizmos.DrawLine(ibl, ibr);
                Gizmos.DrawLine(ibr, itr);
                Gizmos.DrawLine(itr, itl);
                Gizmos.DrawLine(itl, ibl);
            }
        }
    }
}
