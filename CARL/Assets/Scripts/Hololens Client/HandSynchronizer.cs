using LSLNetwork;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

/// <summary>
/// Synchronizes hands represented by a cube-cloud between users.
/// </summary>
public class HandSynchronizer : NetworkBehaviour, IMessageHandler
{
    IMixedRealityHandJointService handJointService;
    //Not constants because of the variant in client ID
    public string LeftHandSyncKey = "LeftHandSync";
    public string RightHandSyncKey = "RightHandSync";
    public ulong thisHandClientID;
    public Dictionary<int, GameObject> leftHandJoints;
    public Dictionary<int, GameObject> rightHandJoints;
    public GameObject jointGameObject;
    public GameObject localJointGO;
    public float targetSyncsPerSecond;
    private float actualSyncsPerSecond;
    private bool msgHandlerSet;
    public MapHands lHandMesh;
    public MapHands rHandMesh;
    public bool MsgHandlerSet { get => msgHandlerSet; }
    // Start is called before the first frame update
    void Start()
    {
        LeftHandSyncKey += this.OwnerClientId;
        RightHandSyncKey += this.OwnerClientId;
        //Initialize variables
        if (MetaDataHolder.IsHLClient)
        {
            handJointService = Microsoft.MixedReality.Toolkit.CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
        }
        this.thisHandClientID = OwnerClientId; //This should be attached to the player, so that the owner is unique for every HandSynchronizer on one machine
        leftHandJoints = new Dictionary<int, GameObject>();
        rightHandJoints = new Dictionary<int, GameObject>();
        StartCoroutine(RegisterMsgHandlers());
        StartCoroutine(ContinousHandSync());
        
    }

