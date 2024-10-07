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
    [SerializeField] private Collider HitscanCollider;
    [SerializeField] private LayerMask hitscanLayer;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    private AnimationEventPropagator animationEventPropagator;
    
    [SerializeField] private EnemyHealthComponent healthComponent;
    //[SerializeField] private HealthBarController healthBar;

    //[SerializeField] private float rotationCorrectionSpeed=1f;
    [SerializeField] private float playerAttackDistance=1f;
    private Transform target;
    //public override void OnNetworkSpawn() { }

    private List<Collider> previouslyCollided=new();
    private StateMachine stateMachine;
    private bool attackAnimActive=false;

    public override void OnNetworkSpawn()
    {
        //healthBar.init(healthComponent.maxHP);
        if(!IsServer) return;
        
        stateMachine=new StateMachine();
        
        StateMachineState idle = new StateMachineState("idle", onStartIdle, null, null);
        StateMachineState chase = new StateMachineState("chase", onStartChase, onUpdateChase, null);
        StateMachineState attack=new StateMachineState("attack",onStartAttack,null,onEndAttack);
        
        idle.AddTransition(anyPlayerInRange, chase);
        chase.AddTransition(()=>(!anyPlayerInRange())&&(!attackAnimActive), idle);
        chase.AddTransition(isTargetWithinAttackRange, attack);
        attack.AddTransition(()=>(!isTargetWithinAttackRange())&&(!attackAnimActive), chase);
        stateMachine.CurrentState = idle;


        EnemyHealthComponent.OnEnemyDeathAction += onDeath;

        animationEventPropagator = GetComponentInChildren<AnimationEventPropagator>();
        animationEventPropagator.AnimationEventAction += onAnimationEventCallbackReceived;
        
    }

    private void onAnimationEventCallbackReceived(string eventName){
        //print("animation event received: "+eventName);
        switch (eventName)
        {
            case "AttackStart":
                onAttackStartAnimEvent();
                break;
            case "AttackHit":
                onAttackHitAnimEvent();
                break;
            case "AttackAnimEnd":
                if(IsServer){
                    print("attack anim end event");
                    attackAnimActive=false;
                }
                    
                break;
        }
    }
    public override void OnNetworkDespawn()
    {
        if(IsServer){
            EnemyHealthComponent.OnEnemyDeathAction -= onDeath;

            if(animationEventPropagator!=null)
                animationEventPropagator.AnimationEventAction -= onAnimationEventCallbackReceived;
        }

        base.OnNetworkDespawn();
    }
    
    private void onStartAttack(){
        agent.isStopped=true;
        animator.SetBool("Attack",true);
    }

    public void onAttackStartAnimEvent(){
        if(!IsServer) return;
        print("attack start anim event");
        attackAnimActive=true;
        //rotate to face player
        rb.rotation = Quaternion.LookRotation(target.position - transform.position);
    }
    private static Collider[] collisionBuffer=new Collider[20];
    public void onAttackHitAnimEvent(){
        if(!IsServer) return;

        Physics.OverlapBoxNonAlloc(
            HitscanCollider.bounds.center, HitscanCollider.bounds.size, collisionBuffer, 
            HitscanCollider.transform.root.rotation,hitscanLayer);
        for(int i=0;i<collisionBuffer.Length;i++){
            if(collisionBuffer[i]==null) break;
            if(collisionBuffer[i].TryGetComponent(out PlayerController player)){
                player.healthComponent.DeductHPServerRpc(1);
            }
            collisionBuffer[i]=null;
        }
        //do a boxcast using a collider, deal damage to all players in it
    }
    private void onEndAttack(){
        agent.isStopped=false;
        animator.SetBool("Attack",false);
    }

    private void onStartIdle()
    {
        animator.SetBool("Run",false);
        //print("onStartIdle");
    }
    
    private void onStartChase()
    {
        animator.SetBool("Run",true);
        agent.isStopped=false;
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

        agent.SetDestination(target.position);

        //Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        //rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationCorrectionSpeed);

    }

    private bool isTargetWithinAttackRange(){
        //print("isTargetWithinAttackRange called, distance is "+Vector3.Distance(transform.position,target.position));
        return Vector3.Distance(transform.position,target.position)<playerAttackDistance;
    }

    private bool anyPlayerInRange()
    {
        return true;
        /*foreach (var player in GameMaster.Instance.getConnectedPlayers())
            if(Vector3.Distance(transform.position,player.transform.position)<playerDetectionRadius)
                return true;

        return false;*/
    }
    private Transform getClosestPlayer(){
        Transform closestPlayer = target;
        float closestDistance = float.MaxValue;
        float nextPlayerDistance;
        
        foreach (var player in GameMaster.Instance.getConnectedPlayers())
        {
            if(player==null) continue;
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
            return;
            //StartCoroutine(enforcePosandVelocityAfterCollision());
        }else if(other.gameObject.TryGetComponent(out PlayerController player)){
            //player.healthComponent.DeductHPServerRpc(1);
        }
        //print("collided enemy"+gameObject.layer+" with "+other.gameObject.layer+" ???: "+(other.gameObject.layer==enemyLayer));
        

        //healthComponent.HP.Value--;
        //if(!IsServer) return;
        /*if(NetworkObject.IsSpawned)
            NetworkObject.Despawn();*/
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!IsServer) return;
        
        //if(other.gameObject.layer==actualValueOfLayerMask(enemyLayer)||other.gameObject.layer==actualValueOfLayerMask(wallLayer)) return;

        if (other.gameObject.GetComponent<BulletController>())
        {
            //deduct hp
            //print("enemy hit by bullet "+NetworkManager.LocalClientId);

            if(IsSpawned&&!previouslyCollided.Contains(other)){
                healthComponent.DeductHPServerRpc(1);
                previouslyCollided.Add(other);
            }
                
        }
    }

    public void onDeath(EnemyController enemyKilled)
    {
        if(this!=enemyKilled||(!NetworkObject.IsSpawned))
            return;
        NetworkObject.Despawn();
    }
    private int actualValueOfLayerMask(LayerMask layerMask)=> (int)Mathf.Log(layerMask.value, 2);

    private void OnDrawGizmos(){
        if(HitscanCollider!=null){
            Gizmos.color=Color.red;
            //print(HitscanCollider.);
            //partially lies because it doesn't account for rotation
            //Gizmos.DrawWireCube(HitscanCollider.bounds.center, HitscanCollider.bounds.size);
           // Gizmos.DrawCube(HitscanCollider.transform.position, HitscanCollider.bounds.size);
        }
    }
}
