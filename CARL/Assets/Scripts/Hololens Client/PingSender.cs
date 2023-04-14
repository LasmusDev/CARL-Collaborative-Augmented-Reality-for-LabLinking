using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using LSLNetwork;
using UnityEngine.UI;

/// <summary>
/// Script that tracks the Hand-pointers and triggers a ping if they are close together for long enough.
/// </summary>
public class PingSender : MonoBehaviour
{
    public const string PingSendKey = "PingSend";
    public float maxPingDistanceFactor;
    public float holdTimeUntilPing;
    public float pingHeld;
    public float pingCooldown;
    public float timeSinceLastPing;
    public Vector3 lhPointerPosition;
    public Vector3 rhPointerPosition;
    public List<GrabEventSender> grabbedObjects;
    public Image LoadingBar;

    public void Start()
    {
        //Initialize variables
        grabbedObjects = new List<GrabEventSender>();
    }

    public void Update()
    {
        timeSinceLastPing += Time.deltaTime;
        if (timeSinceLastPing < pingCooldown)
        {
            return;
        }
        if(grabbedObjects.Count > 0)
        {
            return;
        }
        IPointerResult leftHandPoint = null;
        IPointerResult rightHandPoint = null;
        foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        {
            // Ignore anything that is not a hand because we want articulated hands
            if (source.SourceType == InputSourceType.Hand)
            {
                foreach (var p in source.Pointers)
                {
                    if (p.PointerName.Contains("HandRay"))
                    {

                        if (p.PointerName.Contains("Left"))
                        {
                            leftHandPoint = p.Result;
                            #if UNITY_EDITOR
                            lhPointerPosition = leftHandPoint != null ? leftHandPoint.Details.Point : Vector3.zero;
                            #endif
                        }
                        if (p.PointerName.Contains("Right"))
                        {
                            rightHandPoint = p.Result;
                            #if UNITY_EDITOR
                            rhPointerPosition = rightHandPoint != null ? rightHandPoint.Details.Point : Vector3.zero;
                            #endif
                        }
                    }
                }
            }
        }
        if (leftHandPoint == null || rightHandPoint == null)
        {
            return;
        }
        if (Vector3.Distance(leftHandPoint.Details.Point, rightHandPoint.Details.Point) < Mathf.Min(leftHandPoint.Details.RayDistance, rightHandPoint.Details.RayDistance) * maxPingDistanceFactor)
        {
            pingHeld += Time.deltaTime;
            Vector3 centerPoint = Vector3.Lerp(leftHandPoint.Details.Point, rightHandPoint.Details.Point, 0.5f);
            LoadingBar.gameObject.transform.position = centerPoint;
            LoadingBar.gameObject.transform.LookAt(GameObject.Find("HeadOrigin").transform);
            LoadingBar.fillAmount = pingHeld / holdTimeUntilPing;
            if (pingHeld > holdTimeUntilPing)
            {
                timeSinceLastPing = 0;
                pingHeld = 0;
                LoadingBar.fillAmount = pingHeld / holdTimeUntilPing;
                SendPing(FindObjectOfType<OriginSeeker>().transform.InverseTransformPoint(centerPoint), NetworkManager.ServerClientId);
            }
        }
        else
        {
            pingHeld = 0;
            LoadingBar.fillAmount = 0;
        }
    }


    public static void SendPing(Vector3 position, ulong clientID)
    {
        using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
        SynchronizationUtilities.WriteVector3ToStream(position, writer);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(PingSendKey, clientID, writer);
        if (!MetaDataHolder.IsServer)
        {
            BridgeConnectionManager.Instance.Send("Ping;" + position.ToString(), BridgeConnectionManager.EventChannelKey);
        }
    }
    public static void SendPingToAll(Vector3 position)
    {
        using FastBufferWriter writer = new(1, Unity.Collections.Allocator.Temp, 64000);
        SynchronizationUtilities.WriteVector3ToStream(position, writer);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(PingSendKey, writer);
        if (!MetaDataHolder.IsServer)
        {
            BridgeConnectionManager.Instance.Send("Ping;" + position.ToString(), BridgeConnectionManager.EventChannelKey);
        }
    }

}
