using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private FactionType targetFaction;

    private EnemyMovement movement;
    private MeleeAttack meleeAttack;
    private Transform currentTarget;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        meleeAttack = GetComponent<MeleeAttack>();
    }

    private void Update()
    {
        if (currentTarget == null)
        {
            currentTarget = FindNearestTarget();
        }

        if (currentTarget == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            currentTarget.position
        );

        if (distance <= meleeAttack.AttackRange)
        {
            movement.Stop();
            meleeAttack.TryAttack();
        }
        else
        {
            movement.MoveTowards(currentTarget.position);
        }
    }

    private Transform FindNearestTarget()
    {
        // target search logic
        Faction[] factions = FindObjectsByType<Faction>();

        Transform closest = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (Faction faction in factions)
        {
            if (faction.Type != targetFaction)
                continue;

            // Optional: only target things that can take damage
            if (!faction.TryGetComponent<IDamageable>(out _))
                continue;

            Vector3 offset = faction.transform.position - transform.position;
            float distanceSqr = offset.sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = faction.transform;
            }
        }

        return closest;
    }
}