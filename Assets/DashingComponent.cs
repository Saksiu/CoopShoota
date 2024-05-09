using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class DashingComponent : MonoBehaviour
{

    [SerializeField] private float dashForce;
    [SerializeField] private float dashCooldown;
    
    //[SerializeField] private float dashMaxDistance;
    [SerializeField] private float dashDuration;


    [SerializeField] private CinemachineImpulseSource rumbleCameraEffect;
    
    private bool canDash = true;
    
    public void Dash()
    {
        if(!canDash) return;
        
        Vector3 direction = PlayerController.localPlayer.playerCamera.transform.forward;
        
        PlayerController.localPlayer.onDashFromComponent(dashDuration);
        
        PlayerController.localPlayer.rb.velocity = Vector3.zero;
        PlayerController.localPlayer.rb.AddForce(direction.normalized*dashForce,ForceMode.Impulse);
        rumbleCameraEffect.GenerateImpulse();
        
        StartCoroutine(dashCooldownCoroutine());
    }

    private IEnumerator dashCooldownCoroutine()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
