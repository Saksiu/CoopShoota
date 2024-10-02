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
    private static Dictionary<ulong, Dictionary<string,int>> playerAmmoDict = new Dictionary<ulong, Dictionary<string,int>>();
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
                NetworkManager.OnClientConnectedCallback+=handlePlayerConnected;
                NetworkManager.OnClientDisconnectCallback += handlePlayerDisconnected;
            }
            Assert.IsTrue(unusedGuns.Count > 0, "No guns found instantiated under gunsmanager SERVER");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer){
            
            NetworkManager.OnClientConnectedCallback-=handlePlayerConnected;
            NetworkManager.OnClientDisconnectCallback -= handlePlayerDisconnected;   
        }
        playerAmmoDict.Clear();
        base.OnNetworkDespawn();
    }

    private void handlePlayerConnected(ulong clientID){
        if(!playerAmmoDict.ContainsKey(clientID)){
            playerAmmoDict.Add(clientID,new Dictionary<string, int>());
            foreach(GunController gun in gunPrefabs)
                playerAmmoDict[clientID].Add(gun.gunName,gun.initialAmmo);
        }
    }

    private void handlePlayerDisconnected(ulong clientID){
        if(playerAmmoDict.ContainsKey(clientID))
            playerAmmoDict.Remove(clientID);
    }

    public bool hasAmmoForKey(ulong clientID, string gunName)=>playerAmmoDict[clientID].ContainsKey(gunName);
    public int getAmmoLeft(ulong clientID, string gunName)=>playerAmmoDict[clientID][gunName];
    
    [ServerRpc(RequireOwnership = false)]
    public void OnGunEquippedServerRpc(ulong clientID, string gunName){

        setAmmoServerRpc(clientID,gunName,getAmmoLeft(clientID,gunName));
        print($"OnGunEquippedServerRpc for client {clientID} ended data:");
        printAllAmmoInfo();
    }

    [ServerRpc(RequireOwnership = false)]
    public void resetAllAmmoServerRpc(ulong clientID){
        foreach(var gun in playerAmmoDict[clientID].Keys)
            setAmmoServerRpc(clientID,gun,gunPrefabs.Find(g=>g.gunName==gun).initialAmmo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void setAmmoServerRpc(ulong clientID, string gunName, int ammo){
        
        playerAmmoDict[clientID][gunName]=ammo;
        setAmmoClientRpc(gunName,ammo,new ClientRpcParams{
            Send=new ClientRpcSendParams{
                TargetClientIds=new ulong[]{clientID}}});
        print("setAmmoServerRpc ended data:");
        printAllAmmoInfo();
    }

    [ClientRpc]
    private void setAmmoClientRpc(string gunName,int newAmmo, ClientRpcParams receiveParams = default){
        if(PlayerController.localPlayer.getGunReference()==null||PlayerController.localPlayer.getGunReference().gunName!=gunName){
            print("local player is null, shifting to coroutine");
            StartCoroutine(setAmmoClientRpcCoroutine(gunName,newAmmo));
            return;
        }
        if(PlayerController.localPlayer.getGunReference().gunName==gunName){
            print("requested gun name matches current gun name, setting ammo");
            PlayerController.localPlayer.getGunReference().AmmoLeft=newAmmo;
        }else{
            print("requested gun name does not match current gun name, aborting");
        }

        print("setAmmoClientRpc ended data:");
    }

    private IEnumerator setAmmoClientRpcCoroutine(string gunName, int newAmmo){
        yield return new WaitUntil(()=>PlayerController.localPlayer.getGunReference()!=null);
        yield return new WaitUntil(()=>PlayerController.localPlayer.getGunReference().gunName==gunName);
        setAmmoClientRpc(gunName,newAmmo);
    }


    [ServerRpc(RequireOwnership = false)]
    public void addAmmoServerRpc(ulong clientID, string gunName, int change){
        print("adding ammo "+change+" to "+gunName+" for player P"+NetworkManager.LocalClientId);
        
        if(playerAmmoDict[clientID][gunName]+change<0){
            Debug.LogError($"attempting to change {gunName} weapon ammo for client {clientID} resulting in negative number!!!\n aborting");
            return;
        }
        
        setAmmoServerRpc(clientID,gunName,getAmmoLeft(clientID,gunName)+change);
        
        //getGunReference().AmmoLeft=playerAmmoDict[clientID][gunName];
    }


    [ServerRpc(RequireOwnership = false)]
    public void addAmmoToCurrentlyHeldGunServerRpc(ulong clientID, int ammo){
        /*if(currentGun==null||getGunNetworkObject()==null||getGunReference()==null){
            print("No gun held to add ammo to, how?");
            return;
        }*/
        addAmmoServerRpc(clientID,PlayerController.localPlayer.getGunReference().gunName,ammo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void returnGunToPoolServerRpc(NetworkObjectReference toReturn){
        GunController gun = ((NetworkObject)toReturn).GetComponent<GunController>();
        if(gun.OwnerClientId!=NetworkManager.ServerClientId)
            gun.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        gun.NetworkObject.TrySetParent(NetworkObject);
        gun.transform.position = transform.position+new Vector3(0,0,GUN_HOLDING_AREA_DISTANCE*unusedGuns.Count);
        unusedGuns.Add(gun);
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

    public void printAllAmmoInfo(){

        string data=$"GUNSMANAGER AMMO DATA:";
        foreach(var player in playerAmmoDict){
            data+=($"\nPlayer P{player.Key} has:");
            foreach(var gun in player.Value){
                data+=($"\n\t{gun.Value} ammo for {gun.Key}");
            }
        }
        print(data);
    }

}


