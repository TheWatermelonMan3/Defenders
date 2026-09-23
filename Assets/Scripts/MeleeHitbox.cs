using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    private int damage;

    public void Initialize(int damageAmount)
    {
        damage = damageAmount;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }
    }
}