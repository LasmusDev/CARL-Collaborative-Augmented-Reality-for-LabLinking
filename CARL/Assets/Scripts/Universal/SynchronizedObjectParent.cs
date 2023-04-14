using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Samples;
using UnityEngine;


/// <summary>
/// Synchronizes all child objects of the object that has this script attached. 
/// Child objects have to have synchronized object names, and the names must be unique in this hierarchy
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class SynchronizedObjectParent : SynchronizedObject
{
    public new Dictionary<string,Vector3> lastSentPosition;
    public new Dictionary<string,Vector3> lastSentScale;
    public new Dictionary<string,Quaternion> lastSentRotation;
    public float timeSinceLastCheck;



    protected new void Start()
    {
        //Initialize Position lists
        base.Start();
        lastSentPosition = new Dictionary<string, Vector3>();
        lastSentScale = new Dictionary<string, Vector3>();
        lastSentRotation = new Dictionary<string, Quaternion>();
        foreach(Transform t in transform.GetRecursiveChildren())
        {
            if (!lastSentPosition.ContainsKey(t.name))
            {
                lastSentPosition.Add(t.name, t.localPosition);
                lastSentRotation.Add(t.name, t.localRotation);
                lastSentScale.Add(t.name, t.localScale);
            }
        }
    }

    public new void Update()
    {
        if (IsOwned())
        {
            CheckForTransformUpdate();
        }
        //If this object or any of its children can be normally manipulated, but are currently controlled by an Optitrack-Client, disable manipulation for that object.
        ObjectManipulator[] oms = GetComponentsInChildren<ObjectManipulator>();
        foreach (ObjectManipulator om in oms)
        {
            if (om != null && (TrackingState == TrackingState.OTTRACKED) == om.enabled)
            {
                om.enabled = !om.enabled;
            }
        }
    }

    /// <summary>
    /// Checks whether the transform or any of its children has moved beyond the treshhold, and if so, pushes the change to the network.
    /// </summary>
    public new void CheckForTransformUpdate()
    {
        bool needsUpdate = false;
        timeSinceLastCheck+= Time.deltaTime;
        timeSinceLastSync += Time.deltaTime;
        if (timeSinceLastCheck > 1 / NetworkManager.NetworkTickSystem.TickRate)
        {
            foreach (string name in lastSentPosition.Keys)
            {
                Transform child = (name == this.name) ? this.transform : transform.FindRecursive(name);
                if (Vector3.Distance(lastSentPosition[name], child.localPosition) > positionPrecisionInMeters ||
                Quaternion.Angle(lastSentRotation[name], child.localRotation) > rotationPrecisionInDegrees ||
                syncScale && Vector3.Distance(lastSentScale[name], child.localScale) > scalePrecisionInMeters)
                {
                    needsUpdate = true;
                    Debug.Log(name +" needs update");
                    break;
                }
            }
            timeSinceLastCheck = 0;
        }
        if (needsUpdate)
        {
            if (syncScale) //TODO: Split scale or no scale
            {
                if (MetaDataHolder.IsServer)
                {                   
                     SynchronizedObjectManager.Instance.SendCombinedTransformSync(this, 10000, true);  
                }
                else
                {
                    SynchronizedObjectManager.Instance.SendCombinedTransformSync(this, NetworkManager.ServerClientId);  
                }
            }
            else
            {
                if (MetaDataHolder.IsServer)
                {
                        SynchronizedObjectManager.Instance.SendCombinedTransformSync(this, 10000, true);
                }
                else
                {
                    SynchronizedObjectManager.Instance.SendCombinedTransformSync(this, NetworkManager.ServerClientId);
                }                
            }
            timeSinceLastSync = 0;
        }
    }
}