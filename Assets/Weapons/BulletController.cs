using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float speed;

    public void Launch(Vector3 dir)
    {
        //GetComponent<Rigidbody2D>().AddForce(dir*speed,ForceMode2D.Impulse);
        GetComponent<Rigidbody>().AddForce(dir*speed,ForceMode.Impulse);
        Invoke(nameof(DestroySelf), 5f);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        DestroySelf();
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
    
}
