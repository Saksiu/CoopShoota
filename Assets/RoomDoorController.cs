using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RoomDoorController : NetworkBehaviour
{
    private RoomController owningRoom;
    [Tooltip("0: Up, 1: Right, 2: Down, 3: Left, clockwise from top basically")]
    [SerializeField] private int doorDirection;
    [Header("Target")]
    [SerializeField] private int targetRoomId;
    [Tooltip("0: Up, 1: Right, 2: Down, 3: Left, clockwise from top basically")]
    [SerializeField] private int targetDoorDirection;
    private void Start()
    {
        owningRoom = GetComponentInParent<RoomController>();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        print("door entered");
        var player = col.gameObject.GetComponent<PlayerController>();
        if(player != null&&player.GetComponent<NetworkObject>().IsOwner)
        {
            print($"player {NetworkManager.Singleton.LocalClientId} entered door");
            owningRoom.onDoorEntered(targetRoomId, targetDoorDirection);
        }
    }
}