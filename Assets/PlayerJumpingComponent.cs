using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpingComponent : MonoBehaviour
{
    [SerializeField] private float jumpForce;
    [SerializeField] private float checkGroundDistance=2f;

    private void FixedUpdate()
    {
        if(!PlayerController.localPlayer.IsOwner) return;
        
        if(Input.GetKey(KeyCode.Space)) 
            Jump();
    }

    private void Jump()
    {
        //print("isgrounded: "+isGrounded());
        if(!isGrounded()) return;
        //print("can jump, jumping!");
        PlayerController.localPlayer.rb.AddForce(transform.up*jumpForce,ForceMode.Impulse);
    }

    private bool isGrounded()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.down, out hit, checkGroundDistance);
        //if(hit.collider)
        //    print("isgrounded check "+hit.collider+" "+hit.transform.gameObject.layer+" "+hit.transform.gameObject.name+" "+hit.transform.gameObject.tag);
        return hit.collider && hit.transform.gameObject.layer == LayerMask.NameToLayer("Ground");
    }
    
    private void OnDrawGizmos()
    {
        //grounded check
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * checkGroundDistance);
    }
}
