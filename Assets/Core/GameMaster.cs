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
    
    public List<PlayerController> _players = new();

    public PlayerController getPlayer(ulong playerId)
    {
        return _players.Find(player => player.OwnerClientId == playerId);
    }
    //[SerializeField] private RoomController runRoomController;
    [SerializeField] private List<Transform> spawnPoints;


    //public Action<PlayerController> OnPlayerSpawned;

    //public static Action onPlayerSpawned;
    //public static Action onPlayerDespawned;

    public void printServerFoundMessage(IPEndPoint endPoint, DiscoveryResponseData data)
    {
        print("Server found at "+endPoint+" with data "+data);
    }

    public float respawnTime = 3f;

    //private NetworkManager initialNetworkManager;
    public override void Awake()
    {
        //we REALLY probably don't wanna do this
        /*if(initialNetworkManager==null)
            initialNetworkManager = NetworkManager.Singleton;
        FindObjectsOfType<NetworkManager>().ToList().ForEach(nm => {
            if(nm!=initialNetworkManager){
                print("found duplicate network manager "+nm.gameObject.name+" destroying it");
                Destroy(nm.gameObject);
            }
        });
        initialNetworkManager.SetSingleton();*/
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
        
        
        //OnPlayerSpawned?.Invoke(player);
        setPlayerPositionClientRpc(playerId,spawnPoints[_players.Count-1].position);


        player.healthComponent.OnDeathAction += OnPlayerDeath;
        
        if (NetworkManager.ConnectedClientsIds.Count >= minPlayers)
            onAllPlayersJoined();
        //onPlayerSpawned?.Invoke();
    }

    public void updateSpawnPoints(List<Transform> newSpawnPoints)
    {
        spawnPoints = newSpawnPoints;
    }

    public void onPlayerLeft(ulong playerId)
    {
        if(!IsServer) return;
        //onPlayerDespawned?.Invoke();
        
        var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(player!=null&&_players.Contains(player))
            _players.Remove(player);

        player.healthComponent.OnDeathAction -= OnPlayerDeath;

        if(!player.IsSpawned) return;

        NetworkManager.DisconnectClient(playerId);
        player.NetworkObject.Despawn();
        DestroyPlayerObjectClientRpc(playerId);
        
    }

    private Vector3 getRandomLobbySpawnPos(){
        return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)].position;
    }
    private Vector3 getRandomRunSpawnPos(){
        return RoomController.Instance.spawnPoints[UnityEngine.Random.Range(0, RoomController.Instance.spawnPoints.Count)].position;
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
    private void resetAllPlayersPositionsToLobbyClientRpc(){
        setPlayerPositionClientRpc(NetworkManager.LocalClientId,getRandomLobbySpawnPos());
    }

    private void onAllPlayersJoined()
    {
        print("2 or more players joined the game "+_players.ToArray());
        //probably unlock all game systems outside of the lobby
    }
    public void OnRunStarted(){
    }

    public void endRun(bool win){
        resetAllPlayersPositionsToLobbyClientRpc();
        displayPromptForAllPlayersClientRpc(win?"You win!":"You lose!",3f);
    }

    [ClientRpc]
    public void displayPromptForAllPlayersClientRpc(string message,float duration){
        UIManager.Instance.showPromptFor(message,duration);
    }

    public override void OnNetworkSpawn()
    {
        RoomController.Instance.OnRunStartAction += OnRunStarted;
        RoomController.Instance.OnRunEndAction += endRun;
        
        if(!IsServer) return;

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
        setPlayerPositionClientRpc(player.OwnerClientId,getRandomRunSpawnPos());
    }

    public override void OnNetworkDespawn()
    {
        RoomController.Instance.OnRunStartAction -= OnRunStarted;
        RoomController.Instance.OnRunEndAction -= endRun;

        if(!IsServer) return;

        NetworkManager.OnClientConnectedCallback -= onPlayerJoined;
        NetworkManager.OnClientDisconnectCallback -= onPlayerLeft;

        
    }

    //TODO: handle host/client disconnects here probably
    public void ExitGame(){
        beginShutDown();
        Application.Quit();
    }
    public override void OnDestroy()
    {
        //NetworkManager.OnClientStarted -= onPlayerJoined;
        
        base.OnDestroy();
    }
    
    public void beginShutDown(bool exitGame=false)
    {
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
            
    }
}