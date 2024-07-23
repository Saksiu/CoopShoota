using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpingComponent : MonoBehaviour
{
    [SerializeField] private float jumpForce;

    [SerializeField] private float midAirMoveForce;

    public void Jump()
    {
        //print("isgrounded: "+isGrounded());
        if(PlayerController.localPlayer.isGrounded()){
            PlayerController.localPlayer.rb.AddForce(transform.up*jumpForce,ForceMode.Impulse);
        }
        
    }
    public void OnMoveInput(Vector3 inputDir){
        if(!PlayerController.localPlayer.isGrounded()){
            //print("mid air jump");
            //PlayerController.localPlayer.rb.AddForce(inputDir.normalized*midAirMoveForce,ForceMode.VelocityChange);
            PlayerController.localPlayer.rb.AddForce(inputDir.normalized*midAirMoveForce,ForceMode.Force);
        }
    }
}
