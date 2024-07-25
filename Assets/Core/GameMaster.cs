using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class GameMaster : SingletonNetwork<GameMaster>
{
    [SerializeField] private int minPlayers = 1;
    
    public List<PlayerController> _players = new();
    [SerializeField] private RoomController runRoomController;
    [SerializeField] private List<Transform> spawnPoints;


    public Action<PlayerController> OnPlayerSpawned;

    public float respawnTime = 3f;

    public override void Awake()
    {
        base.Awake();
        //Cursor.lockState = CursorLockMode.Locked;
    }

    public void onPlayerJoined(ulong playerId)
    {
        print("onPlayerJoined "+playerId+" ");

        if(!IsServer) return;
        print("total players joined: "+NetworkManager.ConnectedClientsIds.Count);
        //print(NetworkManager.ConnectedClients[playerId].PlayerObject.name);
        
        var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(player==null||_players.Contains(player))
            return;
        
        _players.Add(player);
        
        
        OnPlayerSpawned?.Invoke(player);
        setPlayerPositionClientRpc(playerId,spawnPoints[_players.Count-1].position);


        player.healthComponent.OnDeathAction += OnPlayerDeath;
        
        if (NetworkManager.ConnectedClientsIds.Count >= minPlayers)
            onAllPlayersJoined();
    }

    public void updateSpawnPoints(List<Transform> newSpawnPoints)
    {
        spawnPoints = newSpawnPoints;
    }

    public void onPlayerLeft(ulong playerId)
    {
        if(!IsServer) return;
        
        var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(player!=null&&_players.Contains(player))
            _players.Remove(player);

        player.healthComponent.OnDeathAction -= OnPlayerDeath;

        if(!player.IsSpawned) return;

        NetworkManager.DisconnectClient(playerId);
        player.NetworkObject.Despawn();
        DestroyPlayerObjectClientRpc(playerId);
        
    }

    [ClientRpc]
    private void DestroyPlayerObjectClientRpc(ulong playerId){
        if(NetworkManager.LocalClientId!=playerId) return;

        Destroy(NetworkManager.LocalClient.PlayerObject);
        Destroy(NetworkManager.gameObject);
    }

    [ClientRpc]
    public void setPlayerPositionClientRpc(ulong playerID,Vector3 pos,ClientRpcParams clientRpcParams=default)
    {
        print("received position update for player " + playerID + " on client " + NetworkManager.LocalClientId+"is owner "+IsOwner);
        
        //if(!IsOwner) return;
        if(NetworkManager.LocalClientId!=playerID) return;
        
        
        NetworkManager.LocalClient.PlayerObject.transform.position = pos;
        //transform.position = pos;
    }

    [ClientRpc]
    public void setPlayerPositionRandomSpawnPointClientRpc(ulong playerID,ClientRpcParams clientRpcParams=default)
    {
        if(runRoomController.isRunActive)
            setPlayerPositionClientRpc(playerID,runRoomController.spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)].position);
        else
            setPlayerPositionClientRpc(playerID,spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)].position);
    }

    private void onAllPlayersJoined()
    {
        print("2 or more players joined the game "+_players.ToArray());
        //probably unlock all game systems outside of the lobby
    }
    public void onRunInit(){
        runRoomController.InitRoom.Invoke();
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.OnClientConnectedCallback += onPlayerJoined;
        NetworkManager.OnClientDisconnectCallback += onPlayerLeft;
        /*if (!IsServer)
        {
            
        }*/
        //_currentRoomController = FindObjectsOfType<RoomController>();
    }

    public void OnPlayerDeath(PlayerController deadPlayer)
    {
        Assert.IsTrue(IsServer);
        print("game over for "+deadPlayer.playerName.Value);

        StartCoroutine(PlayerRespawn(deadPlayer));

    }

    private IEnumerator PlayerRespawn(PlayerController player)
    {
        Assert.IsTrue(IsServer);
        
        yield return new WaitForSeconds(respawnTime);
        player.healthComponent.resetHPServerRpc();
        setPlayerPositionRandomSpawnPointClientRpc(player.OwnerClientId);
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.OnClientConnectedCallback -= onPlayerJoined;
        NetworkManager.OnClientDisconnectCallback -= onPlayerLeft;
    }

    public override void OnDestroy()
    {
        //NetworkManager.OnClientStarted -= onPlayerJoined;
        
        base.OnDestroy();
    }
    
    private void beginShutDown()
    {
        if (IsServer)
            StartCoroutine(HostShutdown());
        else
            Shutdown();
    }
    
    private IEnumerator HostShutdown()
    {
        // Tell all clients to shutdown
        ShutdownClientRpc();

        // Wait some time for the message to get to clients
        yield return new WaitForSeconds(0.5f);

        // Shutdown server/host
        Shutdown();
    }
    
    [ClientRpc]
    private void ShutdownClientRpc()
    {
        if(!IsOwner) return;
        Shutdown();
    }
    private void Shutdown()
    {
        NetworkManager.Singleton.Shutdown();
    }
}