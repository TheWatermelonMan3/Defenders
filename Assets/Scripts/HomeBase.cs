using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Health))]
public class HomeBase : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Start()
    {
        Debug.Log($"Home base initialized with {health.CurrentHealth} HP.");
    }

    private void Update()
    {
        //if (Keyboard.current.spaceKey.wasPressedThisFrame)
        //{
        //    health.TakeDamage(1);
        //}
    }
}