using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public class EventInteractable : Interactable
{

    public UnityEvent onInteract;
    
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;

    private bool overrideIsInteractableValue = true;

    public void overrideIsInteractable(bool isInteractable)
    {
        overrideIsInteractableValue = isInteractable;
    }
    
    protected new virtual void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        
        if(other.GetComponent<PlayerController>())
            onPlayerEnter?.Invoke();
    }

    protected new virtual void OnTriggerExit(Collider other)
    {
        base.OnTriggerExit(other);
        
        if(other.GetComponent<PlayerController>())
            onPlayerExit?.Invoke();
    }

    public override void Interact()
    {
        onInteract?.Invoke();
    }

    public override bool IsInteractable()
    {
        return overrideIsInteractableValue;
    }
}
