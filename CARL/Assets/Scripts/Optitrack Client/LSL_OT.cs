using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LSL.liblsl;

/// <summary>
/// Handles LSL logging for the Optitrack-Client
/// </summary>
public class LSL_OT : MonoBehaviour
{
   
    StreamOutlet streamOut;
    string[] buffer;
    public void Start()
    {
        streamOut = new StreamOutlet(new StreamInfo("OT_Stream", "OT_TrackingPositions",1, 0, channel_format_t.cf_string), 32, 128);
    }

    /// <summary>
    /// Pushes a message to the LSL-Recorder
    /// </summary>
    /// <param name="msg">The message to be logged</param>
    public void PushToRecorder(string msg)
    {
        buffer = new string[1];
        buffer[0] = msg;
        streamOut.push_sample(buffer);
    }
    /// <summary>
    /// Flushes connection to the LSL recorder when the application is closed, so all data is sent.
    /// </summary>
    private void OnApplicationQuit()
    {
        buffer = new string[1];
        buffer[0] = "Application Quit";
        streamOut.push_sample(buffer, 0.0, true);
    }
}
