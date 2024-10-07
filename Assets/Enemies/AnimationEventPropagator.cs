using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventPropagator : MonoBehaviour
{
    public event Action<string> AnimationEventAction;
    
    public void CallAnimationEvent(string eventName){
        AnimationEventAction?.Invoke(eventName);
    }
}
