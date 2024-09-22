using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkUtils
{
    public static List<IPAddress> GetAllBroadcastAddresses()
    {
        List<IPAddress> broadcastAddresses = new List<IPAddress>();

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Skip non-operational interfaces or non-IPv4 addresses
            if (networkInterface.OperationalStatus != OperationalStatus.Up || 
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            // Get the network interface properties
            var properties = networkInterface.GetIPProperties();
            foreach (var unicastAddress in properties.UnicastAddresses)
            {
                // Only consider IPv4 addresses
                if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ipAddress = unicastAddress.Address;
                    var subnetMask = unicastAddress.IPv4Mask;

                    if (subnetMask != null)
                    {
                        // Calculate the broadcast address
                        var ipBytes = ipAddress.GetAddressBytes();
                        var maskBytes = subnetMask.GetAddressBytes();
                        var broadcastBytes = new byte[ipBytes.Length];

                        for (int i = 0; i < ipBytes.Length; i++)
                        {
                            broadcastBytes[i] = (byte)(ipBytes[i] | (maskBytes[i] ^ 255));
                        }

                        var broadcastAddress = new IPAddress(broadcastBytes);
                        broadcastAddresses.Add(broadcastAddress);
                    }
                }
            }
        }

        // If no suitable network interface is found, fallback to global broadcast address
        if (broadcastAddresses.Count == 0)
        {
            broadcastAddresses.Add(IPAddress.Broadcast);
        }

        return broadcastAddresses;
    }

    public static void BroadcastToAllSubnets(UdpClient client, byte[] data, int port)
    {
        List<IPAddress> broadcastAddresses = GetAllBroadcastAddresses();

        foreach (var broadcastAddress in broadcastAddresses)
        {
            IPEndPoint endPoint = new IPEndPoint(broadcastAddress, port);
            try
            {
                Debug.Log($"Sending broadcast to {broadcastAddress}:{port}");
                client.SendAsync(data, data.Length, endPoint);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to send to {broadcastAddress}: {e.Message}");
            }
        }
    }
}
