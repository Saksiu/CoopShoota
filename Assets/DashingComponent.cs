using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashingComponent : MonoBehaviour
{

    [SerializeField] private float dashForce;
    [SerializeField] private float dashCooldown;
    [SerializeField] private float dashDuration;
    
    private bool candash = true;

    private void FixedUpdate()
    {
        if(Input.GetKey(KeyCode.LeftShift)) Dash(GetComponent<Rigidbody2D>().velocity);
    }

    public void Dash(Vector2 direction)
    {
        if(!candash) return;
        GetComponent<PlayerController>().onDash(dashDuration);
        GetComponent<Rigidbody2D>().AddForce(direction.normalized*dashForce,ForceMode2D.Impulse);
        StartCoroutine(dashCooldownCoroutine());
    }

    private IEnumerator dashCooldownCoroutine()
    {
        candash = false;
        yield return new WaitForSeconds(dashCooldown);
        candash = true;
    }
}
