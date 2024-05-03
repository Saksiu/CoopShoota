using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName=new(
        "",NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    public PlayerHealthComponent healthComponent;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed;
    
    private bool MovementEnabled = true;
    
    
    public override void OnNetworkSpawn()
    {
        print("network spawn called on player"+NetworkManager.LocalClientId);
        playerName.OnValueChanged+=setName;
        //HP.OnValueChanged+=onHpChanged;
        if(!IsOwner)
        {
            enabled = false;
            return;
        }
        //healthComponent
        //SetHPServerRpc(maxHP);
        
        //if(!IsOwner) enabled = false;
        base.OnNetworkSpawn();
    }
    //public override onne
    public void setName(FixedString64Bytes prevName, FixedString64Bytes newName)
    {
        print("setting name to "+newName+" for player P"+NetworkManager.LocalClientId+"!");
        playerName.Value = newName;
        TextMeshPro playerNameText = GetComponentInChildren<TextMeshPro>();
        playerNameText.enabled = true;
        playerNameText.text=newName.ToString();
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged-=setName;
       // HP.OnValueChanged-=onHpChanged;
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
    /*private void onHpChanged(int prev, int curr)
    {
        if(!IsOwner) return;
        //print("HP changed from "+prev+" to "+curr);
        UIManager.Instance.updateDisplayedHP(curr);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeductHPServerRpc(int amount)
    {
        if(HP.Value<=0) return;
        print("deducting HP from "+playerName.Value+" by "+amount+" points");
        HP.Value -= amount;
        if(HP.Value<=0)
        {
            GameMaster.Instance.onPlayerDeath(this);
        }
    }

    public void onPlayerDeath()
    {
        GameMaster.Instance.onPlayerDeath(this);
    }
    [ServerRpc]
    private void SetHPServerRpc(int newHP)
    {
        HP.Value = newHP;
    }*/
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
