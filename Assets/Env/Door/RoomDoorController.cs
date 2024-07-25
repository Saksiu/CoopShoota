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

    public override void OnNetworkSpawn()
    {
        RoomController.Instance.OnRunStartAction += Open;
        RoomController.Instance.OnRunEndAction += Close;
        //print("door spawned on network, checking runstate "+RoomController.Instance.isRunActive.Value);

        base.OnNetworkSpawn();
    }
    public override void OnNetworkDespawn()
    {
        RoomController.Instance.OnRunStartAction -= Open;
        RoomController.Instance.OnRunEndAction -= Close;
        base.OnNetworkDespawn();
    }


    public void Open()
    {
        if(isOpen) return;
        
        isOpen = true;
        doorAnimator.SetTrigger("Open");
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenDoorServerRpc(){
        //OpenDoorClientRpc();
        RoomController.Instance.setIsRunActiveServerRpc(true);
        //GameMaster.Instance.startRun();
    }

 /*   [ClientRpc]
    private void OpenDoorClientRpc(){
        Open();
        //GameMaster.Instance.startRun();
    }*/

    public void Close(bool win)
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