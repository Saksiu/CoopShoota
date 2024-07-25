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
        RoomController.Instance.isRunActive.OnValueChanged += OnRunStateChanged;
        base.OnNetworkSpawn();
    }
    public override void OnNetworkDespawn()
    {
        RoomController.Instance.isRunActive.OnValueChanged -= OnRunStateChanged;
        base.OnNetworkDespawn();
    }

    private void OnRunStateChanged(bool prev, bool curr)
    {
        if(curr)
            Open();
        else
            Close();
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