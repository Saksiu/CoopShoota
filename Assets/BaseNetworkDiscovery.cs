using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public abstract class BaseNetworkDiscovery<TBroadCast, TResponse> : MonoBehaviour
    where TBroadCast : INetworkSerializable, new()
    where TResponse : INetworkSerializable, new()
{
    private enum MessageType : byte
    {
        BroadCast = 0,
        Response = 1,
    }

    UdpClient m_Client;

    [SerializeField] ushort m_Port = 47777;

    // This is long because unity inspector does not like ulong.
    [SerializeField]
    long m_UniqueApplicationId;

    /// <summary>
    /// Gets a value indicating whether the discovery is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets whether the discovery is in server mode.
    /// </summary>
    public bool IsServer { get; private set; }

    /// <summary>
    /// Gets whether the discovery is in client mode.
    /// </summary>
    public bool IsClient { get; private set; }

    public void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit called");
        StopDiscovery();
    }

    void OnValidate()
    {
        if (m_UniqueApplicationId == 0)
        {
            var value1 = (long) Random.Range(int.MinValue, int.MaxValue);
            var value2 = (long) Random.Range(int.MinValue, int.MaxValue);
            m_UniqueApplicationId = value1 + (value2 << 32);
        }
    }

    /// <summary>
    /// Sends a broadcast looking for open servers by iterating through all available interfaces
    /// and sending a UDP broadcast to each subnet.
    /// Done this way to circumvent the issue of not being able to send a broadcast to the global broadcast address.
    /// </summary>
    /// <param name="broadCast"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void ClientBroadcast(TBroadCast broadCast)
    {
    Debug.Log("ClientBroadcast called");

    if (!IsClient)
    {
        throw new InvalidOperationException("Cannot send client broadcast while not running in client mode. Call StartClient first.");
    }

    using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
    WriteHeader(writer, MessageType.BroadCast);
    writer.WriteNetworkSerializable(broadCast);
    var data = writer.ToArray();

    // Broadcast to all subnets
    NetworkUtils.BroadcastToAllSubnets(m_Client, data, m_Port);
    }

    //original method from github, but would only work when partially specifing submask,
    //defeating the point

    /*public void ClientBroadcast(TBroadCast broadCast)
    {
        Debug.Log("ClientBroadcast called");

        if (!IsClient)
        {
            throw new InvalidOperationException("Cannot send client broadcast while not running in client mode. Call StartClient first.");
        }

        //WORKING, BUT SPECIFIES THE INTERFACE AGAIN
        var endPoint = new IPEndPoint(IPAddress.Parse("192.168.88.255"), m_Port);

        //DOES NOT WORK
        //var endPoint = new IPEndPoint(IPAddress.Broadcast, m_Port);
        //var endPoint = new IPEndPoint(IPAddress.Parse("192.168.255.255"), m_Port);

        using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
        WriteHeader(writer, MessageType.BroadCast);

        writer.WriteNetworkSerializable(broadCast);
        var data = writer.ToArray();

        try
        {
            // This works because PooledBitStream.Get resets the position to 0 so the array segment will always start from 0.
            m_Client.SendAsync(data, data.Length, endPoint);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }*/

    /// <summary>
    /// Starts the discovery in server mode which will respond to client broadcasts searching for servers.
    /// </summary>
    public void StartServer()
    {
        //Debug.Log("StartServer called");
        StartDiscovery(true);
    }

    /// <summary>
    /// Starts the discovery in client mode. <see cref="ClientBroadcast"/> can be called to send out broadcasts to servers and the client will actively listen for responses.
    /// </summary>
    public void StartClient()
    {
        //Debug.Log("StartClient called");
        StartDiscovery(false);
    }

    public void StopDiscovery()
    {
        //Debug.Log("StopDiscovery called");

        IsClient = false;
        IsServer = false;
        IsRunning = false;

        if (m_Client != null)
        {
            try
            {
                m_Client.Close();
            }
            catch (Exception)
            {
                // We don't care about socket exception here. Socket will always be closed after this.
            }

            m_Client = null;
        }
    }

    /// <summary>
    /// Gets called whenever a broadcast is received. Creates a response based on the incoming broadcast data.
    /// </summary>
    /// <param name="sender">The sender of the broadcast</param>
    /// <param name="broadCast">The broadcast data which was sent</param>
    /// <param name="response">The response to send back</param>
    /// <returns>True if a response should be sent back else false</returns>
    protected abstract bool ProcessBroadcast(IPEndPoint sender, TBroadCast broadCast, out TResponse response);

    /// <summary>
    /// Gets called when a response to a broadcast gets received
    /// </summary>
    /// <param name="sender">The sender of the response</param>
    /// <param name="response">The value of the response</param>
    protected abstract void ResponseReceived(IPEndPoint sender, TResponse response);

    void StartDiscovery(bool isServer)
    {
       // Debug.Log($"StartDiscovery called with isServer: {isServer}");
        StopDiscovery();

        IsServer = isServer;
        IsClient = !isServer;

        // If we are not a server we use the 0 port (let udp client assign a free port to us)
        var port = isServer ? m_Port : 0;
        m_Client = new UdpClient(new IPEndPoint(IPAddress.Any, m_Port)) { EnableBroadcast = true,MulticastLoopback=false };
        //m_Client = new UdpClient(port) {EnableBroadcast = true, MulticastLoopback = false};

        _ = ListenAsync(isServer ? ReceiveBroadcastAsync : new Func<Task>(ReceiveResponseAsync));

        IsRunning = true;
    }

    async Task ListenAsync(Func<Task> onReceiveTask)
    {
        //Debug.Log("ListenAsync started");

        while (true)
        {
            try
            {
                await onReceiveTask();
            }
            catch (ObjectDisposedException)
            {
                // socket has been closed
                Debug.Log("Socket has been closed");
                break;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    async Task ReceiveResponseAsync()
    {
        //Debug.Log("ReceiveResponseAsync called");

        UdpReceiveResult udpReceiveResult = await m_Client.ReceiveAsync();

        var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Persistent);

        try
        {
            if (ReadAndCheckHeader(reader, MessageType.Response) == false)
            {
                return;
            }
            
            reader.ReadNetworkSerializable(out TResponse receivedResponse);
            ResponseReceived(udpReceiveResult.RemoteEndPoint, receivedResponse);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task ReceiveBroadcastAsync()
    {
        //Debug.Log("ReceiveBroadcastAsync called");

        UdpReceiveResult udpReceiveResult = await m_Client.ReceiveAsync();

        var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
        using var reader = new FastBufferReader(segment, Allocator.Persistent);

        try
        {
            if (ReadAndCheckHeader(reader, MessageType.BroadCast) == false)
            {
                return;
            }
            
            reader.ReadNetworkSerializable(out TBroadCast receivedBroadcast);

            if (ProcessBroadcast(udpReceiveResult.RemoteEndPoint, receivedBroadcast, out TResponse response))
            {
                using var writer = new FastBufferWriter(1024, Allocator.Persistent, 1024 * 64);
                WriteHeader(writer, MessageType.Response);

                writer.WriteNetworkSerializable(response);
                var data = writer.ToArray();

                await m_Client.SendAsync(data, data.Length, udpReceiveResult.RemoteEndPoint);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private void WriteHeader(FastBufferWriter writer, MessageType messageType)
    {
        //Debug.Log("WriteHeader called");

        // Serialize unique application id to make sure packet received is from same application.
        writer.WriteValueSafe(m_UniqueApplicationId);

        // Write a flag indicating whether this is a broadcast
        writer.WriteByteSafe((byte) messageType);
    }

    private bool ReadAndCheckHeader(FastBufferReader reader, MessageType expectedType)
    {
        //Debug.Log("ReadAndCheckHeader called");

        reader.ReadValueSafe(out long receivedApplicationId);
        if (receivedApplicationId != m_UniqueApplicationId)
        {
            return false;
        }

        reader.ReadByteSafe(out byte messageType);
        if (messageType != (byte) expectedType)
        {
            return false;
        }

        return true;
    }
}