using LSLNetwork;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles spawning of goals and distance calculation for the goals.
/// </summary>
public class GoalSpawner : MonoBehaviour, IMessageHandler
{
    public List<Goal> goals;
    public Transform originObject;
    public NetworkManager networkManager;
    public const string SpawnGoalsKey = "SpawnGoals";
    public const string GetGoalDistanceKey = "GoalDistances";
    private bool msgHandlerSet;
    public bool MsgHandlerSet { get => msgHandlerSet; }

    public void Start()
    {
        if (!networkManager)
        {
            networkManager = FindObjectOfType<NetworkManager>();
            
        }
        StartCoroutine(RegisterMsgHandlers());
    }



    public IEnumerator RegisterMsgHandlers()
    {
        while (!MsgHandlerSet)
        {
            
            if (networkManager.CustomMessagingManager != null)
            {
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(SpawnGoalsKey, SpawnGoalsHandler);
                networkManager.CustomMessagingManager.RegisterNamedMessageHandler(GetGoalDistanceKey, GoalDistanceHandler);
            }
            yield return null;
        }
    }

    public void SpawnGoalsHandler(ulong ignoredUlong, FastBufferReader messagePayload)
    {
        messagePayload.ReadValueSafe(out int playerID);
        ClientDebugSender.DebugToServer("Received goal spawning for " + playerID);
        SpawnAllGoals(playerID);
    }

    public void GoalDistanceHandler(ulong ignoredUlong, FastBufferReader messagePayload)
    {
        GetAllGoalDistances();
    }


    public void GetAllGoalDistances()
    {
        string result = "RESULTING ACCURACY: \n";   
        foreach(Goal g in goals)
        {
            if (g.instance)
            {
               result += g.instance.name + " : "+ g.instance.GetComponent<GoalComponent>().DistanceToClosest() + "\n";
            }
        }
        if (!BridgeConnectionManager.Instance._noConnection)
        {        
            BridgeConnectionManager.Instance.Send(result, BridgeConnectionManager.HLTrackingChannelKey);
        }
    }

    public void SpawnAllGoals(int playerID)
    {
        if (Application.isPlaying)
        {
            foreach (Goal g in goals)
            {
                if (!g.IsSpawned && (!NetworkManager.Singleton.IsClient || ((ulong)playerID == g.playerID)))
                {
                    g.instance = Instantiate(g.obj, originObject, false);
                    g.instance.transform.localPosition = g.pos;
                    g.instance.transform.localRotation = g.rot;
                }
            }
        }
        else
        {
            MasterSpawn();
        }
    }
    public void MasterSpawn()
    {
        foreach (Goal g in goals)
        {
            g.instance = Instantiate(g.obj, originObject, false);
            g.instance.transform.localPosition = g.pos;
            g.instance.transform.localRotation = g.rot;
        }
    }
}

[System.Serializable]
public class Goal
{
    public GameObject obj;
    public GameObject instance;
    public bool IsSpawned { get => instance != null; }
    public Vector3 pos;
    public Quaternion rot;
    public ulong playerID;
}

[System.Serializable]
public enum GoalTag
{
    NONE,KNIFE, FORK, PLATE, WINE_BOTTLE, CHOPPING_BOARD, CUP, BOWL, BREAD, MUESLI, WINE_GLASS, SPOON
}
