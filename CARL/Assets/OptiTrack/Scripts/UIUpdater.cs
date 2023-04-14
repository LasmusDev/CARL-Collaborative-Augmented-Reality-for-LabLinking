using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIUpdater : MonoBehaviour
{
    public TMPro.TMP_Text textDisplay;
    public OptitrackObjectSpawner spawner;
    public NetworkManager networkManager;
    public OptitrackStreamingClient streamingClient;
    public Button connectButton;
    public Button startSpawnButton;

    // Start is called before the first frame update
    void Start()
    {
        if (!networkManager)
        {
            networkManager = NetworkManager.Singleton;
        }
        if (!spawner)
        {
            spawner = OptitrackObjectSpawner.Instance;
        }
        if (!streamingClient)
        {
            streamingClient = FindObjectOfType<OptitrackStreamingClient>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        string text = "";
        if (networkManager.IsConnectedClient){
            text += "Connection to Server Functional\n";
        } else  {
            text += "Not Connected to Server\n";
        }
        if (!streamingClient.enabled)
        {
            text += "Motive streaming client has crashed, this requires a full restart.\n";
        } 
        foreach(SynchronizedObject so in spawner.spawnedTrackedObjects)
        {
            text += so.name + "\n     Pos: " + so.transform.localPosition + "\n     Rot" + so.transform.localRotation + "\n";
        }
        textDisplay.text = text;

        //Buttons
        connectButton.enabled = !networkManager.IsConnectedClient;
        startSpawnButton.enabled = !(spawner.spawnedTrackedObjects.Count > 0 || spawner.spawnOngoing == -1);
    }

    public void ConnectButtonPressed()
    {
        if (networkManager.IsClient)
        {
            networkManager.Shutdown();
        }
        networkManager.StartClient();
    }

    public void StartSpawningButtonPressed()
    {
        spawner.spawnOngoing = -1;
    }
}
