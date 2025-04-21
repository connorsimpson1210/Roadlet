using UnityEngine;
using System.Collections.Generic;

public class TelekineticWeaponController : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GameObject weaponPrefab;
    [Tooltip("Particle system prefab or instance for hit effect")]
    public ParticleSystem hitEffect;

    [Header("Hover Settings")]
    public float hoverRadius = 2.0f;
    public float hoverHeight = 1.5f;

    [Header("Direction Source")]
    public bool useMouseDirection = true;

    [Header("Smoothing")]
    public float positionSmoothSpeed = 10f;
    public float defaultRotationSmoothSpeed = 3f;
    public float actionRotationSmoothSpeed = 30f;
    public int actionMouseButton = 0;

    [Header("Swing Scale")]
    public float lengthMultiplier = 0.5f;
    public float maxExtraLength = 1f;
    public float scaleSmoothSpeed = 5f;

    [Header("Swing Duration")]
    public float maxSwingDuration = 1f;

    [Header("Fake Collision")]
    public float sweepRadius = 0.2f;
    public float knockbackMultiplier = 1f;
    public float minKnockback = 1f;
    public float maxKnockback = 10f;
    public LayerMask enemyLayerMask;

    [Header("Orientation")]
    public bool maintainOrientation = true;

    // ───────── Internals ─────────
    private GameObject weaponInstance;
    private Camera mainCamera;
    private Plane groundPlane;
    private float rotationSmoothSpeed;
    private Vector3 baseScale, currentScale;
    private Vector3 prevPosition;
    private float swingTimer;
    private HashSet<Collider> hitThisSwing = new HashSet<Collider>();

    void Start()
    {
        mainCamera = Camera.main;
        if (playerTransform == null) playerTransform = transform;

        if (weaponPrefab != null)
        {
            weaponInstance = Instantiate(weaponPrefab);
            hitEffect = weaponInstance.GetComponentInChildren<ParticleSystem>();
            prevPosition = weaponInstance.transform.position;
            baseScale = weaponInstance.transform.localScale;
            currentScale = baseScale;
        }
        else Debug.LogError("No weaponPrefab assigned!");

        if (hitEffect == null)
            Debug.LogWarning("Hit effect not assigned!");

        groundPlane = new Plane(Vector3.up, playerTransform.position);
        rotationSmoothSpeed = defaultRotationSmoothSpeed;
    }

    void LateUpdate()
    {
        if (weaponInstance == null) return;

        // ← here we declare isAction
        bool isAction = Input.GetMouseButton(actionMouseButton);

        // ── 0) Toggle hit effect ──
        if (hitEffect != null)
        {
            bool withinSwing = swingTimer < maxSwingDuration;
            bool shouldPlay = isAction && withinSwing;

            if (shouldPlay && !hitEffect.isPlaying)
            {
                hitEffect.Play();
                
            }
            else if (!shouldPlay && hitEffect.isPlaying)
            {
                hitEffect.Stop();
                
            }
        }

        // ── 1) Rotation smoothing ──
        rotationSmoothSpeed = isAction
            ? actionRotationSmoothSpeed
            : defaultRotationSmoothSpeed;

        // ── 2) Hover direction & movement ──
        Vector3 hoverDir = GetHoverDirection();
        Transform wt = weaponInstance.transform;

        Vector3 desiredPos = playerTransform.position
                           + hoverDir * hoverRadius
                           + Vector3.up * hoverHeight;
        wt.position = Vector3.Lerp(
            wt.position,
            desiredPos,
            positionSmoothSpeed * Time.deltaTime
        );

        if (maintainOrientation)
        {
            Quaternion targetRot = Quaternion.LookRotation(hoverDir, Vector3.up);
            wt.rotation = Quaternion.Slerp(
                wt.rotation,
                targetRot,
                rotationSmoothSpeed * Time.deltaTime
            );
        }

        // ── 3) Swing & collisions ──
        if (isAction && swingTimer < maxSwingDuration)
        {
            if (swingTimer == 0f) hitThisSwing.Clear();
            swingTimer += Time.deltaTime;
            UpdateSwingScale();
            FakeCollisionSweep(prevPosition, wt.position);
        }
        else
        {
            if (!isAction) swingTimer = 0f;
            RetractScale();
        }

        prevPosition = wt.position;
    }

    Vector3 GetHoverDirection()
    {
        if (!useMouseDirection)
            return Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector3 dir = hit - playerTransform.position;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up);
            return dir.normalized;
        }
        return Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
    }

    void UpdateSwingScale()
    {
        Transform wt = weaponInstance.transform;
        Vector3 delta = wt.position - prevPosition;
        float speed = delta.magnitude / Time.deltaTime;
        float extra = Mathf.Min(speed * lengthMultiplier, maxExtraLength);

        Vector3 targetScale = baseScale;
        targetScale.z += extra;

        currentScale = Vector3.Lerp(
            currentScale,
            targetScale,
            scaleSmoothSpeed * Time.deltaTime
        );
        wt.localScale = currentScale;
    }

    void RetractScale()
    {
        Transform wt = weaponInstance.transform;
        currentScale = Vector3.Lerp(
            currentScale,
            baseScale,
            scaleSmoothSpeed * Time.deltaTime
        );
        wt.localScale = currentScale;
    }

    void FakeCollisionSweep(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist < Mathf.Epsilon) return;
        dir.Normalize();

        Ray ray = new Ray(start, dir);
        var hits = Physics.SphereCastAll(ray, sweepRadius, dist, enemyLayerMask);

        foreach (var hit in hits)
        {
            Collider col = hit.collider;
            if (!hitThisSwing.Contains(col))
            {
                hitThisSwing.Add(col);

                float speed = dist / Time.deltaTime;
                float rawKb = speed * knockbackMultiplier;
                float knockback = Mathf.Clamp(rawKb, minKnockback, maxKnockback);

                if (col.attachedRigidbody != null)
                    col.attachedRigidbody.AddForce(dir * knockback, ForceMode.Impulse);
            }
        }
    }

    public void ReleaseWeapon()
    {
        if (weaponInstance != null)
            Destroy(weaponInstance);
    }
}
