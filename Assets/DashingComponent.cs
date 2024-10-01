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
    
    public void Dash(Vector3 moveDir)
    {
        if(!canDash) return;
        StartCoroutine(dashCooldownCoroutine());
        StartCoroutine(dashCoroutine(moveDir));
        //Vector3 direction = PlayerController.localPlayer.playerCamera.transform.forward;
        
        
    }

    private IEnumerator dashCoroutine(Vector3 moveDir){
        Vector3 initVelocity=PlayerController.localPlayer.rb.velocity;
        initVelocity.y=0;
        PlayerController.localPlayer.onDashFromComponent(dashDuration);

        PlayerController.localPlayer.rb.useGravity = false;
        PlayerController.localPlayer.rb.velocity = Vector3.zero;
        PlayerController.localPlayer.rb.drag=PlayerController.localPlayer.getGroundDrag();

        PlayerController.localPlayer.rb.AddForce(moveDir.normalized*dashForce,ForceMode.Impulse);
        //rumbleCameraEffect.GenerateImpulse();
        yield return new WaitForSeconds(dashDuration);
        PlayerController.localPlayer.rb.useGravity = true;
        PlayerController.localPlayer.rb.velocity = 0.8f * initVelocity.magnitude * moveDir.normalized;
        
    }

    private IEnumerator dashCooldownCoroutine()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
