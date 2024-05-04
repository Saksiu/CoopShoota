using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName=new(
        "",NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    [SerializeField] private Camera playerCamera;
    
    public PlayerHealthComponent healthComponent;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private float walkSpeed=5;
    [SerializeField] private float cameraSensitivity=10;
    
    [Tooltip("More=more rotation freedom")]
    [SerializeField] private float verticalCameraClamp = 40;
    
    private bool MovementEnabled = true;
    
    
    public override void OnNetworkSpawn()
    {
        print("network spawn called on player"+NetworkManager.LocalClientId);
        playerName.OnValueChanged+=setName;
        //HP.OnValueChanged+=onHpChanged;
        if(!IsOwner)
        {
            enabled = false;
            return;
        }
        //healthComponent
        //SetHPServerRpc(maxHP);
        
        //if(!IsOwner) enabled = false;
        base.OnNetworkSpawn();
    }
    //public override onne
    public void setName(FixedString64Bytes prevName, FixedString64Bytes newName)
    {
        print("setting name to "+newName+" for player P"+NetworkManager.LocalClientId+"!");
        playerName.Value = newName;
        TextMeshPro playerNameText = GetComponentInChildren<TextMeshPro>();
        playerNameText.enabled = true;
        playerNameText.text=newName.ToString();
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged-=setName;
    }


    private float verticalAngle=0.0f;

    private void Update()
    {
        // Player rotation on the Y axis (horizontal)
        float lookH = Input.GetAxis("Mouse X") * cameraSensitivity;
        transform.Rotate(Vector3.up, lookH);

        // Camera rotation on the X axis (vertical)
        verticalAngle -= Input.GetAxis("Mouse Y") * cameraSensitivity; // Subtract to invert the vertical input
        verticalAngle = Mathf.Clamp(verticalAngle, -verticalCameraClamp, verticalCameraClamp); // Clamp the vertical angle within the limits

        // Apply rotation to the camera using Quaternion to avoid gimbal lock issues
        playerCamera.transform.localRotation = Quaternion.Euler(verticalAngle, 0, 0);
    }

    private void FixedUpdate()
    {
        
        //print("update!");
        //if(!IsOwner) return;

        if(!MovementEnabled) return;
            
        Vector2 moveDir=Vector2.zero;

        if(Input.GetKey(KeyCode.D))
            moveDir.x+=walkSpeed;
        if(Input.GetKey(KeyCode.A))
            moveDir.x-=walkSpeed;
        if(Input.GetKey(KeyCode.W))
            moveDir.y+=walkSpeed;
        if(Input.GetKey(KeyCode.S))
            moveDir.y-=walkSpeed;
        
        Vector3 movementDirection = transform.right * moveDir.x + transform.forward * moveDir.y;
        rb.velocity = new Vector3(movementDirection.x, rb.velocity.y, movementDirection.z);

        //rb.velocity = ;

    }

    public void onDash(float duration)
    {
        MovementEnabled = false;
        Invoke(nameof(enableMovement),duration);
    }
    private void enableMovement()=>MovementEnabled = true;
    
    /*public struct PlayerHealthData: INetworkSerializable
    {
        public int HP;
        public int MaxHP;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HP);
            serializer.SerializeValue(ref MaxHP);
        }
    }*/
}
