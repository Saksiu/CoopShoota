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

    private NetworkList<FixedString64Bytes> connectedPlayerNames;

    //only for use for the server, workaround around dumb fucking way unity handles disconnects
    private Dictionary<ulong, string> connectedPlayerNamesDict=new();
    private MyNetworkDiscovery m_Discovery;


    public override void Awake(){
        base.Awake();
        m_Discovery=NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>();
        connectedPlayerNames=new NetworkList<FixedString64Bytes>(
        new List<FixedString64Bytes>(),NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
        connectedPlayerNames.OnListChanged+=onPlayerNamesListChanged;


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
            NetworkManager.OnConnectionEvent+=handlePlayerCountChanged;
        }
        disableInGameMenu();
    }

    public override void OnNetworkDespawn()
    {
        connectedPlayerNames.OnListChanged-=onPlayerNamesListChanged;

        if(IsServer){
            NetworkManager.OnConnectionEvent-=handlePlayerCountChanged;
        }

        base.OnNetworkDespawn();
    }

    private void onPlayerNamesListChanged(NetworkListEvent<FixedString64Bytes> changeEvent){
        print($"onPlayerNamesListChanged, event type: {changeEvent.Type} for {changeEvent.Value}");
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
        if(changeEvent.Type==NetworkListEvent<FixedString64Bytes>.EventType.Clear){
            clearDisplayedPlayersList();
            return;
        }
    }

    private void handlePlayerCountChanged(NetworkManager networkManager, ConnectionEventData eventData){
        print($"handlePlayerCountChanged, event type: {eventData.EventType}, local client id: {NetworkManager.Singleton.LocalClientId}");
        Assert.IsTrue(IsServer,"Non-Server entity tried to handle player joined event");

        if(eventData.EventType==ConnectionEvent.ClientConnected){
            connectedPlayerNamesDict.Add(eventData.ClientId, 
            NetworkManager.ConnectedClients[eventData.ClientId].PlayerObject
                .GetComponent<PlayerController>().playerName.Value.ToString());
            connectedPlayerNames.Add(connectedPlayerNamesDict[eventData.ClientId]);
        }
        else if(eventData.EventType==ConnectionEvent.ClientDisconnected){

            if(!connectedPlayerNamesDict.ContainsKey(eventData.ClientId)){
                print("received disconnect event for client before connect event was fully processed... aborting");
                return;
            }

            connectedPlayerNames.Remove(connectedPlayerNamesDict[eventData.ClientId]);
            connectedPlayerNamesDict.Remove(eventData.ClientId);
        }
    }


    //[ClientRpc]
    private void clearDisplayedPlayersList(){
        foreach (Transform child in connectedPlayerEntryParent)
            Destroy(child.gameObject);
    }
    private void removeFromDisplayedPlayersList(string playerName){
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
    }

    private void redrawPlayerList(){
        clearDisplayedPlayersList();

        foreach (var playerName in connectedPlayerNames)
            addToDisplayedPlayersList(playerName.ToString());
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
        if(IsClient&&!IsHost)
            DisconnectClientServerRpc(NetworkManager.LocalClientId);
        else
            PlayerSessionManager.Instance.beginShutDown(exitGame: false);
    }
    public void handleExitGameButtonPressed(){
        //if(IsClient&&!IsHost)
           // DisconnectClientServerRpc(NetworkManager.LocalClientId);
        //else
        PlayerSessionManager.Instance.beginShutDown(exitGame: false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisconnectClientServerRpc(ulong clientID){
        NetworkManager.Singleton.DisconnectClient(clientID);
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

