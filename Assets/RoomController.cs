using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RoomController : NetworkBehaviour
{
    [SerializeField] private EnemySpawnerController enemySpawner;
    [SerializeField] private Transform cameraTargetPos;
    [Tooltip("0: Up, 1: Right, 2: Down, 3: Left, clockwise from top basically")]
    [SerializeField] private RoomDoorController[] doors = new RoomDoorController[4];

    private static int _nextId = 0;
    public int Id;
    private static List<RoomController> _allRooms = new List<RoomController>();
    public void Initialize()
    {
        enemySpawner?.BeginSpawningEnemies();
    }
    
    /*public override void OnNetworkSpawn()
    {
        Id = _nextId;
        _nextId++;
        GetComponentInChildren<TextMeshPro>().text = Id.ToString();
    }*/

    private void Start()
    {
        registerNewRoom(this);
        this.enabled = false;
    }

    public void onDoorEntered(int targetRoomID,int targetDoorDirection)
    {
        //if(IsServer||!IsOwner) return;
        enemySpawner?.StopSpawningEnemies();
        RequestRoomChangeServerRpc(targetRoomID,targetDoorDirection);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestRoomChangeServerRpc(int targetRoomID,int targetDoorDirection, ServerRpcParams serverRpcParams = default)
    {
        // Prepare the ClientRpcParams to target the specific client.
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
            }
        };
    
        RoomChangeClientRpc(targetRoomID, targetDoorDirection, clientRpcParams); // Correctly targets the client.
        //RoomChangeClientRpc(targetRoomID);
    }

    [ClientRpc]
    private void RoomChangeClientRpc(int targetRoomID,int targetDoorDirection, ClientRpcParams rpcParams = default)
    {
        //if(!IsOwner) return;
        print("received room change order from server"+NetworkManager.LocalClientId);
        ChangeRoom(targetRoomID,targetDoorDirection);
    }

    private void ChangeRoom(int targetRoomID,int targetDoorDirection)
    {
        //if(!IsOwner) return;
        
        RoomController targetRoom = getRoomById(targetRoomID);
        if(targetRoom==null) return;
        
        
        targetRoom.enabled = true;
        targetRoom.Initialize();
        
        Vector2 spawnPoint = targetRoom.getDoorSpawnPoint(targetDoorDirection);
        if(spawnPoint==Vector2.zero) return;
        
        CameraController.Instance.changeCameraPos(targetRoom.cameraTargetPos.position);
        NetworkManager.LocalClient.PlayerObject.transform.position = spawnPoint;
        
        
        print("initialized room "+targetRoomID);
        
    }

    private Vector2 getDoorSpawnPoint(int targetDirection)
    {
        Vector2 spawnPoint = Vector2.zero;
        if(doors[targetDirection].transform.GetChild(0)!=null)
            spawnPoint=doors[targetDirection].transform.GetChild(0).position;
        
        return spawnPoint;
    }
    private RoomController getRoomById(int id)
    {
        foreach (var room in _allRooms)
        {
            if(room.Id==id)
                return room;
        }
        UnityException e = new UnityException("Room with id "+id+" not found");
        return null;
    }

    public static void registerNewRoom(RoomController toAdd)
    {
        if(!_allRooms.Contains(toAdd))
            _allRooms.Add(toAdd);
    }
}
