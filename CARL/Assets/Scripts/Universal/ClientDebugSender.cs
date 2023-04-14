using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Allows clients to send Debug-Messages to the server. 
/// </summary>
public class ClientDebugSender : MonoBehaviour
{
    /// <summary>
    /// Sends a debug message to the server.
    /// </summary>
    /// <param name="message">The message to be sent.</param>
    public static void DebugToServer(string message)
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.CustomMessagingManager != null)
        {
            using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
            {
                writer.WriteValueSafe<int>(0); //Severity Debug
                writer.WriteValueSafe(message);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ClientDebugReader.ClientDebugKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
            }
        }
    }

    /// <summary>
    /// Sends a warning to the server
    /// </summary>
    /// <param name="message">The Warning to be sent.</param>
    public static void WarningToServer(string message)
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.CustomMessagingManager != null)
        {
            using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
            {
                writer.WriteValueSafe<int>(1); //Severity Warning
                writer.WriteValueSafe(message);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ClientDebugReader.ClientDebugKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
            }
        }
    }

    /// <summary>
    /// Sends an error to the server
    /// </summary>
    /// <param name="message">The error to be sent.</param>
    public static void ErrorToServer(string message)
    {
            if (NetworkManager.Singleton && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
                {
                    writer.WriteValueSafe<int>(2); //Severity Error
                    writer.WriteValueSafe(message);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ClientDebugReader.ClientDebugKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
                }
            }
    }


}
