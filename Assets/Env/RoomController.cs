using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoomController : SingletonNetwork<RoomController>
{

    private NetworkVariable<bool> isRunActive = new NetworkVariable<bool>(false);

    public Action OnRunStartAction;
    public Action<bool> OnRunEndAction;

    public List<Transform> spawnPoints;

    public override void OnNetworkSpawn(){
        EnemyHealthComponent.OnEnemyDeathAction+=OnEnemyKilled;
        isRunActive.OnValueChanged+=onRunStateChanged;

        isRunActive.OnValueChanged.Invoke(false,isRunActive.Value);

        base.OnNetworkSpawn();
    }
    public override void OnNetworkDespawn(){
        EnemyHealthComponent.OnEnemyDeathAction-=OnEnemyKilled;
        isRunActive.OnValueChanged-=onRunStateChanged;

        base.OnNetworkDespawn();
    }

    private void onRunStateChanged(bool prev, bool curr){
        if(curr){
            if(!prev){
                InitializeRoom();
                OnRunStartAction?.Invoke();
            }
        }
        else{
            OnRunEndAction?.Invoke(true);
        }
    }

    public void InitializeRoom()
    {
        //print("INITROOM action called");
        //TODO: win conditions, ensuring some conditions, etc.
        //setIsRunActiveServerRpc(true);

        //* get all enemy spawners, subscribe to their endspawning action events
    }

    private void OnEnemyKilled(EnemyController killedEnemy){
        //look how many enemies are left in the room, so just the entire scene
        var enemies = FindObjectsOfType<EnemyController>();
        print("enemies left in room: "+enemies.Length);
        if(enemies.Length<=1){ //! 1 because it will account for the last enemy, despawning can take 1-2 frames...
            print("all enemies killed in room");
            setIsRunActiveServerRpc(false);
            if(IsServer)
                GameMaster.Instance.endRun(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void setIsRunActiveServerRpc(bool isActive){
        isRunActive.Value = isActive;
    }
}

