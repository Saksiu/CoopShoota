using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(EnemyHealthComponent))]
public class EnemyLootComponent : NetworkBehaviour
{
    [SerializeField] private PlayerPickableComponent pickableToSpawn;

    private EnemyHealthComponent healthComponent;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsServer) return;
        healthComponent = GetComponent<EnemyHealthComponent>();
        healthComponent.OnDeathLocal += handleDeath;

    }
    
    public override void OnNetworkDespawn()
    {
        if(IsServer && healthComponent!=null){
            healthComponent.OnDeathLocal -= handleDeath;
        }

        base.OnNetworkDespawn();
    }

    private void handleDeath(EnemyController enemy){
        Assert.IsTrue(IsServer,"handleDeath called on client");
        Vector3 pos = enemy.transform.position;
        Instantiate(pickableToSpawn, pos, Quaternion.identity,null).GetComponent<NetworkObject>().Spawn();
    }
}
