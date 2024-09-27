using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class PlayerSessionManager : SingletonLocal<PlayerSessionManager>
{

    public void Start(){
        NetworkManager.Singleton.OnClientStopped+=handleStopping;
        NetworkManager.Singleton.OnServerStopped+=handleStopping;
    }

    public void OnDestroy(){
        if(NetworkManager.Singleton==null) return;
        NetworkManager.Singleton.OnClientStopped-=handleStopping;
        NetworkManager.Singleton.OnServerStopped-=handleStopping;
    }

    public void Update(){
        print($"Session State: IsListening: {NetworkManager.Singleton.IsListening} Is Shutting down: {NetworkManager.Singleton.ShutdownInProgress} IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsHost: {NetworkManager.Singleton.IsHost}");
    }
    public void beginShutDown(bool exitGame=false)
    {
        print("beginshutdown called for "+NetworkManager.Singleton.LocalClientId);
        
        
        //caller.
        //print("beginshutdown called for "+caller.playerName.Value);

        if(NetworkManager.Singleton.IsHost)
            DisconnectAllClients();

        Shutdown(exitGame);
        
            
    }

    private void handleStopping(bool wasHost){
        StartCoroutine(safeReloadGameCoroutine());
    }

    private IEnumerator safeReloadGameCoroutine(){
        print("waiting for singleton to finish shutting down before reload");
        yield return new WaitUntil(()=>!NetworkManager.Singleton.ShutdownInProgress);
        SceneManager.LoadScene("PlayScene");
    }

    private void DisconnectAllClients(){
        //print($"disconnect request made to server for {playerID}");
        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {      
            NetworkManager.Singleton.DisconnectClient(clientID);  
        }
        //NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
    }

    private void Shutdown(bool exitGame)
    {
        print("Shutdown called "+NetworkManager.Singleton.LocalClientId);
        NetworkManager.Singleton.Shutdown();
        //NetworkManager.Singleton.NetworkConfig
        //NetworkManager.Singleton.GetComponent<UnityTransport>().Shutdown();
        if(exitGame){Application.Quit();}
        else{
            NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>().StopDiscovery();
           // MainMenuManager.Instance.enableMainMenu();
        }
            
    }
}
