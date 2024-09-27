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
        //NetworkManager.Singleton.OnClientStopped+=handleStopping;
        NetworkManager.Singleton.OnServerStopped+=handleLocalServerStopping;
    }

    public void OnDestroy(){
        if(NetworkManager.Singleton==null) return;
        //NetworkManager.Singleton.OnClientStopped-=handleStopping;
        NetworkManager.Singleton.OnServerStopped-=handleLocalServerStopping;
    }

    public void Update(){
        print($"Session State: IsListening: {NetworkManager.Singleton.IsListening} Is Shutting down: {NetworkManager.Singleton.ShutdownInProgress} IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsHost: {NetworkManager.Singleton.IsHost}");
    }
    public void beginShutDown(bool exitGame=false)
    {
        print("beginshutdown called for "+NetworkManager.Singleton.LocalClientId);
        
        
        //caller.
        //print("beginshutdown called for "+caller.playerName.Value);

        //if(NetworkManager.Singleton.IsHost)
        //    DisconnectAllClients(NetworkManager.Singleton.LocalClientId);

        //Shutdown(exitGame);
        StartCoroutine(shutDownCoroutine(exitGame));
        
            
    }

    private void handleLocalServerStopping(bool wasHost){
        print("Server Stopped, shutting down client was host?"+wasHost);
        if(wasHost) return;
        
        NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>().StopDiscovery();
        SceneManager.LoadScene("PlayScene");
    }

    private IEnumerator shutDownCoroutine(bool exitGame){
        NetworkManager.Singleton.Shutdown();
        print("waiting for singleton to finish shutting down before reload");
        yield return new WaitUntil(()=>!NetworkManager.Singleton.ShutdownInProgress);
        if(exitGame){Application.Quit();}
        else{
            NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>().StopDiscovery();
            SceneManager.LoadScene("PlayScene");
           // MainMenuManager.Instance.enableMainMenu();
        }
        
    }

    private void DisconnectAllClients(ulong hostClientID){
        //print($"disconnect request made to server for {playerID}");
        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if(clientID==hostClientID) continue;
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
