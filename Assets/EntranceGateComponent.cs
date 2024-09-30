using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EntranceGateComponent : NetworkBehaviour
{

    [SerializeField] private Animator gateAnimator;

    [SerializeField] private Collider coll;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer){
            ArenaManager.OnRunEndAction+=handleRunEnd;
        }
        
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer){
            ArenaManager.OnRunEndAction-=handleRunEnd;
        }
        else{
            coll.enabled=false;
        }

        base.OnNetworkDespawn();
    }

    private void handleRunEnd(bool win){
        coll.enabled=true;
        gateAnimator.SetBool("isOpen",true);
    }

    private void OnTriggerEnter(Collider other){

        if(!IsServer) return;
        if(other.gameObject.TryGetComponent(out PlayerController player)){
            //setTriggerActiveClientRpc(false,new ClientRpcParams{Send = new ClientRpcSendParams{TargetClientIds = new[] {player.OwnerClientId}}});
            coll.enabled=false;
            ArenaManager.Instance.onPlayerEnteredArena();
        }
    }

    [ClientRpc]
    private void setTriggerActiveClientRpc(bool enabled, ClientRpcParams rpcParams=default){
        coll.enabled=enabled;
    }
}
