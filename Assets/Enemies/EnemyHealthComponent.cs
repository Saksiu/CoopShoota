using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


//fully server-auth
public class EnemyHealthComponent : NetworkBehaviour
{
    public int maxHP = 10;
    public NetworkVariable<int> HP = new();

    [SerializeField] private Canvas enemyCanvas;
    [SerializeField] private Slider healthBar;
    
    
    public static event Action<EnemyController> OnEnemyDeathAction;
    public event Action<EnemyController> OnDeathLocal;

    public override void OnNetworkSpawn()
    {

        HP.OnValueChanged += onHpChanged;
        HP.Value = maxHP;
        healthBar.maxValue = maxHP;
        healthBar.value = HP.Value;
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= onHpChanged;
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if(PlayerController.localPlayer==null) return;
        enemyCanvas.transform.LookAt(PlayerController.localPlayer.transform);
    }

    private void onHpChanged(int prev, int curr)
    {
       // print("HP changed from "+prev+" to "+curr);
        healthBar.value = curr;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeductHPServerRpc(int amount)
    {
        if(HP.Value<=0) return;
        //print("deducting HP from "+playerName.Value+" by "+amount+" points");
        HP.Value -= amount;
        if(HP.Value<=0){
            OnDeathLocal?.Invoke(GetComponent<EnemyController>());
            OnEnemyDeathAction.Invoke(GetComponent<EnemyController>());
        }
            
    }
}