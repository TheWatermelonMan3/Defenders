using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drop one of these in a scene and it builds a NavMesh from the scene's colliders when the scene starts.
/// Nothing in the level needs to be marked or changed. Ramps and steps work as long as they fit
/// the slope / step limits below.
///
/// Ignored automatically: triggers, enemies, players (CharacterController), physics objects
/// (non-kinematic Rigidbody), and NavBlockers (those carve their own holes at runtime).
/// </summary>
[DefaultExecutionOrder(-1000)] // bake before anything else's Awake/Start
public class RuntimeNavMeshBaker : MonoBehaviour
{
    [Header("Agent size (match the enemies)")]
    [SerializeField] private float agentRadius = 0.5f;
    [SerializeField] private float agentHeight = 2f;

    [Header("What enemies can climb")]
    [Tooltip("Steepest walkable slope in degrees. PlayerMovement uses 45.")]
    [SerializeField, Range(0f, 60f)] private float maxSlope = 45f;
    [Tooltip("Tallest step enemies can walk up. PlayerMovement uses 0.3.")]
    [SerializeField, Min(0f)] private float stepHeight = 0.3f;

    [Header("Bake")]
    [SerializeField] private LayerMask includedLayers = ~0;
    [SerializeField] private bool bakeOnAwake = true;
    [Tooltip("Smaller = more accurate around small gaps but slower. 0 = automatic (radius / 3).")]
    [SerializeField, Min(0f)] private float voxelSize = 0f;

    private NavMeshDataInstance navMeshInstance;

    private void Awake()
    {
        if (bakeOnAwake)
            Bake();
    }

    private void OnDestroy()
    {
        navMeshInstance.Remove();
    }

    [ContextMenu("Bake Now")]
    public void Bake()
    {
        navMeshInstance.Remove();

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0); // default "Humanoid" agent type
        settings.agentRadius = agentRadius;
        settings.agentHeight = agentHeight;
        settings.agentSlope = maxSlope;
        settings.agentClimb = stepHeight;
        if (voxelSize > 0f)
        {
            settings.overrideVoxelSize = true;
            settings.voxelSize = voxelSize;
        }

        if (!TryGetLevelBounds(out Bounds bounds))
        {
            Debug.LogWarning("[RuntimeNavMeshBaker] No colliders found to bake.", this);
            return;
        }

        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            bounds, includedLayers, NavMeshCollectGeometry.PhysicsColliders,
            0, new List<NavMeshBuildMarkup>(), sources);

        sources.RemoveAll(s => ShouldIgnore(s.component));

        NavMeshData data = NavMeshBuilder.BuildNavMeshData(
            settings, sources, bounds, Vector3.zero, Quaternion.identity);

        if (data == null)
        {
            Debug.LogWarning("[RuntimeNavMeshBaker] Bake failed.", this);
            return;
        }

        navMeshInstance = NavMesh.AddNavMeshData(data);
        Debug.Log($"[RuntimeNavMeshBaker] Baked NavMesh from {sources.Count} colliders.", this);
    }

    private bool TryGetLevelBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Collider c in FindObjectsByType<Collider>())
        {
            if (ShouldIgnore(c) || (includedLayers.value & (1 << c.gameObject.layer)) == 0)
                continue;

            if (!found) { bounds = c.bounds; found = true; }
            else bounds.Encapsulate(c.bounds);
        }

        if (found)
            bounds.Expand(agentHeight * 2f);
        return found;
    }

    private static bool ShouldIgnore(Component c)
    {
        if (c == null)
            return false;

        if (c is Collider col && col.isTrigger)
            return true;
        if (c is CharacterController || c.GetComponentInParent<CharacterController>() != null)
            return true; // players
        if (c.GetComponentInParent<EnemyMovement>() != null)
            return true; // enemies
        if (c.GetComponentInParent<NavMeshAgent>() != null)
            return true;
        if (c.GetComponentInParent<NavBlocker>() != null)
            return true; // carved at runtime instead

        Rigidbody rb = c.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
            return true; // things that get knocked around

        return false;
    }
}
