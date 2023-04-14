using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapHands : MonoBehaviour
{
    public IndexJointPair[] joints;
    public Microsoft.MixedReality.Toolkit.Handedness handedness;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void UpdateMesh(int[] indices, Transform[] newJointPositions)
    {
        for(int i = 0; i < indices.Length; i++)
        {
            int jointIndex = indices[i];
            IndexJointPair pair = joints.FirstOrDefault(x => ((int)x.index).Equals(jointIndex));
            if (pair != null && pair.handJointTransform != null)
            {
                pair.handJointTransform.position = newJointPositions[i].position;
                //pair.handJointTransform.SetPositionAndRotation(newJointPositions[i].position, newJointPositions[i].rotation);
            }
        }
    }
}

[System.Serializable]
public class IndexJointPair{


    public IndexJointPair(HandJoint pHandJoint, Transform pTransform)
    {
        index = pHandJoint;
        handJointTransform = pTransform;
    }
    public HandJoint index;
    public Transform handJointTransform;
}