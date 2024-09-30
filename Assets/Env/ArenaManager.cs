using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ArenaManager : SingletonNetwork<ArenaManager>
{
    public NetworkVariable<uint> runPhase = new NetworkVariable<uint>(0);

    public static Action OnRunStartAction;

    /// <summary>
    /// called with a bool determining if players won the run or not
    /// </summary>
    public static event Action<bool> OnRunEndAction;

    public List<Transform> spawnPoints;

    private uint playersInArena = 0;

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();

        if(!IsServer) return;

        EnemyHealthComponent.OnEnemyDeathAction+=OnEnemyKilled;

        runPhase.OnValueChanged.Invoke(0,0);

    }
    public override void OnNetworkDespawn(){
        if(IsServer){
            EnemyHealthComponent.OnEnemyDeathAction-=OnEnemyKilled;
        }
        
        base.OnNetworkDespawn();
    }

    public void onPlayerEnteredArena(){
        if(!IsServer) return;
        playersInArena++;
        if(playersInArena==GameMaster.Instance.getConnectedPlayers().Count){
            OnRunStartAction?.Invoke();
        }
    }

    private void OnEnemyKilled(EnemyController killedEnemy){
        //look how many enemies are left in the room, so just the entire scene
        var enemies = FindObjectsOfType<EnemyController>();
        print("enemies left in room: "+enemies.Length);
        if(enemies.Length<=1){ //! 1 because it will account for the last enemy, despawning can take 1-2 frames...
            print("all enemies killed in room");
            increaseRunPhaseNumServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void increaseRunPhaseNumServerRpc(){
        runPhase.Value++;
    }
}

