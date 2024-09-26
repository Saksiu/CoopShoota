using System.Collections;
using System.Collections.Generic;
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
        if (IsServer)
            StartCoroutine(HostShutdown(exitGame));
        else
            Shutdown(exitGame);
    }
    
    private IEnumerator HostShutdown(bool exitGame)
    {
        // Tell all clients to shutdown
        print("HostShutdown called, sending shutdown message to clients");
        ShutdownClientRpc(false);

        // Wait some time for the message to get to clients
        yield return new WaitForSeconds(0.5f);
        print("Shutting down host itself after 0.5s wait");
        // Shutdown server/host
        Shutdown(exitGame);
    }
    
    [ClientRpc]
    private void ShutdownClientRpc(bool exitGame)
    {
        print("ShutdownClientRpc called "+NetworkManager.LocalClientId+" IsOwner?: "+IsOwner);
        if(!IsOwner) return;
        //if(IsHost) return;

        Shutdown(exitGame);
    }
    private void Shutdown(bool exitGame)
    {
        print("Shutdown called "+NetworkManager.LocalClientId);
        NetworkManager.Singleton.Shutdown();
        if(exitGame){Application.Quit();}
        else{
            NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>().StopDiscovery();
            SceneManager.LoadSceneAsync("PlayScene");
           // MainMenuManager.Instance.enableMainMenu();
        }
            
    }
}