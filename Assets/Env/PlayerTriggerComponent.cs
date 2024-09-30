using UnityEngine;
using UnityEngine.Events;

public class PlayerTriggerComponent : MonoBehaviour
{
        [SerializeField] private bool requireOwner = true;
    [SerializeField] private UnityEvent onPlayerTriggerEnter;

    private void OnTriggerEnter(Collider other){
        if(PlayerController.localPlayer==null) return;
        if(requireOwner&&!PlayerController.localPlayer.IsOwner)
            return;

        if(other.TryGetComponent(out PlayerController player)){
            onPlayerTriggerEnter.Invoke();
        }
    }
}
