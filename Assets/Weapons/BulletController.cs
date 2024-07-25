using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float speed;

    [SerializeField] private float lifeTime;

    private bool ignoringEnemies = false;
    public void Launch(Vector3 dir)
    {
        //GetComponent<Rigidbody2D>().AddForce(dir*speed,ForceMode2D.Impulse);
        GetComponent<Rigidbody>().AddForce(dir*speed,ForceMode.Impulse);
        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void OnCollisionEnter(Collision col)
    {
        //print("bullet collision");
        OnCollisionCustom(col.gameObject);
    }

    private void OnCollisionCustom(GameObject other)
    {
        //print(name+" collided with "+other.name);
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
