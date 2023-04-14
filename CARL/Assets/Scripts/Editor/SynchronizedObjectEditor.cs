using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SynchronizedObject))]
public class SynchronizedObjectEditor : Editor
{
    bool showSpawnButtons = true;
    bool showBaseGUI = false;
    public override void OnInspectorGUI()
    {
        GUILayout.FlexibleSpace();
        SynchronizedObject obj  = target as SynchronizedObject;
        base.OnInspectorGUI();
        //Button that automatically loads prefabs from the given path
        if (Application.isPlaying)
        {
            if (obj.TrackingState == TrackingState.OTTRACKED)
            {
                if (GUILayout.Button("Stop OT-Tracking Functions"))
                {
                    obj.ChangeTrackedState(TrackingState.UNTRACKED, obj.trackingID, true);
                }
            }
            else
            {
                if (GUILayout.Button("Enable OT-Tracking Functions"))
                {
                    obj.ChangeTrackedState(TrackingState.OTTRACKED, obj.trackingID, true);
                }
            }
            if (!obj.IsOwned())
            {
                if (GUILayout.Button("Get Ownership"))
                {
                    obj.GetOwnership(true);
                }
            }

        }
    }
}

