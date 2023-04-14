using LSLNetwork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Send grab events to the Logging-Bridges and updates the Ping Sender about Grab/Release events.
/// </summary>
public class GrabEventSender : MonoBehaviour
{
    public PingSender pingSender;
    public BridgeConnectionManager connectionManager;

    public void Start()
    {
        if (!pingSender)
        {
            pingSender = FindObjectOfType<PingSender>();
        }
        if (!connectionManager)
        {
            connectionManager = FindObjectOfType<BridgeConnectionManager>();
        }
    }


    public void OnGrab()
    {
        pingSender.grabbedObjects.Add(this);
        string objectIdentifier = this.gameObject.name;
        GoalTagged tagHolder = GetComponentInParent<GoalTagged>();
        if (tagHolder != null){ 
        objectIdentifier = tagHolder.goalTag.ToString();
        }
        connectionManager.Send("Grab; " + objectIdentifier + ";  " + this.transform.position.ToString(), BridgeConnectionManager.EventChannelKey);
    }

    public void OnRelease()
    {
        pingSender.grabbedObjects.Remove(this);
        string objectIdentifier = this.gameObject.name;
        GoalTagged tagHolder = GetComponentInParent<GoalTagged>();
        if (tagHolder != null)
        {
            objectIdentifier = tagHolder.goalTag.ToString();
        }
        connectionManager.Send("Release; " + objectIdentifier + "; " + this.transform.position.ToString(), BridgeConnectionManager.EventChannelKey);
    }
}
