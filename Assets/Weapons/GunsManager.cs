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

    //public NetworkList<NetworkObjectReference> unusedGuns = new NetworkList<NetworkObjectReference>();

    private List<GunController> unusedGuns=new List<GunController>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer){
            foreach(GunController gunPrefab in gunPrefabs){
            for(int i = 0; i < TEMP_MAX_GUNS; i++){
                GunController gunInstance = Instantiate(gunPrefab,
                transform.position+new Vector3(0,0,GUN_HOLDING_AREA_DISTANCE*unusedGuns.Count),
                Quaternion.identity,
                transform);
                gunInstance.GetComponent<NetworkObject>().Spawn();
                unusedGuns.Add(gunInstance);
                gunInstance.NetworkObject.TrySetParent(NetworkObject);
            }
            Assert.IsTrue(unusedGuns.Count > 0, "No guns found instantiated under gunsmanager SERVER");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeHeldWeaponServerRpc(NetworkObjectReference playerRef, FixedString32Bytes gunName){

        GunController availableGun = getWeaponToReparent(gunName.ToString());

        Assert.IsNotNull(availableGun, "Failed to find gun of name "+gunName);

        PlayerController player = ((NetworkObject)playerRef).GetComponent<PlayerController>();

        Assert.IsNotNull(player, "Failed to find player with ID "+playerRef.NetworkObjectId);

        //return previous gun to holding area
        if(player.getGunNetworkObject() != null){
            player.getGunNetworkObject().TrySetParent(NetworkObject);
            if(player.getGunNetworkObject().OwnerClientId!=NetworkManager.ServerClientId)
                player.getGunNetworkObject().ChangeOwnership(NetworkManager.ServerClientId);
            player.getGunNetworkObject().transform.position = transform.position+new Vector3(0,0,GUN_HOLDING_AREA_DISTANCE*unusedGuns.Count);
            unusedGuns.Add(player.getGunReference());
            player.currentGun.Value=default;
        }

        //reparent to player
        availableGun.NetworkObject.TrySetParent((NetworkObject)playerRef);
        
        if(availableGun.NetworkObject.OwnerClientId!=player.OwnerClientId)
            availableGun.NetworkObject.ChangeOwnership(player.OwnerClientId);

        Assert.IsNotNull(player.currentGun, "Player current gun is null");
        player.currentGun.Value = availableGun.NetworkObject;

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


