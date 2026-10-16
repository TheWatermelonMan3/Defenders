using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves the enemy toward a destination.
/// If the scene has a NavMesh (e.g. from a RuntimeNavMeshBaker), it pathfinds with a NavMeshAgent
/// around walls, up ramps, and around NavBlockers. If not, it falls back to walking in a straight line.
/// The NavMeshAgent is added automatically at runtime, so the prefab doesn't need one.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("Pathfinding")]
    [SerializeField] private bool usePathfinding = true;
    [Tooltip("How far the destination has to move before we ask for a new path.")]
    [SerializeField] private float repathDistance = 0.5f;
    [SerializeField] private float acceleration = 40f;
    [Tooltip("Degrees per second.")]
    [SerializeField] private float turnSpeed = 720f;
    [Tooltip("How far from the NavMesh the enemy can start and still snap onto it.")]
    [SerializeField] private float snapToNavMeshDistance = 2f;

    private NavMeshAgent agent;
    private bool triedToInitAgent;
    private Vector3 lastDestination = new Vector3(float.PositiveInfinity, 0f, 0f);

    /// <summary>True when the enemy is currently moving with the NavMesh instead of a straight line.</summary>
    public bool IsPathfinding => agent != null && agent.enabled && agent.isOnNavMesh;

    public void MoveTowards(Vector3 destination)
    {
        if (usePathfinding && TryGetAgent())
        {
            agent.speed = moveSpeed;
            agent.isStopped = false;

            if ((destination - lastDestination).sqrMagnitude > repathDistance * repathDistance)
            {
                lastDestination = destination;

                // Targets (like the home base) are often a bit above/inside the mesh;
                // aim for the nearest point on the NavMesh instead.
                if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(destination);
            }
            return;
        }

        // Fallback: straight line, staying at our own height.
        destination.y = transform.position.y;
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );
    }

    public void Stop()
    {
        if (IsPathfinding)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // Force a fresh path next time we start moving.
        lastDestination = new Vector3(float.PositiveInfinity, 0f, 0f);
    }

    /// <summary>Adds and places a NavMeshAgent the first time we need one, if there's a NavMesh nearby.</summary>
    private bool TryGetAgent()
    {
        if (IsPathfinding)
            return true;

        if (triedToInitAgent && agent == null)
            return false;
        triedToInitAgent = true;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, snapToNavMeshDistance, NavMesh.AllAreas))
            return false; // no NavMesh here: use straight-line movement

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            ConfigureAgentFromCollider();
        }

        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = turnSpeed;
        agent.stoppingDistance = 0f;
        agent.autoBraking = false;
        agent.Warp(hit.position);

        return agent.isOnNavMesh;
    }

    /// <summary>Sizes the agent to match the enemy's collider so it hugs walls correctly.</summary>
    private void ConfigureAgentFromCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
            return;

        Bounds b = col.bounds;
        agent.radius = Mathf.Max(0.1f, Mathf.Max(b.extents.x, b.extents.z));
        agent.height = Mathf.Max(0.1f, b.size.y);
        // Distance from our pivot down to our feet.
        agent.baseOffset = transform.position.y - b.min.y;
    }

    private void OnDrawGizmosSelected()
    {
        if (agent == null || !agent.hasPath)
            return;

        Gizmos.color = Color.cyan;
        Vector3[] corners = agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
            Gizmos.DrawLine(corners[i], corners[i + 1]);
    }
}
