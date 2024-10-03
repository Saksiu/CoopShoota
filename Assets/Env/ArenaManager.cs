using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class ArenaManager : SingletonNetwork<ArenaManager>
{
    private int runPhase = -1;

    public static Action<int,int> runPhaseChanged;

    public uint maxPhaseNum = 3;

    //public static Action OnRunStartAction;

    /// <summary>
    /// called with a bool determining if players won the run or not
    /// </summary>
    //public static event Action<bool> OnRunEndAction;

    public List<Transform> spawnPoints;

    private uint playersInArena = 0;

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();

        if(!IsServer) return;

        EnemyHealthComponent.OnEnemyDeathAction+=OnEnemyKilled;

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
        
        print(playersInArena+" players in the arena");
        if(playersInArena>=GameMaster.Instance.getConnectedPlayers().Count){
            print("all players in the arena, setting runPhase to 0");
            setRunPhase(0);
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
    public void OnPlayerKilledServerRpc(ulong playerID){
        print($"received OnPlayerKilledServerRpc in arena manager for player {playerID}");
        playersInArena--;
        if(playersInArena<=0){
            print("all players dead, resetting run phase");
            setRunPhase(-1);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void increaseRunPhaseNumServerRpc(){
        if(runPhase==maxPhaseNum){
            setRunPhase(100);
            return;
        }
        
        setRunPhase(runPhase+1);
    }
    private void setRunPhase(int newPhase){
        int prev=runPhase;
        runPhase=newPhase;
        runPhaseChanged?.Invoke(prev,newPhase);
    }
}

