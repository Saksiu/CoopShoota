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

    private IPAddress sender;
    private DiscoveryResponseData data;
    public void SetServerInfo(IPAddress sender, DiscoveryResponseData data)
    {
        this.sender = sender;
        this.data = data;
        serverNameText.text = data.ServerName;
        serverIPText.text = sender.ToString();

        joinButton.onClick.AddListener(handleJoinButtonPressed);
    }

    void OnDestroy()
    {
        if(joinButton != null)
            joinButton.onClick.RemoveListener(handleJoinButtonPressed);
    }


    private void handleJoinButtonPressed(){
        
        MainMenuManager.Instance.JoinServer(sender,data);
    }
}