    public IEnumerator RegisterMsgHandlers()
    {
        while (!MsgHandlerSet)
        {
            if (NetworkManager.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(LeftHandSyncKey, LeftHandHandler);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RightHandSyncKey, RightHandHandler);
                msgHandlerSet = true;
            }
            yield return null;
        }
    }


    /// <summary>
    /// Checks time ticks and synchronizes hands if necessary. More efficient than using update.
    /// </summary>
    public IEnumerator ContinousHandSync()
    {
        actualSyncsPerSecond = targetSyncsPerSecond;
        if (!MetaDataHolder.IsHLClient)
        {
            yield break;
        }
        while (true)
        {
            if (thisHandClientID == NetworkManager.Singleton.LocalClientId)
            {
                StartCoroutine(SynchHands());
                actualSyncsPerSecond = targetSyncsPerSecond;
            }
            else
            {
                actualSyncsPerSecond /= 2;
                Mathf.Clamp(actualSyncsPerSecond, 0.1f, 60);
            }
            yield return new WaitForSeconds(1.0f / actualSyncsPerSecond);
        }
    }

    /// <summary>
    /// Synchronizes any tracked Hands to the Server. Takes 2 frames to avoid lags.
    /// </summary>
    /// <returns>Coroutine</returns>
    public IEnumerator SynchHands()
    {
        if (handJointService.IsHandTracked(Microsoft.MixedReality.Toolkit.Utilities.Handedness.Right))
        {
            using (FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000))
            {
                WriteHandToStream(Handedness.Right, writer, thisHandClientID);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RightHandSyncKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
            }
        }
        yield return null;
        if (handJointService.IsHandTracked(Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left))
        {
            using (FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000))
            {
                WriteHandToStream(Handedness.Left, writer, thisHandClientID);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(LeftHandSyncKey, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
            }
        }
    }

    /// <summary>
    /// Handler for the left hand synchronization, shouldnt be called except by Message Handler
    /// </summary>
    public void LeftHandHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong handClientID);
        if (handClientID != this.thisHandClientID)
        {
            return; //This message is meant for another HandSynchronizer
        }
        while (messagePayload.Length > messagePayload.Position + 32)
        {
            messagePayload.ReadValueSafe(out int jointID);
            if (!leftHandJoints.ContainsKey(jointID))
            {
                leftHandJoints.Add(jointID, Instantiate(jointGameObject, this.transform));
            }
            SynchronizationUtilities.ReadTransformPoseFromStream(leftHandJoints[jointID].transform, messagePayload, true);
            lHandMesh.UpdateMesh(leftHandJoints.Keys.ToArray(), leftHandJoints.Values.Select(x => x.transform).ToArray());
        }
        if (MetaDataHolder.IsServer)
        {
            using (FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000))
            {
                WriteForwardedHandToStream(Handedness.Left, writer, thisHandClientID);
                foreach (ulong clientID in NetworkManager.ConnectedClientsIds)
                {
                    if (clientID != senderClientId)
                    {
                        Debug.Log("Sending left hand from " + senderClientId + " to " + clientID);
                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(LeftHandSyncKey, clientID, writer, NetworkDelivery.Reliable);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Handler for the right hand synchronization, shouldnt be called except by Message Handler
    /// </summary>
    public void RightHandHandler(ulong senderClientId, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out ulong handClientID);
        if (handClientID != this.thisHandClientID)
        {
            return; //This message is meant for another HandSynchronizer
        }
        while (messagePayload.Length > messagePayload.Position + 32)
        {
            messagePayload.ReadValueSafe(out int jointID);
            if (!rightHandJoints.ContainsKey(jointID))
            {
                rightHandJoints.Add(jointID, Instantiate(jointGameObject, this.transform));
            }
            SynchronizationUtilities.ReadTransformPoseFromStream(rightHandJoints[jointID].transform, messagePayload, true);
            rHandMesh.UpdateMesh(rightHandJoints.Keys.ToArray(), rightHandJoints.Values.Select(x=> x.transform).ToArray());
        }
        if (MetaDataHolder.IsServer)
        {
            using (FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000))
            {
                WriteForwardedHandToStream(Handedness.Right, writer, thisHandClientID);
                foreach (ulong clientID in NetworkManager.ConnectedClientsIds)
                {
                    if (clientID != senderClientId)
                    {
                        Debug.Log("Sending right hand from " + senderClientId + " to " + clientID);
                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RightHandSyncKey, clientID, writer, NetworkDelivery.Reliable);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Writes a single hand to the given writer.
    /// </summary>
    /// <param name="handedness">Whether the left or right hand is written</param>
    /// <param name="writer">The writer to write to</param>
    /// <param name="handClientID">The clientID of the Hand being synchronized</param>
    public void WriteHandToStream(Handedness handedness, FastBufferWriter writer, ulong handClientID)
    {
        writer.WriteValueSafe(handClientID);
        for (int i = 1; i < 27; i++) // 27 because of 26 joints
        {
            Transform t = handJointService.RequestJointTransform((TrackedHandJoint)i, handedness);
            Dictionary<int, GameObject> refDict = Handedness.Left == handedness ? ref leftHandJoints : ref rightHandJoints;
            if (t != null)
            {
                if (!refDict.ContainsKey(i))
                {
                    refDict.Add(i, Instantiate(localJointGO, this.transform));
                }
                refDict[i].transform.SetPositionAndRotation(t.position, t.rotation);
                writer.WriteValueSafe(i);
                SynchronizationUtilities.WriteTransformPoseToStream(refDict[i].transform, writer, true);
            }
        }
    }

    public string GetPackedHands()
    {
        Handedness handedness = Handedness.Left;
        string result = "";
        for (int i = 1; i < 27; i++) // 27 because of 26 joints
        {
            TrackedHandJoint joint = (TrackedHandJoint)i;
            Transform t = handJointService.RequestJointTransform(joint, handedness);
            result += "L_" + joint.ToString() + BridgeConnectionManager.PackTransformAsString(t);
        }
        handedness = Handedness.Right;
        for (int i = 1; i < 27; i++) // 27 because of 26 joints
        {
            TrackedHandJoint joint = (TrackedHandJoint)i;
            Transform t = handJointService.RequestJointTransform((TrackedHandJoint)i, handedness);
            result += "R_" + joint.ToString() + BridgeConnectionManager.PackTransformAsString(t);
        }
        return result;
    }

    public void WriteForwardedHandToStream(Handedness handedness, FastBufferWriter writer, ulong handClientID)
    {
        Debug.Log("Writing forwarded hand " + handedness.ToString() + "for Owner" + handClientID);
        writer.WriteValueSafe(handClientID);
        for (int i = 1; i < 27; i++)
        {
            Transform t = null;
            if (handedness == Handedness.Left && leftHandJoints.ContainsKey(i))
            {
                t = leftHandJoints[i].transform;
            }
            else if (rightHandJoints.ContainsKey(i))
            {
                t = rightHandJoints[i].transform;
            }
            if (t != null)
            {
                writer.WriteValueSafe(i);
                SynchronizationUtilities.WriteTransformPoseToStream(t, writer, true);
            }
        }
    }
}
