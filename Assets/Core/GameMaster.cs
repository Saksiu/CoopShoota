using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class GameMaster : SingletonNetwork<GameMaster>
{
    [SerializeField] private int minPlayers = 1;

    public List<PlayerController> getConnectedPlayers(){
        Assert.IsTrue(IsServer,"getConnectedPlayers called on client");
        
        return playersDict.Values.ToList();
    }

    private Dictionary<ulong,PlayerController> playersDict = new();

    [SerializeField] private List<Transform> spawnPoints;

    private Dictionary<ulong,Transform> assignedSpawnPoints = new();


    public override void OnNetworkSpawn()
    {
        ArenaManager.OnRunStartAction += OnRunStarted;
        ArenaManager.OnRunEndAction += endRun;
        
        if(!IsServer) return;

        NetworkManager.OnClientConnectedCallback += onPlayerJoined;
        NetworkManager.OnClientDisconnectCallback += onPlayerLeft;
    }

    
    public override void OnNetworkDespawn()
    {
        ArenaManager.OnRunStartAction -= OnRunStarted;
        ArenaManager.OnRunEndAction -= endRun;

        if(!IsServer) return;

        NetworkManager.OnClientConnectedCallback -= onPlayerJoined;
        NetworkManager.OnClientDisconnectCallback -= onPlayerLeft;
    }

    public void printServerFoundMessage(IPEndPoint endPoint, DiscoveryResponseData data)
    {
        print("Server found at "+endPoint+" with data "+data);
    }

    public float respawnTime = 3f;

    public void onPlayerJoined(ulong playerId)
    {
        print("onPlayerJoined "+playerId+" ");

        if(!IsServer) return;
        if(playersDict.ContainsKey(playerId)) return;

        
        //print(NetworkManager.ConnectedClients[playerId].PlayerObject.name);
        
        var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(player!=null)
            playersDict.Add(playerId,player);

        assignedSpawnPoints.Add(playerId,spawnPoints[playersDict.Count-1]);
        //_players.Add(player);
        
        print("total players joined: "+NetworkManager.ConnectedClientsIds.Count);

        //OnPlayerSpawned?.Invoke(player);
        setPlayerPositionClientRpc(playerId,assignedSpawnPoints[playerId].position);


        player.healthComponent.OnDeathAction += OnPlayerDeath;
        
        //if (NetworkManager.ConnectedClientsIds.Count >= minPlayers)
        //    onAllPlayersJoined();
        //onPlayerSpawned?.Invoke();
    }

    public void updateSpawnPoints(List<Transform> newSpawnPoints)
    {
        spawnPoints = newSpawnPoints;
    }

    public void onPlayerLeft(ulong playerId)
    {
        print("onPlayerLeft "+playerId);
        if(!IsServer) return;
        //onPlayerDespawned?.Invoke();
    

        //player.healthComponent.OnDeathAction -= OnPlayerDeath;

        //if(!player.IsSpawned) return;

        //NetworkManager.DisconnectClient(playerId);
        if(playersDict.ContainsKey(playerId)){
            playersDict[playerId].NetworkObject.Despawn();
            //DestroyPlayerObjectClientRpc(playerId);
            playersDict.Remove(playerId);
            assignedSpawnPoints.Remove(playerId);
        }
        
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


    
    public void OnRunStarted(){
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="win"></param>
    public void endRun(bool win){
        resetAllPlayersServerRpc();
        displayPromptForAllPlayersClientRpc(win?"You win!":"You lose!",3f);
    }

    [ServerRpc]
    private void resetAllPlayersServerRpc(){
        foreach(var player in playersDict.Values){
            player.healthComponent.resetHPServerRpc();
            setPlayerPositionClientRpc(player.OwnerClientId,getPlayerSpawnPosition(player.OwnerClientId));
        }
    }

    private Vector3 getPlayerSpawnPosition(ulong playerId)=>assignedSpawnPoints[playerId].position;

    [ClientRpc]
    public void displayPromptForAllPlayersClientRpc(string message,float duration){
        HUDManager.Instance.showPromptFor(message,duration);
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
        setPlayerPositionClientRpc(player.OwnerClientId,getPlayerSpawnPosition(player.OwnerClientId));
    }


    //TODO: handle host/client disconnects here probably
    public void ExitGame(){
        PlayerSessionManager.Instance.beginShutDown(exitGame: true);
        //Application.Quit();
    }
    public override void OnDestroy()
    {
        //NetworkManager.OnClientStarted -= onPlayerJoined;
        
        base.OnDestroy();
    }
    
    /*public void beginShutDown(bool exitGame=false)
    {
        //caller.
        //print("beginshutdown called for "+caller.playerName.Value);
        if (IsServer)
            StartCoroutine(HostShutdown(exitGame));
        else
            Shutdown(exitGame);
    }
    
    private IEnumerator HostShutdown(bool exitGame)
    {
        // Tell all clients to shutdown
        ShutdownClientRpc(false);

        // Wait some time for the message to get to clients
        yield return new WaitForSeconds(0.5f);

        // Shutdown server/host
        Shutdown(exitGame);
    }
    
    [ClientRpc]
    private void ShutdownClientRpc(bool exitGame)
    {
        if(!IsOwner) return;
        //if(IsHost) return;

        Shutdown(exitGame);
    }
    private void Shutdown(bool exitGame)
    {
        print("Shutdown called");
        NetworkManager.Singleton.Shutdown();
        if(exitGame){Application.Quit();}
        else{
            NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>().StopDiscovery();
            SceneManager.LoadSceneAsync("PlayScene");
           // MainMenuManager.Instance.enableMainMenu();
        }
            
    }*/
}