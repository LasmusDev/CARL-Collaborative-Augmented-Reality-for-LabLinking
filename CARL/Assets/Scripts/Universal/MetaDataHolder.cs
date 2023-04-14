using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Holds information about this client or server, and provides debug convenience by automatically starting/connecting to the server.
/// </summary>
public class MetaDataHolder : MonoBehaviour
{
    private static MetaDataHolder instance;
    public CustomDeviceType deviceType;
    public ConditionInfo condition;
    public bool immediateNetworkStart;

    public static bool IsHLClient { get => Instance.deviceType == CustomDeviceType.HLCLIENT; }
    public static bool IsOTClient { get => Instance.deviceType == CustomDeviceType.OTCLIENT; }
    public static bool IsServer { get => Instance.deviceType == CustomDeviceType.SERVER; }

    public static bool PingsActive { get => Instance.condition == ConditionInfo.PING; }
    public static MetaDataHolder Instance
    {
        get
        {
            if (instance != null)
            {
                instance = FindObjectOfType<MetaDataHolder>();
            }
            return instance;
        }
        set => instance = value;
    }

    public void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        if (immediateNetworkStart)
        {
            StartCoroutine(StartServerNextFrame());
        }
    }
    public IEnumerator StartServerNextFrame()
    {
        yield return null; //Delay by one frame to give other scripts startup time
        switch (deviceType)
        {
            case CustomDeviceType.HLCLIENT: NetworkManager.Singleton.StartClient(); Debug.LogWarning("Immediate Network start on HL-client is most likely a bad idea!"); break;
            case CustomDeviceType.OTCLIENT: NetworkManager.Singleton.StartClient(); break;
            case CustomDeviceType.SERVER: NetworkManager.Singleton.StartServer(); break;
        }
    }
}

public enum CustomDeviceType
{
    SERVER, HLCLIENT, OTCLIENT
}
public enum ConditionInfo
{
    PING, NOPING
}