using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameMenuManager : SingletonNetwork<InGameMenuManager>
{
    
    [SerializeField] private CanvasGroup MAIN_inGameMenuCanvasGroup;

    [SerializeField] private GameObject connectedPlayerEntryPrefab;
    [SerializeField] private Transform connectedPlayerEntryParent;

    [SerializeField] private TextMeshProUGUI playerIDText;
    [SerializeField] private Button exitButton;

    [Header("Host")]
    [SerializeField] private CanvasGroup HOST_inGameMenuCanvasGroup;

    [SerializeField] private Button toggleServerDiscoveryButton;

    //NetworkList<PlayerController>

    private List<string> connectedPlayerNames=new();
    private MyNetworkDiscovery m_Discovery;

    /*public void Update(){
        print($"InGameMenuManager Update, IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}, Localplayer name: {PlayerController.localPlayer?.playerName.Value}");
    }*/

    public override void Awake(){
        base.Awake();
        m_Discovery=NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>();
        //connectedPlayerNames=new NetworkList<FixedString64Bytes>(
        //new List<FixedString64Bytes>(),NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
        //connectedPlayerNames.OnListChanged+=onPlayerNamesListChanged;


        if(Instance!=this) return;
        clearDisplayedPlayersList();
        disableInGameMenu();
    }
    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        print("InGameMenu Manager NetworkSpawned on "+NetworkManager.LocalClientId);
        playerIDText.text="ID: "+PlayerPrefs.GetString("PlayerID");
        redrawPlayerList();
        if(IsServer){
            NetworkManager.OnClientConnectedCallback+=handlePlayerCountChange;
            NetworkManager.OnClientDisconnectCallback+=handlePlayerCountChange;
        }
        disableInGameMenu();
    }

    public override void OnNetworkDespawn()
    {
        //connectedPlayerNames.OnListChanged-=onPlayerNamesListChanged;

        if(IsServer){
            NetworkManager.OnClientConnectedCallback-=handlePlayerCountChange;
            NetworkManager.OnClientDisconnectCallback-=handlePlayerCountChange;
        }

        base.OnNetworkDespawn();
    }

    /*private void onPlayerNamesListChanged(NetworkListEvent<FixedString64Bytes> changeEvent){
        //if(!IsServer) return;
        switch(changeEvent.Type){
            case NetworkListEvent<FixedString64Bytes>.EventType.Add:
                addToDisplayedPlayersList(changeEvent.Value.ToString());
                break;
            case NetworkListEvent<FixedString64Bytes>.EventType.Remove:
                removeFromDisplayedPlayersList(changeEvent.Value.ToString());
                break;
            default: case NetworkListEvent<FixedString64Bytes>.EventType.Clear:
                clearDisplayedPlayersList();
                break;
        }
    }*/

    private void handlePlayerCountChange(ulong playerID){
        Assert.IsTrue(IsServer,"Non-Server entity tried to handle player count changed event");
        connectedPlayerNames.Clear();
        connectedPlayerNames.AddRange(NetworkManager.ConnectedClients.Values
            .Select(client=>client.PlayerObject.GetComponent<PlayerController>().playerName.Value.ToString()));
        redrawPlayerList();
        //updatePlayerListClientRpc(NetworkManager.ConnectedClients[playerID].PlayerObject.GetComponent<PlayerController>().playerName.Value);
    }

    /*private void handlePlayerLeft(ulong playerID){
        Assert.IsTrue(IsServer,"Non-Server entity tried to handle player left event");
        connectedPlayerNames.Remove(NetworkManager.ConnectedClients[playerID].PlayerObject
        .GetComponent<PlayerController>().playerName.Value);
        //updatePlayerListClientRpc(NetworkManager.ConnectedClients[playerID].PlayerObject.GetComponent<PlayerController>().playerName.Value);
    }*/

    //[ClientRpc]
    private void clearDisplayedPlayersList(){
        foreach (Transform child in connectedPlayerEntryParent)
            Destroy(child.gameObject);
    }
    /*private void removeFromDisplayedPlayersList(string playerName){
        foreach (Transform child in connectedPlayerEntryParent)
            if(child.GetChild(0).GetComponent<TextMeshProUGUI>().text==playerName){
                Destroy(child.gameObject);
                return;
            }
                
    }

    private void addToDisplayedPlayersList(string playerName){
        var entryText = Instantiate(connectedPlayerEntryPrefab, connectedPlayerEntryParent).transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        entryText.text=playerName;
        if(playerName==PlayerPrefs.GetString("PlayerName")) //if is local player basically
            entryText.color=Color.green;
    }*/

    private void redrawPlayerList(){
        //if(!IsOwner) return;

        foreach (Transform child in connectedPlayerEntryParent)
            Destroy(child.gameObject);
        
        //NetworkManager.ConnectedClients
        //var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        /*
        if(connectedPlayerNames.Contains(changedPlayerId.ToString()))
            connectedPlayerNames.Remove(changedPlayerId.ToString());
        else
            connectedPlayerNames.Add(changedPlayerId.ToString());
        */

        foreach (var playerName in connectedPlayerNames){
            var entryText = Instantiate(connectedPlayerEntryPrefab, connectedPlayerEntryParent).transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            entryText.text=playerName;
            if(playerName==PlayerPrefs.GetString("PlayerName"))
                entryText.color=Color.green;
        }
    }

    public void toggleInGameMenu(bool isHost){
        if(MAIN_inGameMenuCanvasGroup.interactable){
            disableInGameMenu();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else{
            enableInGameMenu(isHost);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
            
            
    }
    
    public void enableInGameMenu(bool isHost)
    {
        if(isHost)
            enableCanvasGroup(HOST_inGameMenuCanvasGroup);
        else
            disableCanvasGroup(HOST_inGameMenuCanvasGroup);

        enableCanvasGroup(MAIN_inGameMenuCanvasGroup);
        EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
    }
    public void toggleServerDiscovery(){
        if(m_Discovery.IsRunning){
            m_Discovery.StopDiscovery();
            toggleServerDiscoveryButton.GetComponentInChildren<TextMeshProUGUI>().text="Closed";
            toggleServerDiscoveryButton.GetComponent<Image>().color=Color.red;
            
        }else{
            toggleServerDiscoveryButton.GetComponentInChildren<TextMeshProUGUI>().text="Open";
            toggleServerDiscoveryButton.GetComponent<Image>().color=Color.green;
            m_Discovery.StartServer();
        }
    }

    public void handleMainMenuButtonPressed(){
        print("handleMainMenuButtonPressed");
        //if(!IsOwner) return;
        //TODO: Warning dialog
        disableInGameMenu();
        PlayerController.localPlayer.sessionComponent.beginShutDown(exitGame: false);
    }
    public void handleExitGameButtonPressed(){
        PlayerController.localPlayer.sessionComponent.beginShutDown(exitGame: true);
    }

    public void disableInGameMenu()=>disableCanvasGroup(MAIN_inGameMenuCanvasGroup);

    private void enableCanvasGroup(CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    private void disableCanvasGroup(CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    } 
}

