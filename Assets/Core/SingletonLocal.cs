using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SingletonLocal<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }
    [SerializeField] private bool DestroyOnLoad = true;
    public virtual void Awake() {
        if (Instance == null)
        {
            Instance = this as T;
            if(!DestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            //we can destroy local singletons as they are not tracked by NetworkManager
            Destroy(gameObject);
        }
    }
}
public class SingletonNetwork<T> : NetworkBehaviour where T : Component
{
    public static T Instance { get; private set; }

    public virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
        }
        else
        {
            //we cannot destroy Networked singletons as they are tracked by NetworkManager
            //this works for now
            enabled = false;
        }
    }

    /*public override void OnNetworkSpawn()
    {
        if(!IsServer)
            enabled = false;
        base.OnNetworkSpawn();
    }*/
}