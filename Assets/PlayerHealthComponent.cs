using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealthComponent : NetworkBehaviour
{
    public int maxHP = 10;
    public NetworkVariable<int> HP = new();
    
    public Action<PlayerController> OnDeathAction;

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
        if(HP.Value<=0){
            OnDeathAction.Invoke(GetComponent<PlayerController>());
            OnDeathClientRpc();
        }
            
    }

    [ClientRpc]
    private void OnDeathClientRpc(){
        if(!IsOwner) return;
        OnDeathAction.Invoke(GetComponent<PlayerController>());
    }

    [ServerRpc(RequireOwnership = false)]
    public void resetHPServerRpc()
    {
        SetHPServerRpc(maxHP);
    }

    [ServerRpc]
    private void SetHPServerRpc(int newHP)
    {
        HP.Value = newHP;
    }
    
        
    /*public struct PlayerHealthData: INetworkSerializable
    {
        public int HP;
        public int MaxHP;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HP);
            serializer.SerializeValue(ref MaxHP);
        }
    }*/
}
