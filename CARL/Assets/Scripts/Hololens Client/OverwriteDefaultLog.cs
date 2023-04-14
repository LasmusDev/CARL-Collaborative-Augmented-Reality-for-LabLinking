using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Automatically forwards logmessages raised on the client to the server, to enable easier debugging by the administrator.
/// </summary>
public class OverwriteDefaultLog : MonoBehaviour
{
    public ServerLogLevel logLevel;
    // Start is called before the first frame update
    void Start()
    {
        Application.logMessageReceived += Application_logMessageReceived;
        Application.logMessageReceivedThreaded += Application_logMessageReceived;
    }

    private void Application_logMessageReceived(string logString, string stackTrace, LogType type)
    {
        try
        {
            if (type == LogType.Log && logLevel >= ServerLogLevel.ALL)
            {
                ClientDebugSender.DebugToServer(logString);
                return;
            }
            if (type == LogType.Warning && logLevel >= ServerLogLevel.WARNING)
            {
                ClientDebugSender.WarningToServer(logString);
                return;
            }
            if (type == LogType.Assert && logLevel >= ServerLogLevel.ASSERT)
            {
                ClientDebugSender.WarningToServer(logString);
                return;
            }
            if (type == LogType.Error && logLevel >= ServerLogLevel.ERROR)
            {
                ClientDebugSender.ErrorToServer(logString);
                return;
            }
            if (type == LogType.Exception && logLevel >= ServerLogLevel.EXCEPTION)
            {
                ClientDebugSender.ErrorToServer(logString + stackTrace);
                return;
            }
        }
        catch
        {

        }
    }
   
}

public enum ServerLogLevel
{
    NONE = 0, EXCEPTION = 1, ERROR = 2, ASSERT = 3, WARNING = 4, ALL = 5
}
