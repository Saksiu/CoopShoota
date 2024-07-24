using System;
using System.Numerics;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : NetworkBehaviour, PlayerInputGenerated.IPlayerActions
{
    public static PlayerController localPlayer;
    public NetworkVariable<FixedString64Bytes> playerName=new(
        "",NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    private PlayerInputGenerated input;
    public CameraController playerCamera;
    public PlayerHealthComponent healthComponent;
    
    public PlayerInteractor playerInteractor;
    
    public Rigidbody rb;
    
    [SerializeField] private DashingComponent dashComponent;
    [SerializeField] private PlayerJumpingComponent jumpComponent;
    
    [SerializeField] private float checkGroundDistance=1.2f;
    [SerializeField] private float walkSpeed=5;

    [SerializeField] private float groundDrag=5f;
    [SerializeField] private float airDrag=0.1f;

    [Tooltip("Don't add mid air force if player is moving faster than this")]
    private bool MovementEnabled = true;
    private float verticalAngle=0.0f;

    public override void OnNetworkSpawn()
    {
        print("network spawn called on player"+NetworkManager.LocalClientId);
        playerName.OnValueChanged+=setName;
        
        playerCamera.Init(IsOwner);

        TextMeshPro playerNameText = GetComponentInChildren<TextMeshPro>();
        playerNameText.text=playerName.Value.ToString();
        if(!IsOwner)
        {
            //GetComponentInChildren<PlayerJumpingComponent>().enabled = false;
            //GetComponent<PlayerInput>().enabled = false;
            //PlayerInteractor.Instance.enabled = false;
            enabled = false;
            return;
        }

        input = new PlayerInputGenerated();
        input.Player.SetCallbacks(this);
        input.Enable();
        setNameServerRpc("P"+NetworkManager.LocalClientId);
        localPlayer = this;
        
        base.OnNetworkSpawn();
    }

    private void Update()
    {

        //looking around
        Vector2 lookInput = input.Player.Look.ReadValue<Vector2>();

        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + (lookInput.x*playerCamera.cameraSensitivity), 0);
    
        playerCamera.moveCamera(lookInput.y);
    }
    private void FixedUpdate()
    {
        Vector2 moveDirInput=input.Player.Move.ReadValue<Vector2>();
        Vector3 movementDirection = transform.TransformDirection(moveDirInput.x,0,moveDirInput.y);
        
        if(!MovementEnabled) return;

        if(isGrounded()){
            rb.drag = groundDrag;
            //rb.velocity = new Vector3(movementDirection.x, rb.velocity.y, movementDirection.z);
            rb.AddForce(movementDirection*walkSpeed,ForceMode.VelocityChange);
        }
        else{
            rb.drag = airDrag;
            jumpComponent.OnMoveInput(movementDirection);
        }

        //rb.velocity = ;

    }

    #region input
    
    //empty as we need to read their values on update functions anyway
    public void OnMove(InputAction.CallbackContext context) {}
    public void OnLook(InputAction.CallbackContext context) {}

    public void OnFire(InputAction.CallbackContext context)
    {
        print("fire!");
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if(context.performed){
            dashComponent.Dash();
        }
            
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.performed)
            jumpComponent.Jump();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        playerInteractor.PerformInteraction();
    }

    #endregion
    public void onDashFromComponent(float duration)
    {
        MovementEnabled = false;
        rb.drag=airDrag;
        Invoke(nameof(enableMovement),duration);
    }
    private void enableMovement()=>MovementEnabled = true;
    
    
    private bool wasGrounded = false;
    public bool isGrounded()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.down, out hit, checkGroundDistance);
        //if(hit.collider)
        //    print("isgrounded check "+hit.collider+" "+hit.transform.gameObject.layer+" "+hit.transform.gameObject.name+" "+hit.transform.gameObject.tag);
        bool grounded=hit.collider && hit.transform.gameObject.layer == LayerMask.NameToLayer("Ground");

        if (grounded&&(!wasGrounded))
        {
            onTouchGround();
            wasGrounded = true;
            return grounded;
        }
        if((!grounded)&&wasGrounded)
        {
            onLeaveGround();
            wasGrounded = false;
            return grounded;
        }
        return grounded;
    }

    private void onTouchGround()
    {
        //print("on touch ground!");
        //rb.velocity = Vector3.zero;
    }
    private void onLeaveGround()
    {
        //print("on leave ground!");
    }

    private void setName(FixedString64Bytes prevName, FixedString64Bytes newName)
    {
        print("setting name to "+newName+" for player P"+NetworkManager.LocalClientId+"!");
        playerName.Value = newName;
        TextMeshPro playerNameText = GetComponentInChildren<TextMeshPro>();
        playerNameText.enabled = true;
        playerNameText.text=newName.ToString();
    }
    
    [ServerRpc]
    private void setNameServerRpc(FixedString64Bytes newName)=> playerName.Value = newName;

    
    private void OnDrawGizmos()
    {
        //grounded check
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * checkGroundDistance);
    }
    
    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged-=setName;
        if(!IsOwner)  return;
        
        input.Disable();
        
    }
}
