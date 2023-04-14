using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Receives Metadata from clients on connection and send out responses to hide head objects of Optitrack-Clients and organize LSL-Bridge connections.
/// </summary>
public class ClientMetaDataHolder : MonoBehaviour, IMessageHandler
{
    public const string HideHeadKey = "HideHead";
    public const string LSLPortKey = "LSLPort";
    public List<ClientMetaData> clientMetaDataList;
    public bool msgHandlerSet;
    public NetworkManager networkManager;
    public List<int> clientLSLPorts;
    public int connectedLSLClients;
    public string LSLHostName;
    public bool MsgHandlerSet { get => msgHandlerSet; }

    // Start is called before the first frame update
    void Start()
    {
        if (!networkManager)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }
        StartCoroutine(RegisterMsgHandlers());
    }

    public IEnumerator RegisterMsgHandlers()
    {
        while (!MsgHandlerSet)
        {
            if (networkManager.CustomMessagingManager != null)
            {
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ClientMetaDataSender.MetaDataKey, MetaDataHandler);
                networkManager.OnClientDisconnectCallback += ClientDisconnected;
                networkManager.OnClientConnectedCallback += ClientConnected;
                msgHandlerSet = true;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Once a client connects, update it about already registered Optitrack clients, so their heads can be hidden.
    /// </summary>
    /// <param name="obj"></param>
    private void ClientConnected(ulong obj)
    {
        foreach(ClientMetaData clientMetaData in clientMetaDataList)
        {
            if(clientMetaData.clientType == CustomDeviceType.OTCLIENT)
            {
                using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
                {
                    writer.WriteValueSafe(clientMetaData.clientID);
                    networkManager.CustomMessagingManager.SendNamedMessage(HideHeadKey, obj, writer, NetworkDelivery.Reliable);
                }
            }
        }
    }

    /// <summary>
    /// Once a client has send metadata, store it in the clientMetaDataList, and either forward a Hide-Head command to all clients
    /// if the new client is an Optitrack client, or inform the HoloLens client about its bridge ports.
    /// </summary>
    /// <param name="senderClientId"></param>
    /// <param name="messagePayload"></param>
    private void MetaDataHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        Debug.Log("Metadata received");
        messagePayload.ReadValueSafe(out ulong clientID);
        messagePayload.ReadValueSafe(out CustomDeviceType deviceType);
        messagePayload.ReadValueSafe(out string deviceName);
        clientMetaDataList.Add(new ClientMetaData(clientID, deviceType, deviceName));
        if(deviceType == CustomDeviceType.OTCLIENT)
        {        
                using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
                {
                    writer.WriteValueSafe(clientID);
                    networkManager.CustomMessagingManager.SendNamedMessageToAll(HideHeadKey, writer, NetworkDelivery.Reliable);
                }
        }
        if(deviceType == CustomDeviceType.HLCLIENT)
        {
            using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
            {
                writer.WriteValueSafe(clientLSLPorts[connectedLSLClients]);
                writer.WriteValueSafe(LSLHostName);
                Debug.Log("Requesting Client " + senderClientId + " to connect to: " + LSLHostName + ":" + clientLSLPorts[connectedLSLClients]);
                connectedLSLClients++;               
                networkManager.CustomMessagingManager.SendNamedMessage(LSLPortKey, senderClientId, writer, NetworkDelivery.Reliable);
            }
        }
        
    }

    private void ClientDisconnected(ulong clientID)
    {
        clientMetaDataList.Remove(clientMetaDataList.Find(x => x.clientID == clientID));
    }

}

[System.Serializable]
public struct ClientMetaData{
    public ulong clientID;
    public CustomDeviceType clientType;
    public string clientDeviceName;

    public ClientMetaData(ulong clientID, CustomDeviceType clientType, string clientDeviceName)
    {
        this.clientID = clientID;
        this.clientType = clientType;
        this.clientDeviceName = clientDeviceName;
    }
}
