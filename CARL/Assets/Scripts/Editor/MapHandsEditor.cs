using Microsoft.MixedReality.OpenXR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(MapHands))]
public class MapHandsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.FlexibleSpace();
        MapHands handScript = target as MapHands;
        //Button that automatically loads prefabs from the given path
        if (GUILayout.Button("Try Autofill"))
        {
            List<IndexJointPair> pairs = new List<IndexJointPair>();
            pairs.Add(new IndexJointPair(HandJoint.Wrist, handScript.transform.GetChild(0)));
            foreach (HandJoint currentJoint in Enum.GetValues(typeof(HandJoint))) {
                string[] jointNameSplit = SplitCamelCase(currentJoint.ToString()).ToArray();
                if (jointNameSplit.Length > 1)
                {
                    string a = jointNameSplit[0];
                    string b = jointNameSplit[1];
                    Transform childFit = handScript.transform.FindRecursive(x => x.name.Contains(a) && x.name.Contains(b));
                    if(childFit == null)
                    {
                        Debug.LogWarning("Could not find transform for " + currentJoint.ToString());
                    }
                    pairs.Add(new IndexJointPair(currentJoint, childFit));
                }              
            }
            handScript.joints = pairs.ToArray();
        }
        base.OnInspectorGUI();
    }

    public static IEnumerable<string> SplitCamelCase(string source)
    {
        const string pattern = @"[A-Z][a-z]*|[a-z]+|\d+";
        var matches = Regex.Matches(source, pattern);
        foreach (Match match in matches)
        {
            yield return match.Value;
        }
    }
}
