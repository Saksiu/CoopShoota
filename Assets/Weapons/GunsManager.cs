using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;


/**
* instantiates required amount of each gun and keeps track of them
* grants transform and facilities returning the previous gun to a default off-map location
* and reparenting the new gun to the player
*
*/
public class GunsManager : SingletonNetwork<GunsManager>
{
    private const int TEMP_MAX_GUNS = 4;

    private const float GUN_HOLDING_AREA_DISTANCE = 5.0f;

    // Start is called before the first frame update
    [SerializeField] private List<GunController> gunPrefabs;

    private List<GunController> unusedGuns=new List<GunController>();

    public override void OnNetworkSpawn()
    {
        if(!IsServer) return;


        foreach(GunController gunPrefab in gunPrefabs){
            for(int i = 0; i < TEMP_MAX_GUNS; i++){
                GunController gunInstance = Instantiate(gunPrefab,
                transform.position+new Vector3(0,0,GUN_HOLDING_AREA_DISTANCE*unusedGuns.Count),
                Quaternion.identity,
                transform);
                gunInstance.GetComponent<NetworkObject>().Spawn();
                unusedGuns.Add(gunInstance);
                gunInstance.transform.SetParent(transform,true);
            }
        }
        unusedGuns = GetComponentsInChildren<GunController>().ToList();
        Assert.IsTrue(unusedGuns.Count > 0, "No guns found instantiated under");


        base.OnNetworkSpawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeHeldWeaponServerRpc(ulong playerID, FixedString32Bytes gunName){
        GunController availableGun = getWeaponToReparent(gunName.ToString());

        Assert.IsNotNull(availableGun, "Failed to find gun of name "+gunName);

        PlayerController player = GameMaster.Instance.getPlayer(playerID);
        //reparent to player
        //availableGun.transform.SetParent(player.transform,false);
        availableGun.NetworkObject.TryRemoveParent();
        availableGun.gunAnchor=player.playerCamera.transform.GetChild(2);
        availableGun.gunNozzle=player.CamNozzle;
        availableGun.isControlledByPlayer = true;

        //remove from unused guns
        unusedGuns.Remove(availableGun);
    }

    public GunController getWeaponToReparent(string gunName){
        foreach(GunController gun in unusedGuns)
            if(gun.gunName == gunName)
                return gun;
        return null; //failed to find gun of that name
    }

}


