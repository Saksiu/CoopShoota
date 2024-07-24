using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RoomDoorController : Interactable
{
    //private RoomController owningRoom;

    [SerializeField] private Animator doorAnimator;
    public bool isOpen = false;
    
    private void Start()
    {
        //owningRoom = GetComponentInParent<RoomController>();
    }

    public void Open()
    {
        if(isOpen) return;
        
        isOpen = true;
        doorAnimator.SetTrigger("Open");
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenDoorServerRpc(){
        OpenDoorClientRpc();
        GameMaster.Instance.onRunInit();
    }

    [ClientRpc]
    private void OpenDoorClientRpc(){
        Open();
    }

    public void Close()
    {
        if(!isOpen) return;
        
        isOpen = false;
        doorAnimator.SetTrigger("Close");
    }
    /*private void OnTriggerEnter2D(Collider2D col)
    {
        print("door entered");
        if(col.gameObject==PlayerController.localPlayer.gameObject&&PlayerController.localPlayer.IsOwner)
        {
            print($"player {NetworkManager.LocalClientId} entered door");
            //owningRoom.onDoorEntered(targetRoomId, targetDoorDirection);
        }
    }*/
    public override void Interact()
    {
        OpenDoorServerRpc();
    }

    public override bool IsInteractable()
    {
        return !isOpen;
    }
}