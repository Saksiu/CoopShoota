using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameMaster : SingletonNetwork<GameMaster>
{
    public LinkedList<PlayerController> _players = new();
    [SerializeField] private RoomController _currentRoomController;
    
    public void onPlayerJoined(ulong playerId)
    {
        print("onPlayerJoined "+playerId+" ");
        if(!IsServer) return;
        var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(player==null||_players.Contains(player))
            return;
        _players.AddLast(player);
        
        
        
        //player.setName("", "P"+player.OwnerClientId.ToString());
        if (NetworkManager.ConnectedClientsIds.Count >= 2)
            InitGame();
    }
    public void onPlayerLeft(ulong playerId)
    {
        if(!IsServer) return;
        
        var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(player!=null&&_players.Contains(player))
            _players.Remove(player);
        
        if(NetworkObject.IsSpawned)
            NetworkObject.Despawn();

    }

    private void InitGame()
    {
        print("2 or more players joined the game "+_players.ToArray());

        foreach (var p in _players)
        {
            p.playerName.Value = "P"+p.OwnerClientId.ToString();
            //p.healthComponent.HP.Value = p.healthComponent.maxHP;
        }
            
        /*foreach (var player in _players)
        {
            player.setName("","P"+player.OwnerClientId.ToString());
        }*/
        //replicatePlayerNamesClientRpc();
        _currentRoomController.Initialize();
    }

    public override void OnNetworkSpawn()
    {
        /*if (!IsServer)
        {
            
        }*/
        //_currentRoomController = FindObjectsOfType<RoomController>();
    }

    private void Start()
    {
        /*if (!IsOwnedByServer)
        {
            enabled = false;
            return;
        }*/
        //NetworkManager.OnClientStarted += onPlayerJoined;
        NetworkManager.OnClientConnectedCallback += onPlayerJoined;
        NetworkManager.OnClientDisconnectCallback += onPlayerLeft;
        
    }

    public override void OnDestroy()
    {
        //NetworkManager.OnClientStarted -= onPlayerJoined;
        NetworkManager.OnClientConnectedCallback -= onPlayerJoined;
        NetworkManager.OnClientDisconnectCallback -= onPlayerLeft;
        base.OnDestroy();
    }
    public void onPlayerDeath(PlayerController deadPlayer)
    {
        if(!IsServer) return;
        print("game over for "+deadPlayer.playerName.Value);
        if(NetworkObject.IsSpawned)
            NetworkObject.Despawn();

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