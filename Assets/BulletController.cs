using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] private float speed;

    public void Launch(Vector2 dir)
    {
        GetComponent<Rigidbody2D>().AddForce(dir*speed,ForceMode2D.Impulse);
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
