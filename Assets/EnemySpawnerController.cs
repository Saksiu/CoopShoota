using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawnerController : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval;
    
    [SerializeField] private CircleCollider2D spawnArea;
    
    private Coroutine enemySpawnCoroutineRef;

    public void BeginSpawningEnemies()
    {
        if(!IsServer) return;
        if(enemySpawnCoroutineRef!=null) return;
        enemySpawnCoroutineRef = StartCoroutine(spawnEnemyCoroutine());
    }
    public void StopSpawningEnemies()
    {
        if(enemySpawnCoroutineRef==null) return;
        StopCoroutine(enemySpawnCoroutineRef);
        enemySpawnCoroutineRef = null;
    }

    private IEnumerator spawnEnemyCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            Vector2 randomPos = getRandomSpawnPos();
            SpawnEnemy(randomPos);
        }
        //RequestEnemySpawnServerRpc(randomPos);
    }
    private void SpawnEnemy(Vector2 pos)
    {
        Instantiate(enemyPrefab, pos, Quaternion.identity,null).GetComponent<NetworkObject>().Spawn();
    }
    
    private Vector2 getRandomSpawnPos()
    {
        return (Random.insideUnitCircle * spawnArea.radius)+(Vector2)spawnArea.transform.position;
    }

    public void OnDestroy()
    {
        print("i am being destroyed!");
        //base.OnDestroy();
    }
}
