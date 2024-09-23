using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : SingletonLocal<MainMenuManager>
{
    [SerializeField] private TMP_InputField playerNameInputField;

    [SerializeField] private ErrorPanelComponent ErrorPanel;

    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    public CanvasGroup GetMainMenuCanvasGroup() => mainMenuCanvasGroup;

    [Header("Hosting")]
    [SerializeField] private TMP_InputField serverNameInputField;


    [Header("Joining")]

    [SerializeField] private TMP_InputField hostAddressInputField;
    [SerializeField] private GameObject foundServerEntryPrefab;
    [SerializeField] private Transform foundServerEntryParent;

    [SerializeField] MyNetworkDiscovery m_Discovery;
    

    Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new Dictionary<IPAddress, DiscoveryResponseData>();


    public void disableMainMenu(){
        mainMenuCanvasGroup.alpha=0;
        mainMenuCanvasGroup.blocksRaycasts=false;
        mainMenuCanvasGroup.interactable=false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void enableMainMenu(){
        
        mainMenuCanvasGroup.alpha=0;
        mainMenuCanvasGroup.blocksRaycasts=false;
        mainMenuCanvasGroup.interactable=false;
        Cursor.lockState = CursorLockMode.None;
    }

    public void handleHostButtonPressed(){
        try{
            string serverName=GetServerName();
            m_Discovery.ServerName=serverName;
            NetworkManager.Singleton.StartHost();
            disableMainMenu();
        }catch(Exception e){handleError(e.Message);}
    }

    public void handleJoinDirectlyButtonPressed(){
        try{
            JoinServer(IPAddress.Parse(GetHostAddress()), new DiscoveryResponseData());
        }catch(Exception e){handleError(e.Message);}
    }

    public void JoinServer(IPAddress server, DiscoveryResponseData data){
        UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(server.ToString(), data.Port);
        NetworkManager.Singleton.StartClient();
    }

    
    public void handleToggleServerSearch(Button button){
        if(m_Discovery.IsRunning){
            stopServerSearch();
            button.GetComponentInChildren<TextMeshProUGUI>().text="Start Server Search";
        }else{
            startServerSearch();
            button.GetComponentInChildren<TextMeshProUGUI>().text="Stop Server Search";
        }
    }
    private void startServerSearch(){
        m_Discovery.StartClient();
        m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
    }

    private void stopServerSearch(){
        m_Discovery.StopDiscovery();
        discoveredServers.Clear();
        UpdateDisplayedFoundServers();
    }
    public void handleRefreshButtonPressed(){
        discoveredServers.Clear();
        UpdateDisplayedFoundServers();
        m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
    }
    public void handleServerFound(IPEndPoint sender, DiscoveryResponseData response){
        discoveredServers[sender.Address] = response;
        UpdateDisplayedFoundServers();
    }

    private void UpdateDisplayedFoundServers(){
        foreach (Transform child in foundServerEntryParent)
        {
            Destroy(child.gameObject);
        }
        foreach (var server in discoveredServers)
        {
            var entry = Instantiate(foundServerEntryPrefab, foundServerEntryParent).GetComponent<FoundServerEntryComponent>();
            entry.SetServerInfo(server.Key, server.Value);
        }

    }

    #region input
    private string GetPlayerName(){
        string playerNameRaw=playerNameInputField.text;

        if(playerNameRaw.Length>=5)
            if(playerNameRaw.Length<=16)
                return playerNameRaw;
            else
                throw new ArgumentException("Player Name has to be at most 16 characters long");
        else
            throw new ArgumentException("Player Name needs to be at least 5 character long");
    }

    private string GetServerName(){
        string serverNameRaw=serverNameInputField.text;

        if(serverNameRaw.Length>0)
            if(serverNameRaw.Length<=32)
                return serverNameRaw;
            else
                throw new ArgumentException("Server Name has to be at most 32 characters long");
        else
            throw new ArgumentException("Server Name needs to be at least 1 character long");
    }

    private string GetHostAddress(){
        string addressRaw=hostAddressInputField.text;
        if(ValidateHostAddress(addressRaw))
            return addressRaw;
        else
            throw new ArgumentException("Invalid Host Address");
        
    }

    private bool ValidateHostAddress(string value)=>Regex.IsMatch(value, "^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        
    #endregion


    public void handleError(string error)=>ErrorPanel.DisplayError(error);
}
