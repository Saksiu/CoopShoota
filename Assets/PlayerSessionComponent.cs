using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSessionComponent : NetworkBehaviour
{
    public void beginShutDown(bool exitGame=false)
    {
        print("beginshutdown called for "+NetworkManager.LocalClientId);
        
        //caller.
        //print("beginshutdown called for "+caller.playerName.Value);
        if (IsServer){
            Shutdown(exitGame);
        }else if(IsOwner){
            DisconnectClientServerRpc(NetworkManager.LocalClientId);
            Shutdown(exitGame);
            //StartCoroutine(ClientShutdownCoroutine(exitGame));
        }
            
    }

    public void Awake(){
        NetworkManager.OnClientStopped+=handleClientStopped;
    }

    private void handleClientStopped(bool wasHost){
        StartCoroutine(desperateLoadSceneDelay());
    }

    private IEnumerator desperateLoadSceneDelay(){
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene("PlayScene");
    }

    public override void OnDestroy(){
        NetworkManager.OnClientStopped-=handleClientStopped;
        base.OnDestroy();
    }
    [ServerRpc]
    private void DisconnectClientServerRpc(ulong playerID){
        print($"disconnect request made to server for {playerID}");
        NetworkManager.DisconnectClient(playerID);
    }

    private void Shutdown(bool exitGame)
    {
        print("Shutdown called "+NetworkManager.LocalClientId);
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
