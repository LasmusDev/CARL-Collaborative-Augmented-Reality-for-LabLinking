using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Samples;
using UnityEngine;

/// <summary>
/// A single object that is synchronized across the network. Unless you know what you are doing, use <see cref="SynchronizedObjectParent"/> instead.
/// </summary>
public class SynchronizedObject : NetworkBehaviour
{
    public Vector3 lastSentPosition;
    public Vector3 lastSentScale;
    public Quaternion lastSentRotation;
    public float timeSinceLastSync;
    public float positionPrecisionInMeters = 0.05f;
    public float rotationPrecisionInDegrees = 1;
    public float scalePrecisionInMeters = 0.05f;
    public int trackingID;
    public bool reparentOnSpawn;
    public bool syncScale;
#if UNITY_EDITOR
    [ReadOnly, SerializeField]
#endif
    protected TrackingState trackingState = TrackingState.UNTRACKED;

    public TrackingState TrackingState { get => trackingState;}


    protected void Start()
    {
        //attach to the Origin Seeker
        if((MetaDataHolder.IsHLClient || MetaDataHolder.IsOTClient) && reparentOnSpawn)
        {
            if (GameObject.FindObjectOfType<OriginSeeker>())
            {
                transform.SetParent(GameObject.FindObjectOfType<OriginSeeker>().transform, false);
            }
        }       
    }

    public void Update()
    {
        if (IsOwned())
        {
            CheckForTransformUpdate();
        }
        //If this object can be normally manipulated, but is currently controlled by an Optitrack-Client, disable manipulation for this object.
        ObjectManipulator om = GetComponent<ObjectManipulator>();
        if (om != null && (trackingState == TrackingState.OTTRACKED) == om.enabled)
        {
            om.enabled = !om.enabled;
        }
    }

    //Return if this object, or its first parent with a network component, is Owned by this client.
    public bool IsOwned()
    {
        if (IsOwner)
        {
            return true;
        } else {
            if (transform.parent != null)
            {
                SynchronizedObject soParent = transform.parent.GetComponent<SynchronizedObject>();
                if (soParent != null)
                {
                    return soParent.IsOwned();
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks whether this objects transform has moved beyond the distance treshold, and if so, pushes the change to the network.
    /// </summary>
    public void CheckForTransformUpdate()
    {
        bool needsUpdate = false;
        timeSinceLastSync += Time.deltaTime;
        if (timeSinceLastSync > 1/NetworkManager.NetworkTickSystem.TickRate &&
            Vector3.Distance(lastSentPosition, transform.localPosition) > positionPrecisionInMeters ||
            Quaternion.Angle(lastSentRotation, transform.localRotation) > rotationPrecisionInDegrees ||
            syncScale && Vector3.Distance(lastSentScale, transform.localScale) > scalePrecisionInMeters)
        {
            needsUpdate = true;
        }
        if (needsUpdate)
        {
            if (syncScale)
            {
                if (MetaDataHolder.IsServer)
                {                 
                    SynchronizedObjectManager.Instance.SendFullTransformSync(this, 10000, true);
                }
                else
                {
                    SynchronizedObjectManager.Instance.SendFullTransformSync(this, NetworkManager.ServerClientId);
                }
            }
            else
            {
                if (MetaDataHolder.IsServer)
                {                   
                        SynchronizedObjectManager.Instance.SendTransformSync(this, 10000, true);
                }
                else
                {
                    SynchronizedObjectManager.Instance.SendTransformSync(this, NetworkManager.ServerClientId);
                }
            }
            timeSinceLastSync = 0;
        }
    }
    
    /// <summary>
    /// Changes this object tracked state&ID, and pushes it across the network if propagate is set
    /// </summary>
    /// <param name="trackingState">The new tracking state</param>
    /// <param name="trackingID">The new tracking ID</param>
    /// <param name="propagate">Whether to push the change across the network (used to avoid endless chains)</param>
    public void ChangeTrackedState(TrackingState trackingState, int trackingID, bool propagate)
    {
        this.trackingState = trackingState;
        this.trackingID = trackingID;
        ClientDebugSender.DebugToServer("Received tracking state " + trackingState.ToString() + " for ID" +trackingID);
        if (trackingState == TrackingState.OTTRACKED)
        {
            ClientDebugSender.DebugToServer("Received OT_tracked State for " + trackingID);
            if(OptitrackObjectSpawner.Instance != null)
            {
                OptitrackObjectSpawner.Instance.spawnOngoing = -1;
                OptitrackObjectSpawner.Instance.spawnedTrackedObjects.Add(this);
            }
        }       
        if (propagate)
        {
            if (MetaDataHolder.IsServer)
            {              
                    SynchronizedObjectManager.Instance.SendTrackedStateChange(this, 1, true);
            }
            else
            {
                SynchronizedObjectManager.Instance.SendTrackedStateChange(this, NetworkManager.ServerClientId);
            }
        } 
    }

    /// <summary>
    /// Gets ownership of this object from the server, if its not already owned.
    /// </summary>
    /// <param name="recursive">Whether all child object should also get ownership transferred.</param>
    public void GetOwnership(bool recursive = true)
    {
        if (!this.IsOwned())
        {
            SynchronizedObjectManager.RequestOwnership(NetworkManager.Singleton.LocalClientId, this.NetworkObjectId);
        }
        if (recursive)
        {
            foreach (Transform t in transform)
            {
                SynchronizedObject so = t.GetComponent<SynchronizedObject>();
                if (so != null)
                {
                    so.GetOwnership(true);
                }
            }
        }
    }

}
public enum TrackingState{
    UNTRACKED, OTTRACKED
}