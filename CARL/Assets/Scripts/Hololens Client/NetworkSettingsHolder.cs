using LSLNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;

/// <summary>
/// Connector class for the Hololens-Client UI
/// </summary>
public class NetworkSettingsHolder : NetworkBehaviour
{
    /// <summary>
    /// A touchscreen keyboard provided by the MRTK. Used to get typing Input in Hololens-Devices.
    /// </summary>
    TouchScreenKeyboard keyboard;
    public NetworkManager networkManager;
    public string connectIP;
    /// <summary>
    /// A text element to display the keyboard input
    /// </summary>
    public TMP_Text IPDisplay;
    // Start is called before the first frame update
    void Start()
    {
        if (networkManager == null)
        {
            networkManager = GameObject.FindObjectOfType<NetworkManager>();
        }
        networkManager.OnClientConnectedCallback += ConnectedToServer;
        networkManager.OnClientDisconnectCallback += DisconnectedFromServer;
        connectIP = networkManager.gameObject.GetComponent<UNetTransport>().ConnectAddress;
        IPDisplay.text = connectIP;
    }

    /// <summary>
    /// Keep variables and UI in sync with input
    /// </summary>
    public void Update()
    {
        if (keyboard != null && keyboard.active)
        {
            connectIP = keyboard.text;
            IPDisplay.text = connectIP;
        }
    }

    /// <summary>
    /// Open on-Screen Keyboard
    /// </summary>
    public void StartKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open(connectIP);       
    }

    /// <summary>
    /// Starts the network client
    /// </summary>
    public void StartClient()
    {
        if (networkManager == null)
        {
            networkManager = GameObject.FindObjectOfType<NetworkManager>();
        }
        if (networkManager != null && !networkManager.IsServer && !networkManager.IsClient)
        {
            networkManager.gameObject.GetComponent<UNetTransport>().ConnectAddress = connectIP;
            networkManager.StartClient();
        }
    }

    /// <summary>
    /// On connection to server, sends a message to bridge and disables self.
    /// </summary>
    /// <param name="ignored"></param>
    public void ConnectedToServer(ulong ignored)
    {
        try
        {
            BridgeConnectionManager.Instance.Send("Connected To Server", BridgeConnectionManager.EventChannelKey);
        } catch 
        {
            Debug.LogError("Couldnt send connect to Bridge");
        }
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// On disconnection to server, sends a message to bridge and re-enables self.
    /// </summary>
    /// <param name="ignored"></param>
    public void DisconnectedFromServer(ulong ignored)
    {
        try
        {
            BridgeConnectionManager.Instance.Send("Disconnected From Server", BridgeConnectionManager.EventChannelKey);
        }
        catch
        {
            Debug.LogError("Couldnt send disconnect to Bridge");
        }
        this.gameObject.SetActive(true);
    }
}
