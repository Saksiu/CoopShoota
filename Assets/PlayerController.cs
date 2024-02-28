using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    public string playerName{private set; get;}
    public static int maxHP = 10;
    public NetworkVariable<int> HP = new(maxHP);

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed;
    
    private bool MovementEnabled = true;
    
    
    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
        RequestClientNamesUpdateServerRpc(NetworkObjectId, NetworkManager.LocalClientId);
        //print("network spawn called on player"+NetworkManager.LocalClientId);
        HP.OnValueChanged+=onHpChanged;
        SetHPServerRpc(maxHP);
        
        if(!IsOwner) enabled = false;
    }

    [ServerRpc]
    private void RequestClientNamesUpdateServerRpc(ulong playerObjectId,  ulong playerId)
    {
        
        ClientNameUpdateClientRpc(playerObjectId, playerId);
    }
    [ClientRpc]
    private void ClientNameUpdateClientRpc(ulong playerObjectId, ulong playerId)
    {
        //if(!IsOwner) return;
        var player = NetworkManager.SpawnManager.SpawnedObjects[playerObjectId].GetComponent<PlayerController>();
        if(player==null)
        {
            print("player not found");
            return;
        }
        player.playerName = "Player " + playerId;
        player.GetComponentInChildren<TextMeshPro>().text=playerId.ToString();
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged-=onHpChanged;
    }

    private void FixedUpdate()
    {
        //print("update!");
        //if(!IsOwner) return;
        float finalSpeedHorizontal=0;
        float finalSpeedVertical=0;

        if(Input.GetKey(KeyCode.D))
            finalSpeedHorizontal+=speed;
        if(Input.GetKey(KeyCode.A))
            finalSpeedHorizontal-=speed;
        if(Input.GetKey(KeyCode.W))
            finalSpeedVertical+=speed;
        if(Input.GetKey(KeyCode.S))
            finalSpeedVertical-=speed;
        
        if(!MovementEnabled) return;
        //not sure if I want to keep this, but it fixes some issues when NetworkRigidbody2D is attached to Player Prefab
        if(finalSpeedHorizontal!=0)
            rb.velocity = new Vector2(finalSpeedHorizontal, rb.velocity.y);
        if(finalSpeedVertical!=0)
            rb.velocity = new Vector2(rb.velocity.x, finalSpeedVertical);
    }

    public void onDash(float duration)
    {
        MovementEnabled = false;
        Invoke(nameof(enableMovement),duration);
    }
    private void enableMovement()=>MovementEnabled = true;
    private void onHpChanged(int prev, int curr)
    {
        if(!IsOwner) return;
        //print("HP changed from "+prev+" to "+curr);
        UIManager.Instance.updateDisplayedHP(curr);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeductHPServerRpc(int amount)
    {
        if(HP.Value<=0) return;
        HP.Value -= amount;
        if(HP.Value<=0)
        {
            GameMaster.Instance.onPlayerDeath(this);
        }
    }
    [ServerRpc]
    public void SetHPServerRpc(int newHP)
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
