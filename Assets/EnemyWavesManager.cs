using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class EnemyWavesManager : SingletonNetwork<EnemyWavesManager>
{
    
    //public static event Action OnWaveStart;
    //public static event Action OnWaveEnd;

    [SerializeField] private List<GateSpawnData> GatesSpawnData;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsServer) return;
        ArenaManager.runPhaseChanged += handleRunPhaseChanged;
        //ArenaManager.OnRunStartAction += handleRunStart;
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer){
            ArenaManager.runPhaseChanged -= handleRunPhaseChanged;
        }

        base.OnNetworkDespawn();
    }

    /*private void handleRunStart(){
        Assert.IsTrue(IsServer,"handleRunStart called on client");
        
    }*/

    private void handleRunPhaseChanged(int prev, int curr)
    {
        print("run phase changed from "+prev+" to "+curr);
        if(!IsServer)return;

        if(curr<0){
            foreach(var gateData in GatesSpawnData){
                gateData.gate.CloseGate();
                gateData.gate.enemySpawner.StopSpawningEnemies();
            }
            foreach(var enemy in FindObjectsOfType<EnemyController>()){
            enemy.GetComponent<NetworkObject>().Despawn(true);
            }
            return;
        }
        //default: pass all gate data to the gate enemy spawners, and open the gates
        foreach(var gateData in GatesSpawnData){
            gateData.gate.enemySpawner.injectWaveData(gateData.phasesData[curr].wavesData);
            gateData.gate.OpenGate();
            gateData.gate.enemySpawner.BeginSpawningEnemies();
            /*foreach(var phaseData in gateData.phasesData){
                if(phaseData.wavesData.Count>curr){
                    gateData.gate.injectSpawnerData(phaseData.wavesData);
                    gateData.gate.OpenGate();
                    return;
                }
            }*/
        }
    }

}


[Serializable]
public class GateSpawnData{
    public ArenaGateComponent gate;
    public List<EnemyPhases> phasesData;
}

[Serializable]
public class EnemyPhases{
    public List<EnemyWaveData> wavesData;
}
