using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Interactable : NetworkBehaviour
{
    [SerializeField] private string promptText;


    /*public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GetComponent<Collider>().enabled = false;
            enabled = false;
        }
            
    }*/

    protected virtual void OnTriggerEnter(Collider other)
    {
        if(other.gameObject==PlayerController.localPlayer.gameObject)
            PlayerController.localPlayer.playerInteractor.AddAvailableInteractable(this);
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if(other.gameObject==PlayerController.localPlayer.gameObject)
            PlayerController.localPlayer.playerInteractor.RemoveAvailableInteractable(this);
    }

    public virtual void showPrompt()
    {
        HUDManager.Instance.showPrompt("[E] "+promptText);
    }

    public virtual void hidePrompt()
    {
        HUDManager.Instance.hidePrompt();
    }

    public abstract void Interact();
    public abstract bool IsInteractable();
}
