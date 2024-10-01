using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerPickableComponent : NetworkBehaviour
{
    [SerializeField] private uint AmmoGiven=10;


    private void OnTriggerEnter(Collider other)
    {
        print("player pickable trigger enter");
        if(other.TryGetComponent(out PlayerController player)){
            print($"triggered with player, is player owner: {player.IsOwner}");
            if(!player.IsOwner) return;
            GetComponent<Collider>().enabled=false;
            player.addAmmoToCurrentGun(AmmoGiven);
            DespawnSelfServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnSelfServerRpc(){
        if(!NetworkObject.IsSpawned)
            GetComponent<NetworkObject>().Despawn();
    }
}
