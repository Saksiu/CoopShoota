using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawnerController : NetworkBehaviour
{
    //[SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<EnemyWaveData> Waves;
    [SerializeField] private float spawnInterval;
    
    [SerializeField] private BoxCollider spawnArea;
    
    private Coroutine enemySpawnCoroutineRef;

    public void BeginSpawningEnemies()
    {
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
                Vector2 randomPos = getRandomSpawnPos();
                SpawnEnemy(wave.enemyPrefab,randomPos);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }
    private void SpawnEnemy(GameObject enemyPrefab, Vector2 pos)
    {
        Instantiate(enemyPrefab, pos, Quaternion.identity,null).GetComponent<NetworkObject>().Spawn();
    }
    
    private Vector3 getRandomSpawnPos()
    {
        Vector3 min = spawnArea.bounds.min;
        Vector3 max = spawnArea.bounds.max;
        //get a random point inside the spawn area
        
        return new Vector3(Random.Range(min.x,max.x),Random.Range(min.y,max.y),Random.Range(min.z,max.z));
        
        //return (Random.insideUnitCircle * spawnArea.radius)+(Vector2)spawnArea.transform.position;
    }

    public void OnDestroy()
    {
        print("i am being destroyed!");
        //base.OnDestroy();
    }
}
[Serializable]
public class EnemyWaveData
{
    public GameObject enemyPrefab;
    public uint enemyCount;
    public uint waveInitDelay;
}
