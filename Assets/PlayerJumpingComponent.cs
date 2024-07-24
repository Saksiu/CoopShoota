using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpingComponent : MonoBehaviour
{
    [SerializeField] private float jumpForce;

    [SerializeField] private float midAirMoveForce;

    [SerializeField] private float airForceAddThreshold=17f;

    public void Jump()
    {
        //print("isgrounded: "+isGrounded());
        if(PlayerController.localPlayer.isGrounded()){
            PlayerController.localPlayer.rb.AddForce(transform.up*jumpForce,ForceMode.Impulse);
        }
        
    }
    public void OnMoveInput(Vector3 inputDir){
        if(PlayerController.localPlayer.isGrounded())
            return;

        Vector2 XZVelocity = new Vector2(
            PlayerController.localPlayer.rb.velocity.x,
            PlayerController.localPlayer.rb.velocity.z);
            
        if(XZVelocity.magnitude>airForceAddThreshold)
            return;
        
        //print("mid air jump");
        //PlayerController.localPlayer.rb.AddForce(inputDir.normalized*midAirMoveForce,ForceMode.VelocityChange);
        PlayerController.localPlayer.rb.AddForce(inputDir.normalized*midAirMoveForce,ForceMode.Force);
    }
}
