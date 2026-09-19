using UnityEngine;
using PurrNet;
using System;

public class TestNetwork : NetworkIdentity
{
    [SerializeField] private NetworkIdentity _networkIdentity;

    // Awake is called once before the first frame that the object exists
    private void Awake()
    {
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (!asServer) return;
        Instantiate(_networkIdentity, Vector3.zero, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
