using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LSLNetwork;
using System;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;
using static LSLNetwork.BridgeConnectionManager;
using TMPro;
using UnityEngine.UI;

public class BridgeConnectionSettings : MonoBehaviour
{
    private BridgeConnectionManager connectionManager;

    public bool connected;

    public string host;
    public int port;

    // Start is called before the first frame update
    void Start()
    {
        connectionManager = BridgeConnectionManager.Instance;
        connected = false;

    }

    // Update is called once per frame
    void Update()
    {
        connected = !BridgeConnectionManager.Instance._noConnection;

    }

    public void SetInputs(string host, int port)
    {
        this.host = host;
        this.port = port;
        connectionManager.SetInputs(host, port);
    }

    public void ConnectionToggle()
    {
        if (!connected)
        {
            connectionManager.Connect();
            GameObject.Find("Settings/Canvas/Buttons/Connect/IconAndText/TextMeshPro").GetComponent<TextMeshPro>().text = "Disconnect";
            GameObject.Find("Settings/Canvas/Status").GetComponent<TextMeshProUGUI>().text = "Connected";
        } else 
        {
            connectionManager.StopStream();
            GameObject.Find("Settings/Canvas/Buttons/Connect/IconAndText/TextMeshPro").GetComponent<TextMeshPro>().text = "Connect";
            GameObject.Find("Settings/Canvas/Status").GetComponent<TextMeshProUGUI>().text = "Disconnected";
        }
    }

}
