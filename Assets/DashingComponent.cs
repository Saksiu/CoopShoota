using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashingComponent : MonoBehaviour
{

    [SerializeField] private float dashForce;
    [SerializeField] private float dashCooldown;
    
    //[SerializeField] private float dashMaxDistance;
    [SerializeField] private float dashDuration;
    
    private bool canDash = true;

    private void FixedUpdate()
    {
        if(Input.GetKey(KeyCode.LeftShift)) 
            Dash(PlayerController.localPlayer.playerCamera.transform.forward);
    }

    public void Dash(Vector3 direction)
    {
        if(!canDash) return;
        
        PlayerController.localPlayer.onDash(dashDuration);
        PlayerController.localPlayer.rb.AddForce(direction.normalized*dashForce,ForceMode.Impulse);
        StartCoroutine(dashCooldownCoroutine());
    }

    private IEnumerator dashCooldownCoroutine()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
