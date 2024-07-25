using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Tutorials.Core.Editor;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawnerController : NetworkBehaviour
{
    //[SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<EnemyWaveData> Waves;

    [Tooltip("Global interval between each individual enemy spawn")]
    [SerializeField] private float spawnInterval;
    
    [SerializeField] private BoxCollider spawnArea;
    
    private Coroutine enemySpawnCoroutineRef;

    private RoomController owningRoom;


    private void Start()
    {
        //print("start called on enemyspawner "+NetworkBehaviourId+" isserver: "+IsServer);

        owningRoom = GetComponentInParent<RoomController>();
        if(owningRoom==null){
            Debug.LogError("ERROR: "+name+" could not find RoomController in parents");
            return;
        }
        //print("subscribing beginspawnEnemies to owner room initroom function");
        RoomController.Instance.isRunActive.OnValueChanged += onRunStateChanged;

        if(!IsServer){
            enabled = false;
            return;
        }
    }

    private void onRunStateChanged(bool prev, bool curr)
    {
        if(curr)
        {
            BeginSpawningEnemies();
        }
        else
        {
            if(enemySpawnCoroutineRef!=null)
                StopCoroutine(enemySpawnCoroutineRef);
                
            enemySpawnCoroutineRef = null;
        }
    }
    public void BeginSpawningEnemies()
    {
        print("beginspawningenemies called on "+NetworkBehaviourId);
        if(!IsServer) return;
        if(enemySpawnCoroutineRef!=null) return; //already spawning enemies
        
        enemySpawnCoroutineRef = StartCoroutine(spawnEnemyCoroutine());
    }
    /*public void StopSpawningEnemies()
    {
        if(enemySpawnCoroutineRef==null) return;
        StopCoroutine(enemySpawnCoroutineRef);
        enemySpawnCoroutineRef = null;
    }*/

    private IEnumerator spawnEnemyCoroutine()
    {
        foreach(var wave in Waves)
        {
            yield return new WaitForSeconds(wave.waveInitDelay);
            for (int i = 0; i < wave.enemyCount; i++)
            {
                Vector3 randomPos = getRandomSpawnPos();
                SpawnEnemy(wave.enemyPrefab,randomPos);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }
    private void SpawnEnemy(GameObject enemyPrefab, Vector3 pos)
    {
        Instantiate(enemyPrefab, pos, Quaternion.identity,null).GetComponent<NetworkObject>().Spawn();
    }
    
    private Vector3 getRandomSpawnPos()
    {
        Vector3 offset = spawnArea.center;
        
        Vector3 min = spawnArea.bounds.min;
        Vector3 max = spawnArea.bounds.max;
        //get a random point inside the spawn area
        
        return new Vector3(Random.Range(min.x,max.x),Random.Range(min.y,max.y),Random.Range(min.z,max.z))+offset;
        
        //return (Random.insideUnitCircle * spawnArea.radius)+(Vector2)spawnArea.transform.position;
    }

    public override void OnDestroy()
    {
        //print("i am being destroyed!");
        RoomController.Instance.isRunActive.OnValueChanged -= onRunStateChanged;
        base.OnDestroy();
    }
}
[Serializable]
public class EnemyWaveData
{
    public GameObject enemyPrefab;
    public uint enemyCount;
    public uint waveInitDelay;
}
