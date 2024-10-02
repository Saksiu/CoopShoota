using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerPickableComponent : NetworkBehaviour
{
    [SerializeField] private int AmmoGiven=10;

    [SerializeField] private float rotationSpeed=1f;
    private void FixedUpdate(){
        transform.Rotate(Vector3.up,rotationSpeed);
    }
    private void OnTriggerEnter(Collider other)
    {
        print("player pickable trigger enter");
        if(other.TryGetComponent(out PlayerController player)){
            print($"triggered with player, is player owner: {player.IsOwner}");
            if(!player.IsOwner) return;
            GetComponent<Collider>().enabled=false;


            GunsManager.Instance.addAmmoToCurrentlyHeldGunServerRpc(NetworkManager.LocalClientId,AmmoGiven);
            DespawnSelfServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnSelfServerRpc(){
        if(NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
