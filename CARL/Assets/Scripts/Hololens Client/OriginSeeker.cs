using Microsoft.MixedReality.QR;
using QRTracking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Checks for an origin QR code and attaches itself as a marker for object spawning&sync
/// </summary>
public class OriginSeeker : MonoBehaviour
{
    public int trackedOriginID;
    public QRCodesVisualizer visualizer;
    public Transform originTransform;
    public string originQRString;
    public OptitrackStreamingClient streamingClient;
    void Start()
    {
        //If this is a hololens client, register origin detection handler to QR-Service
        if (MetaDataHolder.IsHLClient)
        {
            if (!visualizer)
            {
                visualizer = FindObjectOfType<QRCodesVisualizer>();
            }
            visualizer.OnQRCodeObjectCreated += CheckForOriginHL;
        }
        //If this is an Optitrack client, start coroutine to look for data of origin object from Motive.
        if (MetaDataHolder.IsOTClient)
        {
            if (!streamingClient)
            {
                streamingClient = FindObjectOfType<OptitrackStreamingClient>();
            }
            StartCoroutine(UpdateOriginObject());
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Keep this transform in sync with origin transform, if its set.
        if (originTransform)
        {
            this.transform.SetPositionAndRotation(originTransform.position, originTransform.rotation);
            this.transform.localScale = originTransform.localScale;
        }
    }

    /// <summary>
    /// Checks if the provided code is the origin QR-Code, and sets origin transform appropriately.
    /// </summary>
    /// <param name="transform">The Transform associated wíth the QRCode</param>
    /// <param name="code">The QRCode</param>
    public void CheckForOriginHL(Transform transform, QRCode code)
    {
        if(code.Data.Contains(originQRString))
        {
            originTransform = transform;
        }
    }
    /// <summary>
    /// Continually updates the origin object as provided by Motive.
    /// </summary>
    /// <returns>Coroutine</returns>
    public IEnumerator UpdateOriginObject()
    {
        while (true)
        {
            OptitrackRigidBodyState orbs = streamingClient.GetLatestRigidBodyState(trackedOriginID);
            if (orbs != null)
            {
                originTransform.SetPositionAndRotation(new Vector3(orbs.Pose.Position.x * -1, orbs.Pose.Position.y * -1, orbs.Pose.Position.z), orbs.Pose.Orientation);
                yield return new WaitForSeconds(0.1f);
            }
            yield return null;
        }
    }

    
}
