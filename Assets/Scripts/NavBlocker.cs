using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Attach to any object to make enemies path around it.
/// At runtime it adds a carving NavMeshObstacle sized to the object's colliders,
/// which cuts a hole in the NavMesh live, so it works for things placed mid-game (like towers)
/// and needs no re-bake. The object itself is not changed in the editor.
/// </summary>
[DisallowMultipleComponent]
public class NavBlocker : MonoBehaviour
{
    [Tooltip("Extra space (meters) to keep clear around the object.")]
    [SerializeField] private float padding = 0.1f;

    [Tooltip("Only re-carve once the object stops moving. Keep on unless the object moves constantly.")]
    [SerializeField] private bool carveOnlyWhenStationary = true;

    private NavMeshObstacle obstacle;

    private void Awake()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null)
            return; // someone already set one up by hand; respect it

        if (!TryGetLocalBounds(out Bounds local))
        {
            Debug.LogWarning($"{name}: NavBlocker found no collider or renderer to size itself from.", this);
            return;
        }

        obstacle = gameObject.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = local.center;

        // padding is in world meters; obstacle size is in local space.
        Vector3 scale = transform.lossyScale;
        Vector3 pad = new Vector3(
            padding * 2f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            0f,
            padding * 2f / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        obstacle.size = local.size + pad;

        obstacle.carving = true;
        obstacle.carveOnlyStationary = carveOnlyWhenStationary;
    }

    /// <summary>Bounds of all child colliders (or renderers as a fallback), in this object's local space.</summary>
    private bool TryGetLocalBounds(out Bounds local)
    {
        local = default;
        bool found = false;

        // A single BoxCollider on this object is the common case: use it exactly.
        if (TryGetComponent(out BoxCollider box) && GetComponentsInChildren<Collider>().Length == 1)
        {
            local = new Bounds(box.center, box.size);
            return true;
        }

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger) continue;
            Encapsulate(ref local, ref found, c.bounds);
        }

        if (!found)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                Encapsulate(ref local, ref found, r.bounds);
        }

        return found;
    }

    private void Encapsulate(ref Bounds local, ref bool found, Bounds world)
    {
        // Convert the 8 corners of the world box into local space.
        Vector3 min = world.min, max = world.max;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z);
            Vector3 p = transform.InverseTransformPoint(corner);

            if (!found) { local = new Bounds(p, Vector3.zero); found = true; }
            else local.Encapsulate(p);
        }
    }
}
