using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages spawning and ownership of synchronized objects.
/// Also contains the handlers for synch objects to avoid duplication.
/// </summary>
public class SynchronizedObjectManager : NetworkBehaviour, IMessageHandler
{

    public const string OwnershipRequestKey = "OwnershipRequest";
    public const string TransformSyncKey = "TransformSync";
    public const string TransformSyncScaleKey = "ScaleTransformSync";
    public const string CombinedTransformSyncKey = "CombinedTransformSync";
    public const string TrackedStateChangeKey = "TrackingStateChange";
    public static SynchronizedObjectManager Instance;
    public OriginSeeker origin;
    public List<GameObject> objectPrefabs;
    public string prefabPath;
    public const string SyncedSpawnKey = "SpawnSynchronizedObject";
    bool msgHandlerSet;
    public NetworkManager networkManager;
    public bool MsgHandlerSet { get => msgHandlerSet; }

    public void Start()
    {
        if (Instance == null) {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("More than one Instance of SynchronizedObjectSpawner was found. This is supposed to be a singleton.");
        }
        origin = FindObjectOfType<OriginSeeker>();
        if (!networkManager)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }
        StartCoroutine(RegisterMsgHandlers());
    }

    public IEnumerator RegisterMsgHandlers()
    {
        while (!MsgHandlerSet)
        {
            if (networkManager.CustomMessagingManager != null)
            {
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(SyncedSpawnKey, SpawnSynchronizedHandler);
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(OwnershipRequestKey, RequestOwnershipHandler);
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(TransformSyncKey, TransformSyncHandler);
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(TrackedStateChangeKey, TrackedStateChangeHandler);
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(TransformSyncScaleKey, FullTransformSyncHandler);
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(CombinedTransformSyncKey, CombinedTransformSyncHandler);
                msgHandlerSet = true;
            }
            yield return null;
        }
    }

    #region SynchronizedObjectSpawning
    /// <summary>
    /// Spawns a non-Optitrack virtual object across the network.
    /// </summary>
    /// <param name="name">The name of the prefab to be spawned</param>
    /// <param name="OwnerID">The Owner of the object at start</param>
    public GameObject SpawnSynchronizedObject(string name, ulong OwnerID)
    {
        return SpawnSynchronizedObject(name, OwnerID, TrackingState.UNTRACKED, -1);
    }

    /// <summary>
    /// Spawns a virtual object across the network.
    /// </summary>
    /// <param name="name">The name of the prefab to be spawned</param>
    /// <param name="OwnerID">The Owner of the object at start</param>
    /// <param name="trackingState">Whether the object is tracked by an optitrack system</param>
    /// <param name="trackingID">If the object is tracked by optitrack, the ID to identify it across the network, otherwise unused</param>
    /// <returns>The spawned object if this is the server, otherwise NULL</returns>
    public GameObject SpawnSynchronizedObject(string name, ulong OwnerID, TrackingState trackingState, int trackingID)
    {
        if (!networkManager.IsServer)
        {
            using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
            writer.WriteValueSafe(name);
            writer.WriteValueSafe((int)trackingState);
            writer.WriteValueSafe(trackingID);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncedSpawnKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
            return null;
        } else {
            Debug.Log("Spawning " + name);
            GameObject prefab = objectPrefabs.Find(x => x.name == name);         
            GameObject go = Instantiate(prefab);            
            if (go.GetComponent<SynchronizedObject>() == null)
            {
                go.AddComponent<SynchronizedObject>();
            }
            go.GetComponent<NetworkObject>().Spawn();
            go.GetComponent<NetworkObject>().ChangeOwnership(OwnerID);
            if (trackingState != TrackingState.UNTRACKED)
            {
                go.GetComponent<SynchronizedObject>().ChangeTrackedState(trackingState, trackingID, true);
            }
            return go;
        }
    }

    /// <summary>
    /// Handler for object spawning, shouldnt be called except by Messaging Manager
    /// </summary>
    private void SpawnSynchronizedHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out string objectPrefabName);
        messagePayload.ReadValueSafe(out int trackingStateInt);
        messagePayload.ReadValueSafe(out int trackingID);
        if ((TrackingState)trackingStateInt == TrackingState.UNTRACKED)
        {
            SpawnSynchronizedObject(objectPrefabName, senderClientId);
        } else
        {
            SpawnSynchronizedObject(objectPrefabName, senderClientId, (TrackingState)trackingStateInt, trackingID);
        }
    }
    #endregion

    #region Tracking State Change
    /// <summary>
    /// Sends a tracking state change across the network. Primarily used to initialize tracked objects after spawning.
    /// </summary>
    /// <param name="syncObj">The object to be synchronized</param>
    /// <param name="receiver">The receipient of the state change (if Server, it will be forwarded to other clients)</param>
    public void SendTrackedStateChange(SynchronizedObject syncObj, ulong receiver, bool toAll = false)
    {
        using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
        writer.WriteValueSafe(syncObj.NetworkObjectId);
        writer.WriteValueSafe((int)syncObj.TrackingState);
        writer.WriteValueSafe(syncObj.trackingID);
        if (!toAll)
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(TrackedStateChangeKey, receiver, writer, NetworkDelivery.Reliable);
        } else
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(TrackedStateChangeKey, writer, NetworkDelivery.Reliable);
        }
    }

    /// <summary>
    /// Handler for tracking change messages, shouldnt be called except by the messaging manager.
    /// </summary>
    private void TrackedStateChangeHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong objectID);
        messagePayload.ReadValueSafe(out int trackingStateInt);
        messagePayload.ReadValueSafe(out int trackingID);
        StartCoroutine(ChangeTrackedStateCR(senderClientId, objectID, trackingStateInt, trackingID));
    } 
    #endregion

    #region TransformSync
    /// <summary>
    /// Synchronizes an object across the network.
    /// </summary>
    /// <param name="obj">The object to be synchronized</param>
    /// <param name="receiver">The receipient of the synch call (if Server, it will be forwarded to other clients)</param>
    public void SendTransformSync(SynchronizedObject obj, ulong receiver, bool toAll = false)
    {
        using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
        writer.WriteValueSafe(obj.NetworkObjectId);
        SynchronizationUtilities.WriteTransformPoseToStream(obj.transform, writer);
        if (!toAll)
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(TransformSyncKey, receiver, writer);
        } else
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(TransformSyncKey, writer);
        }
        
        obj.lastSentPosition = transform.localPosition;
        obj.lastSentRotation = transform.localRotation;
    }

    /// <summary>
    /// Handler for transform synch calls, shouldnt be called except by the messaging manager
    /// </summary>
    private void TransformSyncHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong objectID);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectID, out NetworkObject obj))
        {
            SynchronizedObject syncObj = obj.GetComponent<SynchronizedObject>();
            if(syncObj == null)
            {
                syncObj = obj.GetComponentInChildren<SynchronizedObject>();
            }
            SynchronizationUtilities.ReadTransformPoseFromStream(syncObj.transform, messagePayload);
            syncObj.lastSentPosition = syncObj.transform.localPosition;
            syncObj.lastSentRotation = syncObj.transform.localRotation;
            if (MetaDataHolder.IsServer)
            {
                foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientID != senderClientId)
                    {
                        SendTransformSync(syncObj, clientID);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Received Sync Data for " + objectID + "but this ID isnt registered.");
        }
    }


    public void SendCombinedTransformSync(SynchronizedObjectParent parent, ulong receiver, bool toAll = false)
    {
        using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
        writer.WriteValueSafe(parent.NetworkObjectId);
        writer.WriteValueSafe(parent.lastSentPosition.Count);
        for(int i= 0; i < parent.lastSentPosition.Keys.Count; i++)
        {
            string childName = parent.lastSentPosition.Keys.ToList()[i];
            Transform childtransform = parent.name == childName ? parent.transform : parent.transform.FindRecursive(childName);
            writer.WriteValueSafe(childName);
            SynchronizationUtilities.WriteTransformPoseToStream(childtransform, writer);
            parent.lastSentPosition[childName] = childtransform.localPosition;
            parent.lastSentRotation[childName] = childtransform.localRotation;
            parent.lastSentScale[childName] = childtransform.localScale;
        }
        if (!toAll)
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(CombinedTransformSyncKey, receiver, writer);
        } else
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(CombinedTransformSyncKey, writer);
        }
    }

    private void CombinedTransformSyncHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong objectID);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectID, out NetworkObject obj))
        {
            SynchronizedObjectParent syncObj = obj.GetComponent<SynchronizedObjectParent>();
            messagePayload.ReadValueSafe(out int noOfChildren);
            for(int i = 0; i < noOfChildren; i++)
            {
                messagePayload.ReadValueSafe(out string childName);
                Transform childtransform = obj.name == childName ? obj.transform : obj.transform.FindRecursive(childName);
                if (childtransform)
                {
                    SynchronizationUtilities.ReadTransformPoseFromStream(childtransform, messagePayload);
                    syncObj.lastSentPosition[childName] = childtransform.localPosition;
                    syncObj.lastSentRotation[childName] = childtransform.localRotation;
                }               
            }
            if (MetaDataHolder.IsServer)
            {
                foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientID != senderClientId)
                    {
                        SendCombinedTransformSync(syncObj, clientID);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Received Sync Data for SyncObjectParent " + objectID + "but this ID isnt registered.");
        }
    }

    
    /// <summary>
    /// Synchronizes an object across the network, including scale.
    /// </summary>
    /// <param name="obj">The object to be synchronized</param>
    /// <param name="receiver">The receipient of the sync call (if Server, it will be forwarded to other clients)</param>
    public void SendFullTransformSync(SynchronizedObject obj, ulong receiver, bool toAll= false)
    {
        using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
        writer.WriteValueSafe(obj.NetworkObjectId);
        SynchronizationUtilities.WriteFullTransformToStream(obj.transform, writer);
        if (!toAll)
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(TransformSyncScaleKey, receiver, writer);
        } else
        {
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(TransformSyncScaleKey, writer);
        }
        obj.lastSentPosition = transform.localPosition;
        obj.lastSentRotation = transform.localRotation;
        obj.lastSentScale = transform.localScale;
    }
    
    private void FullTransformSyncHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong objectID);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectID, out NetworkObject obj))
        {
            SynchronizedObject syncObj = obj.GetComponent<SynchronizedObject>();
            SynchronizationUtilities.ReadFullTransformFromStream(syncObj.transform, messagePayload);
            syncObj.lastSentPosition = syncObj.transform.localPosition;
            syncObj.lastSentRotation = syncObj.transform.localRotation;
            syncObj.lastSentScale = syncObj.transform.localScale;
            if (MetaDataHolder.IsServer)
            {
                foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientID != senderClientId)
                    {
                        SendFullTransformSync(syncObj, clientID);
                    }
                }
            }
        }


    }
    #endregion 

    #region OwnershipTransfer
    /// <summary>
    /// Handler for Ownership requests
    /// </summary>
    private static void RequestOwnershipHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong ownerID);
        messagePayload.ReadValueSafe(out ulong objectID);
        RequestOwnership(ownerID, objectID);
    }
    /// <summary>
    /// Requests Ownership or sets it, depending if this is the Server
    /// </summary>
    /// <param name="clientID">The new owner of the object</param>
    /// <param name="objectID">The network ID of the object</param>
    public static void RequestOwnership(ulong clientID, ulong objectID)
    {
        if (MetaDataHolder.IsHLClient || MetaDataHolder.IsOTClient) //OT Clients shouldnt need to use this, but may
        {
            using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
            writer.WriteValueSafe(clientID);
            writer.WriteValueSafe(objectID);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(OwnershipRequestKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
        }
        else
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectID, out NetworkObject obj))
            {
                obj.ChangeOwnership(clientID);
            }
        }
    }
    #endregion

    public void MoveRandom(Transform t)
    {
        float maxDistAfterSpawning = 1f;
        t.position += new Vector3(-UnityEngine.Random.Range(0, maxDistAfterSpawning), -UnityEngine.Random.Range(0, maxDistAfterSpawning), UnityEngine.Random.Range(0, maxDistAfterSpawning));
    }

    public IEnumerator SpawnHardcodedStudyList()
    {
        GameObject current;
        //Seat 1
        //current = spawner.SpawnSynchronizedObject("Synchronized Plate", 0);
        //yield return new WaitForSeconds(0.1f);
        //MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Fork", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Knife", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        //current = spawner.SpawnSynchronizedObject("Synchronized Wine_Bottle", 0);
        //yield return new WaitForSeconds(0.1f);
        //MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Wineglass", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);

        //Seat 2
        current = SpawnSynchronizedObject("Synchronized Wineglass", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        //current = spawner.SpawnSynchronizedObject("Synchronized Chopping_Board", 0);
        //yield return new WaitForSeconds(0.1f);
        //MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Fork", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Knife", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        //current = spawner.SpawnSynchronizedObject("Synchronized Bread", 0);
        //yield return new WaitForSeconds(0.1f);
        //MoveRandom(current.transform);

        //Seat 3
        current = SpawnSynchronizedObject("Synchronized Big Bowl", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        //current = spawner.SpawnSynchronizedObject("Synchronized Muesli", 0);
        //yield return new WaitForSeconds(0.1f);
        //MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Cup", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Spoon", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        //current = spawner.SpawnSynchronizedObject("Synchronized Plate", 0);
        //MoveRandom(current.transform);

        //Seat 4
        current = SpawnSynchronizedObject("Synchronized Fork", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Knife", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Cup", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Plate", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
        current = SpawnSynchronizedObject("Synchronized Spoon", 0);
        yield return new WaitForSeconds(0.1f);
        MoveRandom(current.transform);
    }

    public IEnumerator ChangeTrackedStateCR(ulong senderClientId, ulong objectID, int trackingStateInt, int trackingID)
    {
        while (true)
        {
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectID, out NetworkObject obj);
            if (obj != null)
            {
                SynchronizedObject syncObj = obj.GetComponent<SynchronizedObject>();
                syncObj.ChangeTrackedState((TrackingState)trackingStateInt, trackingID, false);
                if (MetaDataHolder.IsServer)
                {
                    Debug.LogWarning("Propagating tracked state change");
                    foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
                    {
                        if (clientID != senderClientId)
                        {
                            SendTrackedStateChange(syncObj, clientID);
                        }
                    }
                }
                yield break;
            }
            yield return null;
        }
    }


}
