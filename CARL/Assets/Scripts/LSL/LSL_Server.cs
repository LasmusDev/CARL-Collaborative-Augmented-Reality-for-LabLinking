using LSL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSL_Server : MonoBehaviour
{
    StreamOutlet streamOut;
    string[] buffer;
    public void Start()
    {
        buffer = new string[1];
        streamOut = new StreamOutlet(new StreamInfo("Server_Stream", "Server_Debug_Log", 1, 0, channel_format_t.cf_string), 8, 32);
        Application.logMessageReceived += Application_logMessageReceived;
    }

    private void Application_logMessageReceived(string message, string stackTrace, LogType type)
    {     
        buffer[0] = message + type.ToString();
        streamOut.push_sample(buffer);
    }

    private void OnApplicationQuit()
    {
        buffer = new string[1];
        buffer[0] = "Application Quit";
        streamOut.push_sample(buffer, 0.0, true);
    }

}
