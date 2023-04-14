using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor script to allow goal spawning via button press from the server-editor.
/// </summary>
[CustomEditor(typeof(GoalSpawner))]
public class GoalSpawnerEditor : Editor
{

    public List<bool> foldouts;
    bool showDefaultGUI;
    bool showGoals;

    public override void OnInspectorGUI()
    {
        GUILayout.FlexibleSpace();
        GoalSpawner spawner = target as GoalSpawner;
        //Button that automatically loads prefabs from the given path
        if (GUILayout.Button("Spawn All Goals"))
        {        
                spawner.SpawnAllGoals(-1);
        }
        if (GUILayout.Button("Spawn All Goals Master"))
        {
            spawner.MasterSpawn();
        }


        if (GUILayout.Button("Store Position Changes"))
        {
            foreach(Goal g in spawner.goals)
            {
                if (g.IsSpawned)
                {
                    g.pos = g.instance.transform.localPosition;
                    g.rot = g.instance.transform.localRotation;
                }
            }
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }
        

        if (foldouts == null)
        {
            foldouts = new List<bool>();
        }
        while (foldouts.Count< spawner.goals.Count)
        {
            foldouts.Add(false);
        }
        while (foldouts.Count > spawner.goals.Count)
        {
            foldouts.RemoveAt(foldouts.Count - 1);
        }
        if (showGoals = EditorGUILayout.Foldout(showGoals, "ShowGoals"))
        {
            EditorGUI.indentLevel += 1;
            for (int i = 0; i < foldouts.Count; i++)
            {
                Goal g = spawner.goals[i];
                string guiName = g.obj != null ? g.obj.name + "; Player " + g.playerID : "Empty Goal";
                guiName += " (Element" + i + ")";
                foldouts[i] = EditorGUILayout.Foldout(foldouts[i], guiName);
                if (foldouts[i])
                {
                    EditorGUI.indentLevel += 1;
                    g.pos = EditorGUILayout.Vector3Field("Position", g.pos);
                    g.rot = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", g.rot.eulerAngles));
                    g.obj = (GameObject)EditorGUILayout.ObjectField("Goal-Prefab", g.obj, typeof(GameObject));
                    g.instance = (GameObject)EditorGUILayout.ObjectField("Instance(Readonly)", g.instance, typeof(GameObject));
                    g.playerID = (ulong)EditorGUILayout.IntField("PlayerID", (int)g.playerID);
                    EditorGUI.indentLevel -= 1;
                }
            }
            EditorGUI.indentLevel -= 1;
        }


        if (showDefaultGUI = EditorGUILayout.Foldout(showDefaultGUI, "DefaultGUI"))
        {
            EditorGUI.indentLevel += 1;
            base.OnInspectorGUI();
            EditorGUI.indentLevel -= 1;
        }
    }
}
