using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SynchronizedObjectManager))]
public class SynchronizedObjectManagerEditor : UnityEditor.Editor
{
    bool showSpawnButtons = true;
    bool showBaseGUI = false;
    
    public override void OnInspectorGUI()
    {
        GUILayout.FlexibleSpace();
        SynchronizedObjectManager spawner = target as SynchronizedObjectManager;
        //Button that automatically loads prefabs from the given path
        if (GUILayout.Button("Reload synchronized Prefabs"))
        {
            //TODO: Also fill in NetworkManager
            spawner.objectPrefabs.Clear();
            string[] assetGUIDs = AssetDatabase.FindAssets("t: GameObject", new[] { spawner.prefabPath });
            foreach (string assetGUID in assetGUIDs)
            {
                GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGUID), typeof(GameObject));
                spawner.objectPrefabs.Add(prefab);
                Debug.LogWarning("Dont Forget to Add your prefabs to the NetworkManager as well! This unfortunately cannnot be automated at the moment (2023)," +
                    " as the needed variable (NetworkConfig.NetworkPrefabs) is internal to the Unity.Netcode namespace");
            }
            EditorUtility.SetDirty(spawner);
            AssetDatabase.SaveAssets();

        }
        //Foldout for spawn buttons
        if (showSpawnButtons = EditorGUILayout.Foldout(showSpawnButtons, "Spawn buttons"))
        {
            foreach (GameObject go in spawner.objectPrefabs)
            {
                if (GUILayout.Button("Spawn " + go.name) && Application.isPlaying)
                {
                    spawner.SpawnSynchronizedObject(go.name, NetworkManager.Singleton.LocalClientId);
                }
            }
        }
        if (GUILayout.Button("SpawnHardcodedStudyList (SERVER ONLY)"))
        {
            spawner.StartCoroutine(spawner.SpawnHardcodedStudyList());
        }

        

        //Foldout for default GUI
        if (showBaseGUI = EditorGUILayout.Foldout(showBaseGUI, "Base UI"))
        {
            base.OnInspectorGUI();
        }
    }

   
}
