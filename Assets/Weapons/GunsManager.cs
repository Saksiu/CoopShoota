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

    public bool hasAmmoForKey(ulong clientID, string gunName)=>playerAmmoDict[clientID].ContainsKey(gunName);
    public uint getAmmoLeft(ulong clientID, string gunName)=>playerAmmoDict[clientID][gunName];
    
    [ServerRpc(RequireOwnership = false)]
    public void OnGunEquippedServerRpc(ulong clientID, string gunName, uint initAmmo){
        print("on gun equipped server rpc for player P"+clientID+" with gun "+gunName);
        if(!playerAmmoDict.ContainsKey(clientID)) playerAmmoDict.Add(clientID,new Dictionary<string, uint>());
        if(!playerAmmoDict[clientID].ContainsKey(gunName)){
            playerAmmoDict[clientID].Add(gunName,initAmmo);
            setAmmoServerRpc(clientID,gunName,initAmmo);
        }else{
            setAmmoServerRpc(clientID,gunName,getAmmoLeft(clientID,gunName));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void setAmmoServerRpc(ulong clientID, string gunName, uint ammo){
        print("setting ammo "+ammo+" to "+gunName+" for player P"+clientID);
        //if(playerAmmoDict[clientID][gunName]==ammo) return;
        
        playerAmmoDict[clientID][gunName]=ammo;
        setAmmoClientRpc(gunName,ammo,new ClientRpcParams{
            Send=new ClientRpcSendParams{
                TargetClientIds=new ulong[]{clientID}}});
    }

    [ClientRpc]
    private void setAmmoClientRpc(string gunName,uint newAmmo, ClientRpcParams receiveParams = default){
        print("setting ammo client rpc "+newAmmo+" on client "+NetworkManager.LocalClientId);
        if(PlayerController.localPlayer.getGunReference().gunName==gunName)
            PlayerController.localPlayer.getGunReference().AmmoLeft=newAmmo;
    }


    [ServerRpc(RequireOwnership = false)]
    public void addAmmoServerRpc(ulong clientID, string gunName, int change){
        print("adding ammo "+change+" to "+gunName+" for player P"+NetworkManager.LocalClientId);
        
        if(!hasAmmoForKey(clientID,gunName)&&change>=0) {playerAmmoDict[clientID].Add(gunName,(uint)change);}
        else {
            if(playerAmmoDict[clientID][gunName]+change<0){
                Debug.LogError($"attempting to change {gunName} weapon ammo for client {clientID} resulting in negative number!!!\n aborting");
                return;
                }
            }
            setAmmoServerRpc(clientID,gunName,(uint)((int)getAmmoLeft(clientID,gunName)+change));
        //getGunReference().AmmoLeft=playerAmmoDict[clientID][gunName];
    }

    [ServerRpc(RequireOwnership = false)]
    public void addAmmoServerRpc(ulong clientID, string gunName, uint change)=>addAmmoServerRpc(clientID,gunName,(int)change);    


    [ServerRpc(RequireOwnership = false)]
    public void addAmmoToCurrentlyHeldGunServerRpc(ulong clientID, uint ammo){
        /*if(currentGun==null||getGunNetworkObject()==null||getGunReference()==null){
            print("No gun held to add ammo to, how?");
            return;
        }*/
        addAmmoServerRpc(clientID,PlayerController.localPlayer.getGunReference().gunName,ammo);
    }

    private List<GunController> unusedGuns=new List<GunController>();

    private void handlePlayerDisconnected(ulong clientID){
        if(playerAmmoDict.ContainsKey(clientID))
            playerAmmoDict.Remove(clientID);
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

        StartCoroutine(ReparentGunNetworkObject(playerRef, availableGun));
        
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


