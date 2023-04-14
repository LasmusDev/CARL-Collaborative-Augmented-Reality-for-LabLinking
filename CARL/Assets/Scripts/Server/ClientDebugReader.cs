using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Receives Debug messages from clients and forwards them to the Unity Log.
/// </summary>
public class ClientDebugReader : MonoBehaviour, IMessageHandler
{
    public const string ClientDebugKey = "ClientDebug";
    public bool msgHandlerSet;
    public NetworkManager networkManager;
    public bool MsgHandlerSet { get => msgHandlerSet; }

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
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ClientDebugKey, ClientDebugHandler);
                msgHandlerSet = true;
            }
            yield return null;
        }
    }

    private void ClientDebugHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out int severity);
        messagePayload.ReadValueSafe(out string message);
        if (severity == 0) {
            Debug.Log("Debug from Client " + senderClientId + ": " + message);
        } else if(severity == 1)
        {
            Debug.LogWarning("DebugWarning from Client " + senderClientId + ": " + message);
        } else
        {
            Debug.LogError("DebugError from client " + senderClientId + ": " + message);
        }
    }
}
