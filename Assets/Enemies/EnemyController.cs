using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using AIUtils;
using UnityEngine.AI;

public class EnemyController : NetworkBehaviour
{
    [SerializeField] private float speed;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private NavMeshAgent agent;
    
    [SerializeField] private EnemyHealthComponent healthComponent;
    //[SerializeField] private HealthBarController healthBar;

    [SerializeField] private float playerDetectionRadius=10f;
    private Transform target;
    //public override void OnNetworkSpawn() { }

    private StateMachine stateMachine;

    public override void OnNetworkSpawn()
    {
        //healthBar.init(healthComponent.maxHP);
        if(!IsServer) return;
        
        stateMachine=new StateMachine();
        
        StateMachineState idle = new StateMachineState("idle", onStartIdle, null, null);
        StateMachineState chase = new StateMachineState("chase", onStartChase, onUpdateChase, null);
        
        idle.AddTransition(anyPlayerInRange, chase);
        chase.AddTransition(()=>!anyPlayerInRange(), idle);
        
        healthComponent.OnEnemyDeathACtion += onDeath;

        stateMachine.CurrentState = idle;
    }
    public override void OnNetworkDespawn()
    {
        if(IsServer){
            healthComponent.OnEnemyDeathACtion -= onDeath;
        }

        base.OnNetworkDespawn();
    }

    private void onStartIdle()
    {
        //print("onStartIdle");
    }
    
    private void onStartChase()
    {
        //print("onStartChase");
    }


    //TODO: improve performance
    //this is expensive to do on update, as we iterate through all players for each enemy, 
    //and then recalculate the path to it, even if its the same
    private void onUpdateChase()
    {
        if(target==null){
            target=getClosestPlayer();
            if(target==null) return;
        }

        if(Vector3.Distance(transform.position,target.position)>playerDetectionRadius) return;

        agent.SetDestination(target.position);
        agent.CalculatePath(target.position,agent.path);

        
        //! DEPRECATED: old direct-move code
        //Vector3 direction= (target.position - transform.position).normalized*speed;
        //rb.velocity = new Vector3(direction.x,rb.velocity.y,direction.z);
        //transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
    }

    private bool anyPlayerInRange()
    {
        foreach (var player in GameMaster.Instance._players)
            if(Vector3.Distance(transform.position,player.transform.position)<playerDetectionRadius)
                return true;

        return false;
    }

    private Transform getClosestPlayer(){
        Transform closestPlayer = target;
        float closestDistance = float.MaxValue;
        float nextPlayerDistance;
        
        foreach (var player in GameMaster.Instance._players)
        {
            if(player==null) break;
            nextPlayerDistance = Vector3.Distance(player.transform.position, transform.position);
            if (nextPlayerDistance < closestDistance)
            {
                closestDistance = nextPlayerDistance;
                closestPlayer = player.transform;
            }
        }
        
        if(closestDistance<1f) return null;
        else return closestPlayer;
    }
    
    private void FixedUpdate()
    {
        if (!IsServer) return;
        
        stateMachine.Execute();
    }

    private void OnCollisionEnter(Collision other)
    {
        //print(other.gameObject.layer);
        if(!IsServer) return;
        
        if (other.gameObject.GetComponent<BulletController>())
        {
            
            //deduct hp
            //print("enemy hit by bullet "+NetworkManager.LocalClientId);
            if(IsSpawned)
                healthComponent.DeductHPServerRpc(1);
            StartCoroutine(enforcePosandVelocityAfterCollision());
        }
        //print("collided enemy"+gameObject.layer+" with "+other.gameObject.layer+" ???: "+(other.gameObject.layer==enemyLayer));
        
        //col.enabled = false;

        other.gameObject.GetComponent<PlayerController>()?.healthComponent.DeductHPServerRpc(1);

        //healthComponent.HP.Value--;
        //if(!IsServer) return;
        /*if(NetworkObject.IsSpawned)
            NetworkObject.Despawn();*/
    }

    //this is probably very dumb, I should probably just make bullets colliders triggers
    private IEnumerator enforcePosandVelocityAfterCollision()
    {
        Vector3 tempPos = transform.position;
        yield return new WaitForFixedUpdate();
        transform.position = tempPos;
        rb.velocity = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!IsServer) return;
        
        //if(other.gameObject.layer==actualValueOfLayerMask(enemyLayer)||other.gameObject.layer==actualValueOfLayerMask(wallLayer)) return;

        if (other.gameObject.GetComponent<BulletController>())
        {
            //deduct hp
            //print("enemy hit by bullet "+NetworkManager.LocalClientId);
            if(IsSpawned)
                healthComponent.DeductHPServerRpc(1);
        }
    }

    public void onDeath(EnemyController enemySelf)
    {
        if(NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
    private int actualValueOfLayerMask(LayerMask layerMask)=> (int)Mathf.Log(layerMask.value, 2);
}
