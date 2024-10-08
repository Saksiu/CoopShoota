using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : NetworkBehaviour, PlayerInputGenerated.IPlayerActions
{
    public static PlayerController localPlayer;

    public static Action<PlayerController> OnPlayerSpawned;
    public NetworkVariable<FixedString64Bytes> playerName=new(
        "",NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    public CameraController playerCamera;
    public PlayerHealthComponent healthComponent;
    
    public PlayerInteractor playerInteractor;
    
    public Rigidbody rb;
    public CapsuleCollider coll;

    public NetworkVariable<NetworkObjectReference> currentGun=new();
    public GunController getGunReference()=>((NetworkObject)currentGun.Value).GetComponent<GunController>();
    public NetworkObject getGunNetworkObject()=>(NetworkObject)currentGun.Value;
    [SerializeField] public Transform CamNozzle;
    
    [SerializeField] private DashingComponent dashComponent;
    [SerializeField] private PlayerJumpingComponent jumpComponent;

    [SerializeField] private TextMeshPro playerNameText;
    
    [SerializeField] private float checkGroundDistance=1.2f;
    [SerializeField] private float walkSpeed=5;

    [SerializeField] private float groundDrag=5f;
    public float getGroundDrag()=>groundDrag;
    [SerializeField] private float airDrag=0.1f;

    [SerializeField] private float stepHeight=0.6f;

    [Tooltip("Don't add mid air force if player is moving faster than this")]
    private bool MovementEnabled = true;

    /// <summary>
    /// Dictionary of player ammo, with gun names as keys
    /// </summary>
    


    //private float verticalAngle=0.0f;

    //public PlayerSessionManager sessionComponent;
    /*private void Awake(){
        sessionComponent=GetComponent<PlayerSessionManager>();
    }*/


    public override void OnNetworkSpawn()
    {
        print("network spawn called on player"+NetworkManager.LocalClientId);
        OnPlayerSpawned?.Invoke(this);
        playerName.OnValueChanged+=setName;
        
        playerCamera.Init(IsOwner);

        playerNameText.text=playerName.Value.ToString();
        if(!IsOwner)
        {
            playerNameText.enabled=true;
            //GetComponent<MeshRenderer>().enabled=true;
            return;
        }
        playerNameText.enabled=false;//you shouldnt see your own name tag
       // GetComponent<MeshRenderer>().enabled=false;

        InputManager.PlayerInput.Player.SetCallbacks(this);
        InputManager.PlayerInput.UI.SetCallbacks(HUDManager.Instance);

        InputManager.PlayerInput.Player.Enable();
        InputManager.PlayerInput.UI.Disable();


        //setNameServerRpc("P"+NetworkManager.LocalClientId);
        
        //no need to verify name here, as we would not start the client if it were invalid
        setNameServerRpc(MainMenuManager.Instance.GetPlayerName());
        HUDManager.Instance.onPlayerSpawn(this);
        localPlayer = this;
        
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged-=setName;
        print("on network despawn called on player"+NetworkManager.LocalClientId+" is owner?"+IsOwner);
        if(!IsOwner)  return;

        InputManager.PlayerInput.Player.RemoveCallbacks(this);
        InputManager.PlayerInput.UI.RemoveCallbacks(HUDManager.Instance);

        //InputManager.PlayerInput.UI.Disable();
        //InputManager.PlayerInput.Player.Disable();
        
        //InputManager.PlayerInput?.Disable();
        
    }
    private void Update()
    {
        playerNameText.GetComponentInParent<Canvas>().transform.LookAt(localPlayer.transform);
    
        if(!IsOwner) return;

        //looking around
        Vector2 lookInput = InputManager.PlayerInput.Player.Look.ReadValue<Vector2>();
        //print("update called on owner, lookinput="+lookInput);

        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + (lookInput.x*playerCamera.cameraSensitivity), 0);
    
        playerCamera.moveCamera(lookInput.y);


    }
    /*public void OnHeldGunChanged(){
        GunController newGun = GetComponentInChildren<GunController>();
        Assert.IsNotNull(newGun, "Failed to find gun in children of player");
        newGun.transform.localPosition = Vector3.zero;
        newGun.gunNozzle=CamNozzle;
        newGun.isControlledByPlayer = true;
        currentGun = getGunReference();

    }*/
    public Transform getGunAnchor(){
        return playerCamera.transform; //correct, but we would have to make the camera its own networkobjects, 
        //spawned after spawning the player, and hooking it up
        //return transform;
    }

    //TODO: constant horizontal movement with lerping as acceleration and slowing down rather than modifying drag
    private void FixedUpdate()
    {
        if(!IsOwner) return;
        if(!MovementEnabled) return;

        Vector2 moveDirInput=InputManager.PlayerInput.Player.Move.ReadValue<Vector2>();
        Vector3 movementDirection = transform.TransformDirection(moveDirInput.x,0,moveDirInput.y);

        if(isGrounded()){
            rb.drag = groundDrag;
            //rb.velocity = new Vector3(movementDirection.x, rb.velocity.y, movementDirection.z);
            rb.AddForce(movementDirection*walkSpeed,ForceMode.VelocityChange);
            Vector3 stepMoveOffset=getStepMovePos(movementDirection);
            //print("got step height "+stepMoveOffset);
            if(stepMoveOffset.magnitude>0){
                rb.MovePosition(stepMoveOffset);
            }
        }
        else{
            rb.drag = airDrag;
            jumpComponent.OnMoveInput(movementDirection);
        }

        //rb.velocity = ;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="moveDir"></param>
    /// <returns>the next step edge position, or Vector3.zero if no steps were found</returns>
    private Vector3 getStepMovePos(Vector3 moveDir){
        //moveDir.Normalize();

        Vector3 basePos=new Vector3(
            coll.bounds.center.x,
            coll.bounds.center.y-((coll.bounds.center.y-coll.bounds.min.y)*0.95f),
            //coll.bounds.center.y,
            coll.bounds.center.z);

        float checkDistance=coll.bounds.extents.x;
        RaycastHit[] hits=Physics.RaycastAll(basePos, moveDir.normalized*checkDistance, checkDistance);

        //Physics.Raycast(basePos, new Vector3(moveDir.x,0,moveDir.y), out RaycastHit hit, checkDistance);
        Debug.DrawRay(basePos, moveDir.normalized*checkDistance, Color.blue, 0.1f);

        foreach(var h in hits){
           // print("hit "+h.collider.gameObject.name+" "+h.collider.gameObject.layer);
            if(h.collider.gameObject.layer==LayerMask.NameToLayer("Ground")){
                return new Vector3(h.point.x, h.point.y+(stepHeight*2.0f), h.point.z);
            }
        }
        return Vector3.zero;
    }

    #region input
    

    public void OnWeaponSwitch(InputAction.CallbackContext context)
    {
        if(!context.performed) return;

        if(currentGun==null||getGunNetworkObject()==null||getGunReference()==null){
            GunsManager.Instance.ChangeHeldWeaponServerRpc(this.NetworkObject,"AKM_Rifle");
        }
        else if(getGunReference().gunName=="AKM_Rifle"){
            GunsManager.Instance.ChangeHeldWeaponServerRpc(this.NetworkObject,"Pump_Shotgun");
        }
        else if(getGunReference().gunName=="Pump_Shotgun"){
            GunsManager.Instance.ChangeHeldWeaponServerRpc(this.NetworkObject,"AKM_Rifle");
        }
        else{
            Debug.LogError("Reached illegal state: currently held gun could not be found in list, how??");
        }
    }
    public void OnReload(InputAction.CallbackContext context)
    {
        if(!context.performed) return;

        /*

        if(currentGun==null||getGunNetworkObject()==null||getGunReference()==null){
            print("No gun to reload");
            return;
        }

        getGunReference().Reload();
        */
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if(context.performed){
            Vector2 moveDirInput=InputManager.PlayerInput.Player.Move.ReadValue<Vector2>();
            Vector3 movementDirection = transform.TransformDirection(moveDirInput.x,0,moveDirInput.y);
            dashComponent.Dash(movementDirection);
        }
            
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.performed)
            jumpComponent.Jump();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if(context.performed)
            playerInteractor.PerformInteraction();
    }

    //empty as we read their values on updates
    public void OnMove(InputAction.CallbackContext context) {}
    public void OnLook(InputAction.CallbackContext context) {}
    public void OnShoot(InputAction.CallbackContext context){}

    
    public void OnMainMenu(InputAction.CallbackContext context)
    {
        if(!context.performed) return;

        InGameMenuManager.Instance.toggleInGameMenu(IsHost);
    }

    #endregion
    public void onDashFromComponent(float duration)
    {
        MovementEnabled = false;
        rb.drag=airDrag;
        Invoke(nameof(enableMovement),duration);
    }
    public void enableMovement()=>MovementEnabled = true;
    public void disableMovement(){
        MovementEnabled = false;
        rb.velocity = Vector3.zero;
        rb.useGravity = true;
    }

    public void switchToUIInput(){
        InputManager.PlayerInput.UI.Enable();
        InputManager.PlayerInput.Player.Disable();
    }

    
    public void switchToPlayerInput(){
        InputManager.PlayerInput.Player.Enable();
        InputManager.PlayerInput.UI.Disable();
    }
    
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

    public void changePlayerName(string newName){
        setNameServerRpc(newName);
    }
    private void setName(FixedString64Bytes prevName, FixedString64Bytes newName)
    {
        print("setting name to "+newName+" for player P"+NetworkManager.LocalClientId+"!");
        playerName.Value = newName;
        //playerNameText.enabled = true;
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

}
