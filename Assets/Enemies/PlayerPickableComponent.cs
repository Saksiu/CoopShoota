using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerPickableComponent : NetworkBehaviour
{
    [SerializeField] private int AmmoGiven=10;

    [SerializeField] private float rotationSpeed=1f;


    public override void OnNetworkSpawn()
    {
        if(IsServer){
            ArenaManager.runPhaseChanged+=handleRunPhaseChange;
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer){
            ArenaManager.runPhaseChanged-=handleRunPhaseChange;
        }


        base.OnNetworkDespawn();
    }

    private void handleRunPhaseChange(int prev, int curr){
        
        if(curr<0){
            NetworkObject.Despawn(true);
        }
    }


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
