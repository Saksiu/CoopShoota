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

    public CameraController playerCamera;

    public static PlayerController localPlayer;
    
    public PlayerHealthComponent healthComponent;

    public Rigidbody rb;
    [SerializeField] private float walkSpeed=5;

    private bool MovementEnabled = true;
    
    
    public override void OnNetworkSpawn()
    {
        print("network spawn called on player"+NetworkManager.LocalClientId);
        playerName.OnValueChanged+=setName;
        //HP.OnValueChanged+=onHpChanged;
        playerCamera.Init(IsOwner);
        
        if(!IsOwner)
        {
            GetComponentInChildren<PlayerJumpingComponent>().enabled = false;
            enabled = false;
            return;
        }
        localPlayer = this;

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
        float lookH = Input.GetAxis("Mouse X") * playerCamera.cameraSensitivity;
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + lookH, 0);
        //transform.Rotate(Vector3.up, lookH);
        
        if(playerCamera==null||(!playerCamera.isActiveAndEnabled)) return;
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
        //rb.AddForce(new Vector3(movementDirection.x, rb.velocity.y, movementDirection.z),ForceMode.Force);
        //transform.Translate(new Vector3(movementDirection.x, rb.velocity.y, movementDirection.z));
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
