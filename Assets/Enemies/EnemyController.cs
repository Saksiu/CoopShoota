using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : NetworkBehaviour
{
    [SerializeField] private float speed;
    
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask enemyLayer;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;
    
    [SerializeField] private EnemyHealthComponent healthComponent;
    //[SerializeField] private HealthBarController healthBar;

    private Vector2 targetPos=Vector2.zero;
    //public override void OnNetworkSpawn() { }


    public override void OnNetworkSpawn()
    {
        //healthBar.init(healthComponent.maxHP);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        //get closer of the players
        Vector2 closestPlayerPos = Vector2.zero;
        float closestDistance = float.MaxValue;
        float nextPlayerDistance;
        foreach (var player in GameMaster.Instance._players)
        {
            if(player==null) break;
            nextPlayerDistance = Vector2.Distance(player.transform.position, transform.position);
            if (nextPlayerDistance < closestDistance)
            {
                closestDistance = nextPlayerDistance;
                closestPlayerPos = player.transform.position;
            }
        }
        if(closestDistance<1f) targetPos = transform.position;
        
        targetPos = closestPlayerPos;
        Vector2 direction= (targetPos - (Vector2)transform.position).normalized;
        rb.velocity = direction*speed;
        //transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        //print(other.gameObject.layer);
        if(!IsServer) return;
        
        //if(other.gameObject.layer==actualValueOfLayerMask(enemyLayer)||other.gameObject.layer==actualValueOfLayerMask(wallLayer)) return;

        if (other.gameObject.GetComponent<BulletController>())
        {
            //deduct hp
            print("enemy hit by bullet "+NetworkManager.LocalClientId);
            if(IsSpawned)
                healthComponent.DeductHPServerRpc(1);
        }
        //print("collided enemy"+gameObject.layer+" with "+other.gameObject.layer+" ???: "+(other.gameObject.layer==enemyLayer));
        
        //col.enabled = false;
        
        PlayerController player = other.gameObject.GetComponent<PlayerController>();
        if(player!=null)
        {
            player.healthComponent.DeductHPServerRpc(1);
        }
        //healthComponent.HP.Value--;
        //if(!IsServer) return;
        /*if(NetworkObject.IsSpawned)
            NetworkObject.Despawn();*/
    }

    public void onDeath()
    {
        if(NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
    private int actualValueOfLayerMask(LayerMask layerMask)=> (int)Mathf.Log(layerMask.value, 2);
}
