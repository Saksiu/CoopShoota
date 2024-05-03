using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealthComponent : NetworkBehaviour
{
    public int maxHP = 10;
    public NetworkVariable<int> HP = new();
    
    
    [SerializeField] private UnityEvent onDeath;

    public override void OnNetworkSpawn()
    {
        HP.OnValueChanged += onHpChanged;
        
        if(!IsOwner) return;
        
        SetHPServerRpc(maxHP);
        //print("HP set to "+HP.Value+"!");
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= onHpChanged;
        base.OnNetworkDespawn();
    }
    
    private void onHpChanged(int prev, int curr)
    {
        if(!IsOwner) return;
        //print("onHpChanged called");
        UIManager.Instance.updateDisplayedHP(curr);
        
        //pint("HP changed from "+prev+" to "+curr);
        //UIManager.Instance.updateDisplayedHP(curr);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeductHPServerRpc(int amount)
    {
        if(HP.Value<=0) return;
        //print("deducting HP from "+playerName.Value+" by "+amount+" points");
        HP.Value -= amount;
        if(HP.Value<=0)
            onDeath?.Invoke();
    }

    [ServerRpc]
    private void SetHPServerRpc(int newHP)
    {
        HP.Value = newHP;
    }
}
