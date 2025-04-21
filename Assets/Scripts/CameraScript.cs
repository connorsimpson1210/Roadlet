using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScript : MonoBehaviour
{
    // ───────────── Inspector References ─────────────
    [Header("References")]
    public Transform player;      // drag the player root here
    

    // ───────────── Base Offset & Follow ─────────────
    public Vector3 offset = new Vector3(10f, 15f, -10f);
    public float smoothSpeed = 0.125f;

    [Header("Mouse‑Driven X Offset (vertical mouse)")]
    public float minX = -10f;
    public float maxX = -50f;
    public float xSmoothTime = 0.2f;

    [Header("Mouse‑Driven Z Offset (horizontal mouse)")]
    public float minZ = -20f;
    public float maxZ = 20f;
    public float zSmoothTime = 0.2f;

    [Header("Scroll‑Adjustable Base Zoom")]
    public float minBaseSize = 8f;
    public float maxBaseSize = 12f;
    public float scrollSensitivity = 1f;
    public float scrollSmoothTime = 0.2f;

    [Header("Speed‑Based Zoom (only when following car)")]
    public float maxSpeedSize = 15f;
    public float zoomMaxSpeed = 20f;
    public float speedZoomSmoothTime = 0.2f;

    [Header("Absolute Zoom Limit")]
    public float absoluteMaxSize = 20f;

    // ───────────── Internals ─────────────
    Transform target;          // current follow target
    Camera cam;
    

    float currentX, currentZ, xVel, zVel;
    float baseSizeTarget, baseSizeCurrent, baseSizeVel;
    float camSizeCurrent, camSizeVel;

    // ───────────── Public API for CarScript ─────────────
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        
    }

    // ───────────── Unity Events ─────────────
    void Awake()
    {
        cam = GetComponent<Camera>();
        SetTarget(player);
        

        // Initial offset & zoom state
        currentX = offset.x;
        currentZ = offset.z;

        baseSizeTarget = Mathf.Clamp(minBaseSize, minBaseSize, maxBaseSize);
        baseSizeCurrent = baseSizeTarget;
        camSizeCurrent = baseSizeTarget;
        ApplyCameraSize(camSizeCurrent);
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // ── 1) Mouse offsets ──
        float tY = Mathf.Clamp01(Input.mousePosition.y / (float)Screen.height);
        float newX = Mathf.Lerp(minX, maxX, tY);
        currentX = Mathf.SmoothDamp(currentX, newX, ref xVel, xSmoothTime);

        float tX = Mathf.Clamp01(Input.mousePosition.x / (float)Screen.width);
        float newZ = Mathf.Lerp(minZ, maxZ, tX);
        currentZ = Mathf.SmoothDamp(currentZ, newZ, ref zVel, zSmoothTime);

        // ── 2) Scroll‑wheel base zoom ──
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            baseSizeTarget = Mathf.Clamp(
                baseSizeTarget - scroll * scrollSensitivity,
                minBaseSize, maxBaseSize
            );
        }
        baseSizeCurrent = Mathf.SmoothDamp(
            baseSizeCurrent,
            baseSizeTarget,
            ref baseSizeVel,
            scrollSmoothTime
        );

        // Apply zoom directly (no speed‑based adjustments)
        camSizeCurrent = baseSizeCurrent;
        ApplyCameraSize(camSizeCurrent);

        // ── 4) Follow position & look ──
        Vector3 dynOffset = new Vector3(currentX, offset.y, currentZ);
        Vector3 desiredPos = target.position + dynOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
        transform.LookAt(target);
    }


    void ApplyCameraSize(float size)
    {
        if (cam.orthographic) cam.orthographicSize = size;
        else cam.fieldOfView = size;
    }
}