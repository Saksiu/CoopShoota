using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSessionComponent : NetworkBehaviour
{
    public void beginShutDown(bool exitGame=false)
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
            
    }
}
