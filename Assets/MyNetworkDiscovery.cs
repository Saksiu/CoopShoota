using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkManager))]
public class MyNetworkDiscovery : BaseNetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    [Serializable]
    public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
    {
    };

    NetworkManager m_NetworkManager;
    
    [SerializeField]
    [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
    bool m_StartWithServer = true;

    public string ServerName = "EnterName";

    public ServerFoundEvent OnServerFound;
    
    private bool m_HasStartedWithServer = false;

    public void Awake()
    {
        Debug.Log("Awake called");
        m_NetworkManager = GetComponent<NetworkManager>();
    }

    public void Update()
    {
        //Debug.Log($"Update called - IsRunning: {IsRunning}, IsServer: {m_NetworkManager.IsServer}, HasStartedWithServer: {m_HasStartedWithServer}");
        if (m_StartWithServer && m_HasStartedWithServer == false && IsRunning == false)
        {
            if (m_NetworkManager.IsServer)
            {
                Debug.Log("Starting server in Update");
                StartServer();
                m_HasStartedWithServer = true;
            }
        }
    }

    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        //Debug.Log($"ProcessBroadcast called from {sender.Address}");
        response = new DiscoveryResponseData()
        {
            ServerName = ServerName,
            Port = ((UnityTransport) m_NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        //Debug.Log($"ResponseReceived called - Server found: {response.ServerName} at {sender.Address}:{response.Port}");
        OnServerFound.Invoke(sender, response);
    }
}