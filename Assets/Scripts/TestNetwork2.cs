using UnityEngine;
using PurrNet;
using System;
using UnityEngine.InputSystem;

public class TestNetwork2 : NetworkIdentity
{
    [SerializeField] private Color _color;
    [SerializeField] private Renderer _renderer;

    public void setColorWrapper()
    {
        setColor(_color);
    }

    [ObserversRpc(bufferLast:true)]
    public void setColor(Color color)
    {
        _renderer.material.color = color;
    }
}
