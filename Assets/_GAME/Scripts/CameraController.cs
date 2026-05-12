using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 20f, 0f);
    [SerializeField] private float targetFollowHeight = 20f;

    [Header("Zoom (Height)")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 50f;
    [SerializeField] private float pinchZoomSpeed = 0.1f;

    [Header("Drag")]
    [SerializeField] private bool allowDrag = true;

    [Header("Bounds (X, Z)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 boundsMax = new Vector2(50f, 50f);

    private Camera cam;
    private bool isDragging;
    private Vector3 dragWorldOrigin;
    private float targetHeight;
    private float lastPinchDist;
    private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetHeight = transform.position.y;
    }

    private void LateUpdate()
    {
        HandleZoom();
        HandleDrag();
        FollowTarget();
        ClampToBounds();
    }

    private void HandleZoom()
    {
        if (IsPointerOverUI()) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
        {
            if (target != null) target = null;
            targetHeight -= scroll * zoomSpeed;
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
                targetHeight += delta * pinchZoomSpeed;
                lastPinchDist = pinchDist;
            }
        }

        targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

        if (useBounds)
            targetHeight = Mathf.Min(targetHeight, MaxHeightForBounds());

        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetHeight, Time.deltaTime * 10f);
        transform.position = pos;
    }

    private void HandleDrag()
    {
        if (!allowDrag || Input.touchCount >= 2) { isDragging = false; return; }

        Vector3 inputPos = Input.touchCount == 1
            ? (Vector3)Input.GetTouch(0).position
            : Input.mousePosition;

        bool pressed  = Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began);
        bool held     = Input.GetMouseButton(0)     || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved);
        bool released = Input.GetMouseButtonUp(0)   || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended);

        if (pressed && !IsPointerOverUI())
        {
            if (GetGroundPoint(inputPos, out Vector3 worldPoint))
            {
                isDragging = true;
                dragWorldOrigin = worldPoint;
            }
        }

        if (held && isDragging)
        {
            if (GetGroundPoint(inputPos, out Vector3 currentGroundPoint))
            {
                Vector3 diff = dragWorldOrigin - currentGroundPoint;
                diff.y = 0f;
                if (diff.sqrMagnitude > 0.0001f && target != null) target = null;
                Vector3 pos = transform.position;
                pos.x += diff.x;
                pos.z += diff.z;
                transform.position = pos;
            }
        }

        if (released) isDragging = false;
    }

    private void FollowTarget()
    {
        if (target == null || isDragging) return;

        Vector3 desired = target.position + offset;
        Vector3 current = transform.position;
        current.x = Mathf.Lerp(current.x, desired.x, followSpeed * Time.deltaTime);
        current.z = Mathf.Lerp(current.z, desired.z, followSpeed * Time.deltaTime);
        current.y = Mathf.Lerp(current.y, targetFollowHeight, followSpeed * Time.deltaTime);
        transform.position = current;

        targetHeight = targetFollowHeight;
    }

    private void ClampToBounds()
    {
        if (!useBounds) return;

        VisibleExtents(transform.position.y, out float halfX, out float halfZ);

        float minX = boundsMin.x + halfX;
        float maxX = boundsMax.x - halfX;
        float minZ = boundsMin.y + halfZ;
        float maxZ = boundsMax.y - halfZ;

        if (minX > maxX) minX = maxX = (boundsMin.x + boundsMax.x) * 0.5f;
        if (minZ > maxZ) minZ = maxZ = (boundsMin.y + boundsMax.y) * 0.5f;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }

    private void VisibleExtents(float height, out float halfX, out float halfZ)
    {
        float tanHalfFov = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        halfZ = height * tanHalfFov;
        halfX = halfZ * cam.aspect;
    }

    private float MaxHeightForBounds()
    {
        float tanHalfFov = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float boundsWidth  = boundsMax.x - boundsMin.x;
        float boundsHeight = boundsMax.y - boundsMin.y;
        float maxByZ = boundsHeight * 0.5f / tanHalfFov;
        float maxByX = boundsWidth  * 0.5f / (tanHalfFov * cam.aspect);
        return Mathf.Min(maxByZ, maxByX);
    }

    private bool GetGroundPoint(Vector3 screenPos, out Vector3 worldPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }
        worldPos = Vector3.zero;
        return false;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    public void SetHeight(float height) => targetHeight = Mathf.Clamp(height, minHeight, maxHeight);
    public float GetTargetFollowHeight() => targetFollowHeight;

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
        Vector3 bl = new Vector3(boundsMin.x, 0f, boundsMin.y);
        Vector3 br = new Vector3(boundsMax.x, 0f, boundsMin.y);
        Vector3 tl = new Vector3(boundsMin.x, 0f, boundsMax.y);
        Vector3 tr = new Vector3(boundsMax.x, 0f, boundsMax.y);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}
