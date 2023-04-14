using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(SpawnGoalSender))]
public class SpawnGoalSenderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.FlexibleSpace();
        SpawnGoalSender spawner = target as SpawnGoalSender;
        //Button that automatically loads prefabs from the given path
        if (GUILayout.Button("Send Spawn Goals"))
        {
            spawner.SendSpawnGoals();
        }
        if (GUILayout.Button("Store Accuracy"))
        {
            foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
            {
                using FastBufferWriter writer = new FastBufferWriter(1, Unity.Collections.Allocator.Temp, 64000);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(GoalSpawner.GetGoalDistanceKey, clientID, writer);
            }
        }
        base.OnInspectorGUI();
    }
}
