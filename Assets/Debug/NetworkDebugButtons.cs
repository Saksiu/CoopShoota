using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using ParrelSync;

public class NetworkDebugButtons : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10,10,300,300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (!ClonesManager.IsClone())
            {
                if(GUILayout.Button("Host")) 
                {
                    NetworkManager.Singleton.StartHost();
                    Cursor.lockState = CursorLockMode.Locked;
                }
                if(GUILayout.Button("Server"))
                {
                    NetworkManager.Singleton.StartServer();
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            if(GUILayout.Button("Client"))
            {
                NetworkManager.Singleton.StartClient();
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        
        GUILayout.EndArea();
    }
}
