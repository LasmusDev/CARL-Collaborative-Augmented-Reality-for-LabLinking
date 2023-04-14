using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using UnityEngine;

//Receives and Displays ping notifications.
public class PingReceiver : MonoBehaviour, IMessageHandler
{
    bool msgHandlerSet;
    public NetworkManager networkManager;
    public GameObject[] pingParticleSystems;
    public int lastUsedPS;
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
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(PingSender.PingSendKey, PingHandler);
                msgHandlerSet = true;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Displays a ping at the position sent in the payload.
    /// </summary>
    /// <param name="senderClientId">The Senders id</param>
    /// <param name="messagePayload">The payload, contains a position in relative playspace where the ping is to be shown.</param>
    public void PingHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        Vector3 pos = SynchronizationUtilities.ReadVector3FromStream(messagePayload);        
        if (MetaDataHolder.IsServer)
        {          
             PingSender.SendPingToAll(pos);
        } else {
            ClientDebugSender.DebugToServer("Received Ping at " + pos); ;
            lastUsedPS = lastUsedPS + 1 < pingParticleSystems.Length ? lastUsedPS + 1 : 0;
            pingParticleSystems[lastUsedPS].transform.localPosition = pos;
            pingParticleSystems[lastUsedPS].SetActive(true);
        }
    }
}
