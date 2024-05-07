using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoomController : NetworkBehaviour
{
    [SerializeField] private EnemySpawnerController enemySpawner;
    //[Tooltip("0: Up, 1: Right, 2: Down, 3: Left, clockwise from top basically")]
    //[SerializeField] private RoomDoorController[] doors = new RoomDoorController[4];

    private static uint _nextId = 0;
    public uint Id;
    private static Dictionary<uint,RoomController> _allRooms = new();
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

    public void onDoorEntered(uint targetRoomID,uint targetDoorDirection)
    {
        //if(IsServer||!IsOwner) return;
        //enemySpawner?.StopSpawningEnemies();
        RequestRoomChangeServerRpc(targetRoomID,targetDoorDirection);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestRoomChangeServerRpc(uint targetRoomID,uint targetDoorDirection, ServerRpcParams serverRpcParams = default)
    {
        RoomController targetRoom = tryGetRoomById(targetRoomID);
        if(targetRoom==null) return;
        
        
        targetRoom.enabled = true;
        targetRoom.Initialize();
        
        Vector2 spawnPoint = targetRoom.tryGetDoorSpawnPoint(targetDoorDirection);
        if(spawnPoint==Vector2.zero) return;
        
        print("initialized room "+targetRoomID);

        // Prepare the ClientRpcParams to target the specific client.
        var clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }}};
    
        RoomChangeClientRpc(targetRoomID, spawnPoint, clientRpcParams); // Correctly targets the client.
        //RoomChangeClientRpc(targetRoomID);
    }

    [ClientRpc]
    private void RoomChangeClientRpc(uint targetRoomID,Vector2 spawnPoint, ClientRpcParams rpcParams = default)
    {
        //if(!IsOwner) return;
        print("received room change order from server P"+NetworkManager.LocalClientId);
        //ChangeRoom(targetRoomID,targetDoorDirection);
        
        RoomController targetRoom = tryGetRoomById(targetRoomID);
        if(targetRoom==null) return;
        
        //CameraController.Instance.changeCameraPos(targetRoom.cameraTargetPos.position);
        NetworkManager.LocalClient.PlayerObject.transform.position = spawnPoint;
        
    }
/*
    private void ChangeRoom(uint targetRoomID,Vector2 spawnPoint)
    {
        //if(!IsOwner) return;
    }
*/
    private Vector2 tryGetDoorSpawnPoint(uint targetDirection)
    {
        /*Vector2 spawnPoint = Vector2.zero;
        if(doors[targetDirection].transform.GetChild(0)!=null)
            spawnPoint=doors[targetDirection].transform.GetChild(0).position;
        return spawnPoint;*/
        return Vector2.zero;
    }
    private RoomController tryGetRoomById(uint id)
    {
        return _allRooms.TryGetValue(id, out var room) ? room : null;
    }

    private static void registerNewRoom(RoomController toAdd)
    {
        _allRooms.Add(toAdd.Id,toAdd);
    }
}

