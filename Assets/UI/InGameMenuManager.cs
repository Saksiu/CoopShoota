using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameMenuManager : SingletonNetwork<InGameMenuManager>
{
    [SerializeField] private MyNetworkDiscovery m_Discovery;
    [SerializeField] private CanvasGroup MAIN_inGameMenuCanvasGroup;

    [SerializeField] private GameObject connectedPlayerEntryPrefab;
    [SerializeField] private Transform connectedPlayerEntryParent;

    [SerializeField] private TextMeshProUGUI playerIDText;
    [SerializeField] private Button exitButton;

    [Header("Host")]
    [SerializeField] private CanvasGroup HOST_inGameMenuCanvasGroup;

    //NetworkList<PlayerController>

    private List<string> connectedPlayerNames=new();


    public override void Awake(){
        base.Awake();
        Assert.AreEqual(Instance,this);

        disableInGameMenu();
    }
    public override void OnNetworkSpawn(){
        Assert.IsTrue(IsServer);

        playerIDText.text="ID: "+PlayerPrefs.GetString("PlayerID");
        NetworkManager.OnClientConnectedCallback+=handlePlayerJoined;
        NetworkManager.OnClientDisconnectCallback+=handlePlayerLeft;
        base.OnNetworkSpawn();
        disableInGameMenu();
    }

    public override void OnNetworkDespawn()
    {
        Assert.IsTrue(IsServer);
        NetworkManager.OnClientConnectedCallback-=handlePlayerJoined;
        NetworkManager.OnClientDisconnectCallback-=handlePlayerLeft;
        base.OnNetworkDespawn();
    }



    private void handlePlayerJoined(ulong playerID){
        Assert.IsTrue(IsServer);
        updatePlayerListClientRpc(NetworkManager.ConnectedClients[playerID].PlayerObject.GetComponent<PlayerController>().playerName.Value);
    }

    private void handlePlayerLeft(ulong playerID){
        Assert.IsTrue(IsServer);
        updatePlayerListClientRpc(NetworkManager.ConnectedClients[playerID].PlayerObject.GetComponent<PlayerController>().playerName.Value);
    }

    [ClientRpc]
    private void updatePlayerListClientRpc(FixedString64Bytes changedPlayerId){
        Assert.IsTrue(IsOwner);

        foreach (Transform child in connectedPlayerEntryParent)
            Destroy(child.gameObject);
        
        //NetworkManager.ConnectedClients
        //var player = NetworkManager.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>();
        if(connectedPlayerNames.Contains(changedPlayerId.ToString()))
            connectedPlayerNames.Remove(changedPlayerId.ToString());
        else
            connectedPlayerNames.Add(changedPlayerId.ToString());
        
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
        }else{
            m_Discovery.StartServer();
        }
    }

    public void handleMainMenuButtonPressed(){
        //TODO: Warning dialog
        disableInGameMenu();
        GameMaster.Instance.beginShutDown(exitGame: false);
    }
    public void handleExitGameButtonPressed(){
        GameMaster.Instance.beginShutDown(exitGame: true);
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

