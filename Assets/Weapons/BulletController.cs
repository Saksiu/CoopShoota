using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float speed;

    [SerializeField] private float lifeTime;

    [SerializeField] private LineRenderer bulletTraceRenderer;

    private Vector3 startingPos;

    private bool ignoringEnemies = false;
    public void Launch(Vector3 dir,Vector3 visualInitPos)
    {
        startingPos = visualInitPos;
        bulletTraceRenderer.SetPosition(1,startingPos);
        //GetComponent<Rigidbody2D>().AddForce(dir*speed,ForceMode2D.Impulse);
        GetComponent<Rigidbody>().AddForce(dir*speed,ForceMode.Impulse);
        Invoke(nameof(DestroySelf), lifeTime);
    }
    private void Update()
    {
        //bulletTraceRenderer.GetPosition(1).Set(startingPos.x,startingPos.y,startingPos.z);
        bulletTraceRenderer.SetPosition(0,transform.position);
        
    }

    private void OnCollisionEnter(Collision col)
    {
        //print("bullet collision");
        OnCollisionCustom(col.gameObject);
    }

    private void OnCollisionCustom(GameObject other)
    {
        //print(name+" collided with "+other.name);
        if(other.layer == LayerMask.NameToLayer("Ground"))
        {
            DestroySelf();
            return;
        }

        if ((!ignoringEnemies)&&other.layer == LayerMask.NameToLayer("Ground"))
        {
            ignoringEnemies=true;
            GetComponent<Collider>().excludeLayers = LayerMask.GetMask("Enemy");
            return;
        }
        if(other==PlayerController.localPlayer.gameObject){
            print(name+" collided with "+other.name+" player. This should not happen");
            return;
        }
        DestroySelf();
    }
    
    private void OnTriggerEnter(Collider coll)
    {
        //print("bullet trigger"+coll.gameObject.name);
        OnCollisionCustom(coll.gameObject);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
    
}
