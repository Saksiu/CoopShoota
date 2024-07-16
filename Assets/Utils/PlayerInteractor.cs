using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    private List<Interactable> interactables = new();
    private Interactable currentClosestInteractable;
    private void FixedUpdate()
    {
        currentClosestInteractable=UpdateNearbyInteractables(interactables,currentClosestInteractable);
    }
    private Interactable UpdateNearbyInteractables(List<Interactable> interactablesList, Interactable prevClosest)
    {
        Interactable newClosest = getClosestInteractableFromList(interactablesList);
        
        if(prevClosest&&prevClosest!=newClosest)
            prevClosest.hidePrompt();
        
        if (newClosest)
            newClosest.showPrompt();


        return newClosest;
    }
    private Interactable getClosestInteractableFromList(List<Interactable> interactables)
    {
        if(interactables.Count==0) return null;
        
        Vector2 playerPos=transform.position;
        int closestIndex=-1;
        
        float closestDistance=float.MaxValue;
        float distance;

        for(int i=0;i<interactables.Count;i++)
        {
            distance=Vector2.Distance(playerPos,interactables[i].transform.position);
            if(distance<closestDistance&&interactables[i].IsInteractable())
            {
                closestDistance=distance;
                closestIndex=i;
            }
        }
        if(closestIndex==-1) return null;
        
        return interactables[closestIndex];
    }

    public bool PerformInteraction()
    {
        if (currentClosestInteractable == null) return false;
        
        currentClosestInteractable.Interact();
        return true;
    }
    
    public void AddAvailableInteractable(Interactable toAdd)
    {
        if(!interactables.Contains(toAdd))
            interactables.Add(toAdd);
    }
    public void RemoveAvailableInteractable(Interactable toRemove)
    {
        if (interactables.Contains(toRemove))
        {
            toRemove.hidePrompt();
            interactables.Remove(toRemove);
        }
            
    }
}
