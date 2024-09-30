using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWavesManager : SingletonNetwork<EnemyWavesManager>
{
    
    public static event Action OnWaveStart;
    public static event Action OnWaveEnd;

    [SerializeField] private uint maxPhaseNum = 3;

    [SerializeField] private List<GateSpawnData> GatesSpawnData;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkDespawn();
        ArenaManager.Instance.runPhase.OnValueChanged += handleRunPhaseChanged;
    }

    public override void OnNetworkDespawn()
    {
        ArenaManager.Instance.runPhase.OnValueChanged += handleRunPhaseChanged;
        base.OnNetworkDespawn();
    }

    private void handleRunPhaseChanged(uint prev, uint curr)
    {
        if(!IsServer)return;

        if(curr>=maxPhaseNum){
            GameMaster.Instance.endRun(true);
        }

        if(curr==0){

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
