using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sends out the spawn goal command to all clients.
/// </summary>
public class SpawnGoalSender : MonoBehaviour
{
    /// <summary>
    /// Sends out the spawn goal command to all clients.
    /// </summary>
    public void SendSpawnGoals()
    {
        int playerID = 1;
        foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (FindObjectOfType<ClientMetaDataHolder>().clientMetaDataList.Find(x => x.clientID == clientID).clientType == CustomDeviceType.HLCLIENT)
            {
                using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
                {
                    writer.WriteValueSafe(playerID);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(GoalSpawner.SpawnGoalsKey, clientID, writer, NetworkDelivery.Reliable);
                    playerID++;
                }
            }
        }
    }
}
