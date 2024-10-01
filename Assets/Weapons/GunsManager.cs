using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;
using UnityEditor.PackageManager;


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
    public static Dictionary<ulong, Dictionary<string,uint>> playerAmmoDict = new Dictionary<ulong, Dictionary<string,uint>>();

    public bool hasAmmoForKey(ulong playerID, string gunName)=>playerAmmoDict[playerID].ContainsKey(gunName);
    public uint getAmmoLeft(ulong playerID, string gunName)=>playerAmmoDict[playerID][gunName];
    
    [ServerRpc(RequireOwnership = false)]
    public void OnGunEquippedServerRpc(ulong playerID, string gunName, uint initAmmo){
        print("on gun equipped server rpc for player P"+playerID+" with gun "+gunName);
        if(!playerAmmoDict.ContainsKey(playerID)) playerAmmoDict.Add(playerID,new Dictionary<string, uint>());
        if(!playerAmmoDict[playerID].ContainsKey(gunName)){
            playerAmmoDict[playerID].Add(gunName,0);
            setAmmoServerRpc(playerID,gunName,initAmmo);
        }else{
            setAmmoServerRpc(playerID,gunName,getAmmoLeft(playerID,gunName));
        }
    }

    [ServerRpc]
    public void setAmmoServerRpc(ulong playerID, string gunName, uint ammo){
        print("setting ammo "+ammo+" to "+gunName+" for player P"+playerID);
        //if(playerAmmoDict[playerID][gunName]==ammo) return;

        playerAmmoDict[playerID][gunName]=ammo;
        setAmmoClientRpc(ammo,new ClientRpcParams{
            Send=new ClientRpcSendParams{
                TargetClientIds=new List<ulong>{playerID}}});
    }

    [ClientRpc]
    private void setAmmoClientRpc(uint newAmmo, ClientRpcParams receiveParams){
        PlayerController.localPlayer.getGunReference().onAmmoChangedClientRpc(newAmmo);
    }


    [ServerRpc]
    public void addAmmoServerRpc(ulong playerID, string gunName, uint ammo){
        print("adding ammo "+ammo+" to "+gunName+" for player P"+NetworkManager.LocalClientId);
        if(!hasAmmoForKey(playerID,gunName)) playerAmmoDict[playerID].Add(gunName,ammo);
        else setAmmoServerRpc(playerID,gunName,getAmmoLeft(playerID,gunName)+ammo);
        //getGunReference().AmmoLeft=playerAmmoDict[playerID][gunName];
    }


    [ServerRpc(RequireOwnership = false)]
    public void addAmmoToCurrentlyHeldGunServerRpc(ulong playerID, uint ammo){
        /*if(currentGun==null||getGunNetworkObject()==null||getGunReference()==null){
            print("No gun held to add ammo to, how?");
            return;
        }*/
        addAmmoServerRpc(playerID,PlayerController.localPlayer.getGunReference().gunName,ammo);
    }

    private List<GunController> unusedGuns=new List<GunController>();

    private void handlePlayerDisconnected(ulong playerID){
        if(playerAmmoDict.ContainsKey(playerID))
            playerAmmoDict.Remove(playerID);
    }

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

                NetworkManager.OnClientDisconnectCallback += handlePlayerDisconnected;
            }
            Assert.IsTrue(unusedGuns.Count > 0, "No guns found instantiated under gunsmanager SERVER");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer){
            NetworkManager.OnClientDisconnectCallback -= handlePlayerDisconnected;   
        }
        base.OnNetworkDespawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeHeldWeaponServerRpc(NetworkObjectReference playerRef, FixedString32Bytes gunName){

        GunController availableGun = getWeaponToReparent(gunName.ToString());

        Assert.IsNotNull(availableGun, "Failed to find gun of name "+gunName);

        PlayerController player = ((NetworkObject)playerRef).GetComponent<PlayerController>();

        Assert.IsNotNull(player, "Failed to find player with ID "+playerRef.NetworkObjectId);

        //return previous gun to holding area
        if(player.getGunNetworkObject() != null){
            if(player.getGunNetworkObject().OwnerClientId!=NetworkManager.ServerClientId)
                player.getGunNetworkObject().ChangeOwnership(NetworkManager.ServerClientId);
        
            player.getGunNetworkObject().TrySetParent(NetworkObject);
            player.getGunNetworkObject().transform.position = transform.position+new Vector3(0,0,GUN_HOLDING_AREA_DISTANCE*unusedGuns.Count);
            unusedGuns.Add(player.getGunReference());
            player.currentGun.Value=default;
        }

        print($"setting {player.playerName.Value} player current gun");

        if(availableGun.NetworkObject.OwnerClientId!=player.OwnerClientId)
            availableGun.NetworkObject.ChangeOwnership(player.OwnerClientId);

            //reparent to player
        
        player.currentGun.Value = availableGun.NetworkObject;

        ReparentGunNetworkObject(playerRef, availableGun);
        
    }

    private IEnumerator ReparentGunNetworkObject(NetworkObjectReference playerRef, GunController toReparent){
        yield return new WaitForFixedUpdate();
        toReparent.NetworkObject.TrySetParent((NetworkObject)playerRef);
        Assert.IsNotNull(((NetworkObject)playerRef).GetComponent<PlayerController>().currentGun, "Player current gun is null");
        //remove from unused guns
        unusedGuns.Remove(toReparent);
        }

    public GunController getWeaponToReparent(string gunName){
        foreach(GunController gun in unusedGuns)
            if(gun.gunName == gunName)
                return gun;
        return null; //failed to find gun of that name
    }

}


