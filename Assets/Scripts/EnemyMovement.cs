using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    public void MoveTowards(Vector3 destination)
    {
        if (transform == null)
            return;
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );
    }

    public void Stop()
    {
        // stop movement
    }
}