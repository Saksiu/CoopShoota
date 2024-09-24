using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoundServerEntryComponent : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI serverNameText;
    [SerializeField] private TextMeshProUGUI serverIPText;
    [SerializeField] private Button joinButton;

    private IPAddress hostAddress;
    private DiscoveryResponseData data;
    public void SetServerInfo(IPAddress hostAddress, DiscoveryResponseData data)
    {
        this.hostAddress = hostAddress;
        this.data = data;
        serverNameText.text = data.ServerName;
        serverIPText.text = hostAddress.ToString();

        joinButton.onClick.AddListener(handleJoinButtonPressed);
    }

    void OnDestroy()
    {
        if(joinButton != null)
            joinButton.onClick.RemoveListener(handleJoinButtonPressed);
    }


    private void handleJoinButtonPressed(){
        
        MainMenuManager.Instance.JoinServer(hostAddress,data);
    }
}
