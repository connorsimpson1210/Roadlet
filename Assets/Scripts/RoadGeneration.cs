using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns road segments infinitely along the X axis relative to the camera (or a target).
/// Segments are recycled/despawned when out of range.
/// </summary>
[RequireComponent(typeof(Transform))]
public class InfiniteRoadGenerator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Flat road segment prefab to spawn")]
    public GameObject roadPrefab;
    [Tooltip("Transform to follow (e.g. your camera or car)")]
    public Transform followTarget;

    [Header("Segment Settings")]
    [Tooltip("Spacing between each road segment along X")]
    public float segmentSpacing = 15f;
    [Tooltip("How many segments to keep ahead of the target")]
    public int segmentsAhead = 10;
    [Tooltip("How many segments to keep behind the target")]
    public int segmentsBehind = 5;

    // Tracks which segments are spawned at which index
    private Dictionary<int, GameObject> segments = new Dictionary<int, GameObject>();

    void Start()
    {
        if (roadPrefab == null)
        {
            Debug.LogError("InfiniteRoadGenerator: roadPrefab not assigned!");
            enabled = false;
            return;
        }

        if (followTarget == null && Camera.main != null)
            followTarget = Camera.main.transform;

        // initial spawn
        UpdateSegments();
    }

    void LateUpdate()
    {
        UpdateSegments();
    }

    private void UpdateSegments()
    {
        if (followTarget == null) return;

        // Determine the center index based on target's X position
        float tx = followTarget.position.x;
        int centerIndex = Mathf.FloorToInt(tx / segmentSpacing);

        int start = centerIndex - segmentsBehind;
        int end = centerIndex + segmentsAhead;

        // Spawn missing
        for (int i = start; i <= end; i++)
        {
            if (!segments.ContainsKey(i))
                SpawnSegment(i);
        }

        // Remove out-of-range
        var keys = new List<int>(segments.Keys);
        foreach (var idx in keys)
        {
            if (idx < start || idx > end)
            {
                Destroy(segments[idx]);
                segments.Remove(idx);
            }
        }
    }

    private void SpawnSegment(int index)
    {
        Vector3 pos = new Vector3(index * segmentSpacing,
                                  transform.position.y,
                                  transform.position.z);
        GameObject go = Instantiate(roadPrefab, pos, Quaternion.identity, transform);
        go.name = $"RoadSegment_{index}";
        segments[index] = go;
    }
}
