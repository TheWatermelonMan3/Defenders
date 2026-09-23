using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;

    public float AttackRange => attackRange;

    private float lastAttackTime = -Mathf.Infinity;
    private Faction faction;

    private void Awake()
    {
        faction = GetComponent<Faction>();
    }

    public void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRange
        );

        foreach (Collider hit in hits)
        {
            // Don't hit yourself.
            if (hit.gameObject == gameObject)
                continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            Faction targetFaction = hit.GetComponent<Faction>();

            if (damageable == null || targetFaction == null)
                continue;

            // No friendly fire.
            if (faction != null && faction.Type == targetFaction.Type)
                continue;

            damageable.TakeDamage(damage);
        }
    }

    // Shows attack range in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}