using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArenaGateComponent : NetworkBehaviour
{
    [SerializeField] private Animator gateAnimator;

    public EnemySpawnerComponent enemySpawner;


    public override void OnNetworkSpawn(){
        //EnemyWavesManager.OnWaveStart+=handleWaveStart;
        //EnemyWavesManager.OnWaveEnd+=handleWaveEnd;
        base.OnNetworkSpawn();
    }
    public void OnNetworkDeSpawn(){
        //EnemyWavesManager.OnWaveStart-=handleWaveStart;
        //EnemyWavesManager.OnWaveEnd-=handleWaveEnd;
    }


    /*private void handleWaveStart(){
        OpenGate();
        enemySpawner.BeginSpawningEnemies();
    }

    private void handleWaveEnd(){

    }*/

    public void OpenGate(){
        gateAnimator.SetBool("isOpen",true);
    }

    public void CloseGate(){
        gateAnimator.SetBool("isOpen",false);
    }
}
