using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MainMenuManager : SingletonLocal<MainMenuManager>
{

    public string getPlayerID()=>PlayerPrefs.GetString("PlayerID");
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TextMeshProUGUI playerIDText;
    [SerializeField] private ErrorPanelComponent ErrorPanel;

    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    public CanvasGroup GetMainMenuCanvasGroup() => mainMenuCanvasGroup;

    [Header("Hosting")]
    [SerializeField] private TMP_InputField serverNameInputField;


    [Header("Joining")]

    [SerializeField] private TMP_InputField hostAddressInputField;
    [SerializeField] private GameObject foundServerEntryPrefab;
    [SerializeField] private Transform foundServerEntryParent;

    MyNetworkDiscovery m_Discovery;

    private string initialBindIP;
    private ushort initialBindPort;

    Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new Dictionary<IPAddress, DiscoveryResponseData>();


    void Start()
    {
        var connectionData=NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData;
        initialBindIP=connectionData.Address;
        initialBindPort=connectionData.Port;

        m_Discovery=NetworkManager.Singleton.GetComponent<MyNetworkDiscovery>();
        if(string.IsNullOrEmpty(PlayerPrefs.GetString("PlayerID"))){
            PlayerPrefs.SetString("PlayerID", Guid.NewGuid().ToString());
            print("initial launch detected, generating unique player ID "+PlayerPrefs.GetString("PlayerID"));
        }
        playerIDText.text="ID: "+PlayerPrefs.GetString("PlayerID");
        string savedPlayerName=PlayerPrefs.GetString("PlayerName");
        playerNameInputField.text=string.IsNullOrEmpty(savedPlayerName)?"":savedPlayerName;
        serverNameInputField.text=string.IsNullOrEmpty(savedPlayerName)?"":savedPlayerName+"'s Server";
        m_Discovery.OnServerFound.AddListener(handleServerFound);

        enableMainMenu();

    }
    void OnDestroy()
    {
        if(m_Discovery!=null)
            m_Discovery.OnServerFound.RemoveListener(handleServerFound);
    }
    public void disableMainMenu(){
        mainMenuCanvasGroup.alpha=0;
        mainMenuCanvasGroup.blocksRaycasts=false;
        mainMenuCanvasGroup.interactable=false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void enableMainMenu(){
        discoveredServers.Clear();
        UpdateDisplayedFoundServers();

        mainMenuCanvasGroup.alpha=1;
        mainMenuCanvasGroup.blocksRaycasts=true;
        mainMenuCanvasGroup.interactable=true;
        Cursor.lockState = CursorLockMode.None;
        EventSystem.current.SetSelectedGameObject(playerNameInputField.gameObject);
        
    }

    public void handleHostButtonPressed(){
        try{
            string serverName=GetServerName();
            m_Discovery.ServerName=serverName;
            string playerName=GetPlayerName();

            var transport=NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(initialBindIP, initialBindPort,"0.0.0.0");
            print($"starting host on address {transport.ConnectionData.Address} and port {transport.ConnectionData.Port}");
            NetworkManager.Singleton.StartHost();
            //PlayerController.localPlayer.changePlayerName(playerName);
            disableMainMenu();
        }catch(Exception e){handleError(e.Message);}
    }

    public void handleJoinDirectlyButtonPressed(){
        try{
            UnityTransport transport=(UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            
            JoinServer(
                IPAddress.Parse(GetHostAddress()), 
                new DiscoveryResponseData{
                    Port=transport.ConnectionData.Port,
                    ServerName="Direct Connection"});
        }catch(Exception e){handleError(e.Message);}
    }

    public void JoinServer(IPAddress server, DiscoveryResponseData data){

        //verify player name compliance
        try{GetPlayerName();}
        catch(Exception e){handleError(e.Message);return;}
        
        print($"Joining server at {server.ToString()} with data {data.Port}");
        UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(server.ToString(), data.Port);
        NetworkManager.Singleton.StartClient();
        //PlayerController.localPlayer.changePlayerName(playerName); 
        disableMainMenu();
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
        discoveredServers.Clear();
        UpdateDisplayedFoundServers();
        m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
    }

    private void stopServerSearch(){
        m_Discovery.StopDiscovery();
        //discoveredServers.Clear();
        //UpdateDisplayedFoundServers();
    }

    public void handleRefreshButtonPressed(){
        discoveredServers.Clear();
        UpdateDisplayedFoundServers();
        m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
    }

    public void handleServerFound(IPEndPoint sender, DiscoveryResponseData response){
        print($"Server found: {response.ServerName} at {sender.Address}:{response.Port}");
        discoveredServers[sender.Address] = response;
        UpdateDisplayedFoundServers();
    }

    private void UpdateDisplayedFoundServers(){
        print("Updating displayed found servers");
        foreach (Transform child in foundServerEntryParent){
            Destroy(child.gameObject);
        }
        foreach (var server in discoveredServers){
            var entry = Instantiate(foundServerEntryPrefab, foundServerEntryParent).GetComponent<FoundServerEntryComponent>();
            entry.SetServerInfo(server.Key, server.Value);
        }

    }

    #region input
    public string GetPlayerName(){
        string playerNameRaw=playerNameInputField.text;

        if(playerNameRaw.Length<5)
            throw new ArgumentException("Player Name needs to be at least 5 character long");
        if(playerNameRaw.Length>16)
            throw new ArgumentException("Player Name has to be at most 16 characters long");

        PlayerPrefs.SetString("PlayerName", playerNameRaw);    
        return playerNameRaw;
    }

    private string GetServerName(){
        string serverNameRaw=serverNameInputField.text;
        if(serverNameRaw.Length==0){
            //try and generate server name based on player name if exists
            try{
                string playerName=GetPlayerName();
                if(playerName.Length>0){
                    return playerName+"'s Server";
                }
                    
            }catch(Exception e){throw new ArgumentException("Can't determine automatic server name due to:\n"+e);}

        }
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
