using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameMaster : SingletonNetwork<GameMaster>
{
    [NonSerialized] public List<PlayerController> _players = new();
    [SerializeField] private RoomController _currentRoomController;
    
    public void onPlayerJoined(ulong playerId)
    {
        if(!IsServer) return;
        var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(player!=null&&!_players.Contains(player))
            _players.Add(player);
        if (NetworkManager.ConnectedClientsIds.Count >= 2)
        {
            print("2 or more players joined the game");
            _currentRoomController.Initialize();
        }
    }

    public override void OnNetworkSpawn()
    {
        //if(!IsServer) return;
        //_currentRoomController = FindObjectsOfType<RoomController>();
    }

    private void Start()
    {
        //NetworkManager.OnClientStarted += onPlayerJoined;
        NetworkManager.OnClientConnectedCallback += onPlayerJoined;
    }

    public override void OnDestroy()
    {
        //NetworkManager.OnClientStarted -= onPlayerJoined;
        NetworkManager.OnClientConnectedCallback -= onPlayerJoined;
        base.OnDestroy();
    }
    public void onPlayerDeath(PlayerController deadPlayer)
    {
        if(!IsServer) return;
        
        print("game over for"+NetworkManager.LocalClientId);
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