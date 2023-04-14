using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MotiveObject))]
public class MotiveObjectEditor : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        //Key Label
        Rect currFieldRect = position;
        currFieldRect.height = 18;
        currFieldRect.width /= 8; //12.5%
        EditorGUI.LabelField(currFieldRect, "Key");
        //key property
        currFieldRect.position += new Vector2(currFieldRect.width + 1, 0);
        SerializedProperty keyProp = property.FindPropertyRelative("streamingID");
        EditorGUI.PropertyField(currFieldRect, keyProp, GUIContent.none);
        //Object Label
        currFieldRect.position += new Vector2(currFieldRect.width + 10, 0);
        EditorGUI.LabelField(currFieldRect, "Object");
        //Object Property
        currFieldRect.position += new Vector2(currFieldRect.width + 1, 0);
        currFieldRect.width *= 4f; //50%
        SerializedProperty valueProp = property.FindPropertyRelative("unityObject");
        EditorGUI.PropertyField(currFieldRect, valueProp, GUIContent.none);
        //PosOffsetProperty
        currFieldRect = position;
        currFieldRect.y += 20;
        SerializedProperty posOffset = property.FindPropertyRelative("posOffset");
        EditorGUI.PropertyField(currFieldRect, posOffset);
        //RotOffsetProperty
        currFieldRect.y += 20;
        SerializedProperty rotOffset = property.FindPropertyRelative("rotOffset");
        EditorGUI.PropertyField(currFieldRect, rotOffset);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 60;
    }


}
