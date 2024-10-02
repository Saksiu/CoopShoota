using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Assertions;
/*
*1. Open the gate for all players, disable trigger on the client side
*2. When player enters the arena, inform the arenaManager
*3. Once all players join, close the gate for all of them
*4. Re-open the gate when the run ends
**/
public class EntranceGateComponent : NetworkBehaviour
{
    [SerializeField] private Animator gateAnimator;

    [SerializeField] private Collider entranceTrigger;

    private List<ulong> playersInArena=new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer){
            //ArenaManager.OnRunEndAction+=handleRunEnd;
            ArenaManager.runPhaseChanged+=handleRunPhaseChange;
            //ArenaManager.OnRunStartAction+=handleRunStart;
            gateAnimator.SetBool("isOpen",true);
        }else{
            entranceTrigger.enabled=false;
        }
        
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer){
            //ArenaManager.OnRunEndAction-=handleRunEnd;
            ArenaManager.runPhaseChanged-=handleRunPhaseChange;
            //ArenaManager.OnRunStartAction-=handleRunStart;
        }

        base.OnNetworkDespawn();
    }

    private void handleRunPhaseChange(int prev, int curr){
        print("run phase changed from "+prev+" to "+curr);
        if(!IsServer) return;
        if(curr<0){
            //reset
        }
        else if(curr==0){
            handleRunStart();
        }
        else if(curr>99){
            handleRunEnd(true);
        }
    }
    private void handleRunStart(){
        print("run started");
        Assert.IsTrue(IsServer,"handleRunStart called on client");
        entranceTrigger.enabled=false;
        gateAnimator.SetBool("isOpen",false);
    }
    private void handleRunEnd(bool win){
        entranceTrigger.enabled=true;
        gateAnimator.SetBool("isOpen",true);
        playersInArena.Clear();
    }

    public void OnPlayerTriggerEnter(){
        print($"player triggered entrance gate on client {NetworkManager.LocalClientId}, isServer: {IsServer}, isOwner: {IsOwner}, isClient: {IsClient}");
        if(IsClient){
            onPlayerEnteredTriggerServerRpc(NetworkManager.LocalClientId);
        }
        //entranceTrigger.enabled=false;
        
    }
    [ServerRpc(RequireOwnership = false)]
    private void onPlayerEnteredTriggerServerRpc(ulong enteringClientID){
        if(!playersInArena.Contains(enteringClientID)){
            ArenaManager.Instance.onPlayerEnteredArena();
            playersInArena.Add(enteringClientID);
        }
    }

    [ClientRpc]
    private void setTriggerActiveClientRpc(bool enabled, ClientRpcParams rpcParams=default){
        entranceTrigger.enabled=enabled;
        gateAnimator.SetBool("isOpen",enabled);
    }
}
