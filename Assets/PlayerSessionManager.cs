using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class PlayerSessionManager : SingletonLocal<PlayerSessionManager>
{

    public void Start(){
        NetworkManager.Singleton.OnClientStopped+=handleClientStopping;
        //NetworkManager.Singleton.OnServerStopped+=handleLocalServerStopping;
    }

    public void OnDestroy(){
        if(NetworkManager.Singleton==null) return;
        NetworkManager.Singleton.OnClientStopped-=handleClientStopping;
        //NetworkManager.Singleton.OnServerStopped-=handleLocalServerStopping;
    }

    /*public void Update(){
        print($"Session State: IsListening: {NetworkManager.Singleton.IsListening} Is Shutting down: {NetworkManager.Singleton.ShutdownInProgress} IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsHost: {NetworkManager.Singleton.IsHost}");
    }*/
    public void beginShutDown(bool exitGame=false)
    {
        print("beginshutdown called for "+NetworkManager.Singleton.LocalClientId);
        StartCoroutine(shutDownCoroutine(exitGame));
        
            
    }

    private void handleClientStopping(bool wasHost){
        print("Shutting down client was host?"+wasHost);
        if(wasHost) return;
        NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>().StopDiscovery();
        SceneManager.LoadScene("Prot_Arena1");

    }

    private IEnumerator shutDownCoroutine(bool exitGame){
        NetworkManager.Singleton.Shutdown();
        print("waiting for singleton to finish shutting down before reload");
        yield return new WaitUntil(()=>!NetworkManager.Singleton.ShutdownInProgress);
        if(exitGame){Application.Quit();}
        else{
            NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>().StopDiscovery();
            SceneManager.LoadScene("Prot_Arena1");
           // MainMenuManager.Instance.enableMainMenu();
        }
        
    }

    public void DisconnectClient(ulong clientID){
        if(!NetworkManager.Singleton.IsServer){
            Debug.LogWarning($"DisconnectClient for {clientID} called on non-server {NetworkManager.Singleton.LocalClientId}, aborting");
            return;
        }
        NetworkManager.Singleton.DisconnectClient(clientID);
    }
}
