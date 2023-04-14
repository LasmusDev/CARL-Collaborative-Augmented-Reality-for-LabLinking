using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sends metadata about this client to the server after connection is established. 
/// This is primarily used so that the server can differentiate between Optitrack and Hololens Clients, and handle their visuals/data appropriately.
/// </summary>
public class ClientMetaDataSender : MonoBehaviour, IMessageHandler
{
    public const string MetaDataKey = "MetaDataInformation";
    private bool msgHandlerSet;
    public NetworkManager networkManager;

    public bool MsgHandlerSet { get => msgHandlerSet;}

    public void Start()
    {
        //register the connected to server callback
        if (!networkManager)
        {
            networkManager = NetworkManager.Singleton;
        } 
        networkManager.OnClientConnectedCallback += ConnectedToServer;
        StartCoroutine(RegisterMsgHandlers());
    }

    public IEnumerator RegisterMsgHandlers() {
        while (!MsgHandlerSet)
        {
            if (networkManager.CustomMessagingManager != null)
            {
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ClientMetaDataHolder.HideHeadKey, HideHeadHandler);
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ClientMetaDataHolder.LSLPortKey, LSLPortHandler);
                msgHandlerSet = true;
            }
            yield return null;
        }
    }


    /// <summary>
    /// On connection to server, send metadata (device type, localClientID and device name).
    /// </summary>
    /// <param name="ignored"></param>
    public void ConnectedToServer(ulong ignored)
    {
        using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
        {
            writer.WriteValueSafe(NetworkManager.Singleton.LocalClientId);
            writer.WriteValueSafe(MetaDataHolder.Instance.deviceType);
            writer.WriteValueSafe(SystemInfo.deviceName);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(MetaDataKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
        }
    }

    /// <summary>
    /// Hides the head of the client belonging to the given client ID. Used to filter out Heads originating from Optitrack-Clients. 
    /// Only to be called from the messaging manager.
    /// </summary>
    /// <param name="senderClientId">The sender (should be server)</param>
    /// <param name="messagePayload">The payload, contains the clientID of the head to be hidden.</param>
    private void HideHeadHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong headClientID);
        ClientDebugSender.DebugToServer("Received Hide Head command for head for client " + headClientID);
        StartCoroutine(HideHeadCR(headClientID));
    }

    /// <summary>
    /// Sets the LSL-bridge port to the given port.
    /// </summary>
    /// <param name="senderClientId">The sender (should be server)</param>
    /// <param name="messagePayload">The payload, containing the Port&Host address of the bridges for this client</param>
    private void LSLPortHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out int LSLPort);
        messagePayload.ReadValueSafe(out string LSLHost);
        ClientDebugSender.DebugToServer("Connecting to bridge on  " + LSLHost + ":" + LSLPort);
        LSLNetwork.BridgeConnectionManager bcm = FindObjectOfType<LSLNetwork.BridgeConnectionManager>();
        bcm._hostName = LSLHost;
        bcm._port = LSLPort;
        bcm.StartCoroutine(bcm.ConnectionPoll());
    }

    /// <summary>
    /// Looks for a player object belonging to given playerID, and hides its head.
    /// </summary>
    /// <param name="headClientID"></param>
    /// <returns></returns>
    public IEnumerator HideHeadCR(ulong headClientID)
    {
        while (true)
        {
            try
            {
                NetworkObject player = FindObjectsOfType<ParentPlayerToOrigin>().Select(x => x.GetComponent<NetworkObject>()).ToList().Find(y => y.OwnerClientId == headClientID);
                if (player != null)
                {
                    player.gameObject.SetActive(false);
                    yield break;
                }
            } catch (Exception e)
            {
                ClientDebugSender.ErrorToServer(e.Message);
            }
            yield return null;
        }
    }
}
