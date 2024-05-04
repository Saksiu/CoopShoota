using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraController : SingletonLocal<CameraController>
{
    public Camera Camera;

    private float zOffset = -10;
    public override void Awake()
    {
        base.Awake();
        print("CameraController Awake");
        zOffset = Camera.transform.position.z;
    }

    public void changeCameraPos(Vector2 pos)
    {
        Camera.transform.position = new Vector3(pos.x,pos.y, zOffset);
    }
}
