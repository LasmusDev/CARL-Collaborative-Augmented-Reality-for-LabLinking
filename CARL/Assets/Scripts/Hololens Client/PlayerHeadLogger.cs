using LSLNetwork;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sends head and hand updates to the Bridges
/// </summary>
public class PlayerHeadLogger : NetworkBehaviour
{
    public Transform playerTransform;
    public Transform originTransform;
    public HandSynchronizer syncher;
    public ulong thisPlayerID;
    public int HeadStreamUpdatesPerSec;

    void Start()
    {
        thisPlayerID = OwnerClientId;
        if (!playerTransform && MetaDataHolder.IsHLClient)
        {
            playerTransform = Camera.main.transform;
        }
        if (!originTransform && MetaDataHolder.IsHLClient)
        {           
            originTransform = FindObjectOfType<OriginSeeker>().transform;
        }
        if(!syncher && MetaDataHolder.IsHLClient)
        {
            syncher = FindObjectOfType<HandSynchronizer>();
        }
        if (MetaDataHolder.IsHLClient)
        {
            StartCoroutine(HeadSyncStream());
        }
    }

    /// <summary>
    /// Continually sends the head&hand positions to the LSL-Bridges 
    /// </summary>
    /// <returns></returns>
    public IEnumerator HeadSyncStream()
    {
        while (true)
        {
            if (!BridgeConnectionManager.Instance._noConnection)
            {
                string trackingUpdate = BridgeConnectionManager.PackTransformAsString(this.transform);
                trackingUpdate += syncher.GetPackedHands();
                BridgeConnectionManager.Instance.Send(trackingUpdate, BridgeConnectionManager.HLTrackingChannelKey);
            }
            yield return new WaitForSeconds(1f / HeadStreamUpdatesPerSec);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton.LocalClientId != thisPlayerID || !MetaDataHolder.IsHLClient)
        {
            return;
        }
        this.transform.SetPositionAndRotation(playerTransform.position, playerTransform.rotation);
    }
}
