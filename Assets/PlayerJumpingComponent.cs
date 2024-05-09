using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpingComponent : MonoBehaviour
{
    [SerializeField] private float jumpForce;

    public void Jump()
    {
        //print("isgrounded: "+isGrounded());
        if(!PlayerController.localPlayer.isGrounded()) return;
        //print("can jump, jumping!");
        PlayerController.localPlayer.rb.AddForce(transform.up*jumpForce,ForceMode.Impulse);
    }
}
