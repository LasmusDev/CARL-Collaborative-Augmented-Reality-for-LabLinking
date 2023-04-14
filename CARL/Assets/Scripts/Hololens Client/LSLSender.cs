using LSLNetwork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Testscript to see if LSL-Recorder is functional.
/// </summary>
public class LSLSender : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(SendAfter());
    }

    public IEnumerator SendAfter()
    {
        //ClientDebugSender.ErrorToServer("Debug Send Stream Triggered");
        LSLNetwork.BridgeConnectionManager bcm = FindObjectOfType<LSLNetwork.BridgeConnectionManager>();
        bcm.Connect();
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            string myString = new string('*', 10000);
            GetComponent<BridgeConnectionManager>().Send(myString, BridgeConnectionManager.HLTrackingChannelKey);
            GetComponent<BridgeConnectionManager>().Send(myString, BridgeConnectionManager.EventChannelKey);
        }
    }
    
}
