using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float speed;

    private bool ignoringEnemies = false;
    public void Launch(Vector3 dir)
    {
        //GetComponent<Rigidbody2D>().AddForce(dir*speed,ForceMode2D.Impulse);
        GetComponent<Rigidbody>().AddForce(dir*speed,ForceMode.Impulse);
        Invoke(nameof(DestroySelf), 5f);
    }

    private void OnCollisionEnter(Collision col)
    {
        //print("bullet collision");
        OnCollisionCustom(col.gameObject);
    }

    private void OnCollisionCustom(GameObject other)
    {
        if ((!ignoringEnemies)&&other.layer == LayerMask.NameToLayer("Ground"))
        {
            ignoringEnemies=true;
            GetComponent<Collider>().excludeLayers = LayerMask.GetMask("Enemy");
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
