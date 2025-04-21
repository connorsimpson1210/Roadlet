using UnityEngine;
using System.Collections.Generic;

public class InfiniteDesertRockScatter : MonoBehaviour
{
    [System.Serializable]
    public struct RockEntry
    {
        [Tooltip("The rock prefab to spawn")]
        public GameObject prefab;
        [Tooltip("Relative spawn weight (higher = more common)")]
        [Range(0f, 1f)]
        public float weight;
        [Tooltip("Vertical offset for this rock: positive lifts up, negative sinks down")]
        [Range(-10f, 10f)]
        public float depthOffset;
    }

    [Header("Rock Types & Weights")]
    [Tooltip("List of rock prefabs, each with its own weight and depth offset")]
    public RockEntry[] rockTypes;
    [Tooltip("Number of rocks per chunk")]
    public int rocksPerChunk = 20;

    [Header("Chunk Settings")]
    [Tooltip("Size of each square chunk (meters)")]
    public float chunkSize = 50f;
    [Tooltip("How many chunks in each direction from the camera to spawn")]
    [Min(0)]
    public int spawnRadiusInChunks = 2;

    [Header("Timing & Cleanup")]
    [Tooltip("Auto-destroy rocks when chunks go out of range")]
    private Dictionary<Vector2Int, List<GameObject>> chunkLookup = new Dictionary<Vector2Int, List<GameObject>>();

    [Header("Terrain & Exclusion")]
    [Tooltip("Layer(s) on which the desert ground lives")]
    public LayerMask desertLayer;
    [Tooltip("Layer(s) for exclusion colliders")]
    public LayerMask exclusionLayer;
    [Tooltip("Radius to check around a spawn point for exclusions")]
    public float exclusionCheckRadius = 1f;

    [Header("Placement")]
    [Tooltip("Rotate the model so it stands upright")]
    public Vector3 rotationOffset = new Vector3(-90f, 0f, 0f);
    [Tooltip("Random scale range for spawned rocks")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    [Header("Camera Reference")]
    [Tooltip("If left empty, will use Camera.main")]
    public Transform cameraTransform;

    private Plane groundPlane;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        groundPlane = new Plane(Vector3.up, Vector3.zero);
        UpdateChunks(true);
    }

    void LateUpdate()
    {
        UpdateChunks();
    }

    void UpdateChunks(bool force = false)
    {
        if (cameraTransform == null) return;

        Vector3 camPos = cameraTransform.position;
        Vector2Int center = new Vector2Int(
            Mathf.FloorToInt(camPos.x / chunkSize),
            Mathf.FloorToInt(camPos.z / chunkSize)
        );

        // Determine which chunks should exist
        HashSet<Vector2Int> desired = new HashSet<Vector2Int>();
        for (int dx = -spawnRadiusInChunks; dx <= spawnRadiusInChunks; dx++)
            for (int dz = -spawnRadiusInChunks; dz <= spawnRadiusInChunks; dz++)
                desired.Add(new Vector2Int(center.x + dx, center.y + dz));

        // Spawn any missing
        foreach (var coord in desired)
        {
            if (!chunkLookup.ContainsKey(coord))
                SpawnChunk(coord);
        }

        // Remove any that are too far
        var keys = new List<Vector2Int>(chunkLookup.Keys);
        foreach (var key in keys)
        {
            if (!desired.Contains(key))
            {
                foreach (var go in chunkLookup[key])
                    Destroy(go);
                chunkLookup.Remove(key);
            }
        }
    }

    void SpawnChunk(Vector2Int chunk)
    {
        List<GameObject> list = new List<GameObject>();
        Vector3 baseOffset = new Vector3(chunk.x * chunkSize, 0, chunk.y * chunkSize);

        for (int i = 0; i < rocksPerChunk; i++)
        {
            float rx = Random.Range(0f, chunkSize);
            float rz = Random.Range(0f, chunkSize);
            Vector3 rayOrigin = baseOffset + new Vector3(rx, 100f, rz);

            if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, 200f, desertLayer))
                continue;

            Vector3 pos = hit.point;
            if (Physics.CheckSphere(pos, exclusionCheckRadius, exclusionLayer))
                continue;

            // Pick a rock entry
            RockEntry entry = PickWeightedEntry();
            if (entry.prefab == null) continue;

            // Rotation
            float yaw = Random.Range(0f, 360f);
            Quaternion rot = Quaternion.Euler(rotationOffset.x, yaw, rotationOffset.z);

            // Instantiate
            var go = Instantiate(entry.prefab, pos, rot, transform);

            // Lift to sit on ground + entry depthOffset
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                float halfH = rend.bounds.size.y * 0.5f;
                go.transform.position = pos + Vector3.up * (halfH + entry.depthOffset);
            }
            else
            {
                go.transform.position = pos + Vector3.up * entry.depthOffset;
            }

            // Random scale
            float s = Random.Range(scaleRange.x, scaleRange.y);
            go.transform.localScale *= s;

            list.Add(go);
        }

        chunkLookup[chunk] = list;
    }

    RockEntry PickWeightedEntry()
    {
        float total = 0f;
        foreach (var e in rockTypes) total += e.weight;

        float r = Random.value * total;
        float acc = 0f;
        foreach (var e in rockTypes)
        {
            acc += e.weight;
            if (r <= acc) return e;
        }

        return rockTypes[rockTypes.Length - 1];
    }

    void OnDrawGizmosSelected()
    {
        if (cameraTransform == null) return;
        Vector3 camPos = cameraTransform.position;
        Vector2Int center = new Vector2Int(
            Mathf.FloorToInt(camPos.x / chunkSize),
            Mathf.FloorToInt(camPos.z / chunkSize)
        );
        Vector3 cpos = new Vector3(
            (center.x + 0.5f) * chunkSize,
            camPos.y,
            (center.y + 0.5f) * chunkSize
        );
        Vector3 size = new Vector3(
            (spawnRadiusInChunks * 2 + 1) * chunkSize,
            0,
            (spawnRadiusInChunks * 2 + 1) * chunkSize
        );
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(cpos, size);
    }
}
