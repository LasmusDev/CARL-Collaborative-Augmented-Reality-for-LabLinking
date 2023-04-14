using LSLNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawns objects once they have been found by motive, and keeps them in sync with updates provided by motive.
/// </summary>
public class OptitrackObjectSpawner : MonoBehaviour
{
    public OptitrackStreamingClient streamingClient;
    public static OptitrackObjectSpawner Instance;
    //A list of all potentially spawned objects
    public List<MotiveObject> trackedObjectSpawnables;
    //A list of the already spawned objects associated with motive objects.
    public List<SynchronizedObject> spawnedTrackedObjects;
    //How often new 
    public int spawnChecksPerSecond;
    public int spawnOngoing;
    public Action<int, string> OnNetworkSpawnSent;
    public LSL_OT lsl_connector;
    

    void Start()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        if(streamingClient == null)
        {
            streamingClient = OptitrackStreamingClient.FindDefaultClient();
            foreach (MotiveObject iop in trackedObjectSpawnables)
            {
                this.streamingClient.RegisterRigidBody(this, iop.streamingID);
            }
        }
        if (!lsl_connector)
        {
            lsl_connector = FindObjectOfType<LSL_OT>();
        }
        StartCoroutine(CheckForSpawningUpdate());    
        

    }

    /// <summary>
    /// Checks all optitrack objects for changes and spawns/updates accordingly. 
    /// Spawns only one object per frame and waits for 1/spawnChecksPerSecond after checking all objects.
    /// </summary>
    /// <returns>Coroutine</returns>
    public IEnumerator CheckForSpawningUpdate()
    {
        while (true)
        {
            foreach (MotiveObject motiveObject in trackedObjectSpawnables)
            {
                SynchronizedObject trackedObject = spawnedTrackedObjects.Find(x => x.trackingID == motiveObject.streamingID);
                //If the current object has not been spawned
                if (!trackedObject)
                {   //check if we are still waiting for the previous spawning to finish
                    if (spawnOngoing == -1)
                    {
                        OptitrackRigidBodyState orbs = streamingClient.GetLatestRigidBodyState(motiveObject.streamingID);
                        //If motive has found the missing object, spawn it and mark it as Optitrack-controlled.
                        if (orbs != null)
                        {
                            //Try catch to not break Coroutine in case of error
                            try
                            {
                                SendNetworkSpawnCommand(motiveObject.unityObject.name, motiveObject.streamingID);
                                OnNetworkSpawnSent?.Invoke(motiveObject.streamingID, motiveObject.unityObject.name);
                            }
                            catch
                            {
                                Debug.LogError("Couldnt send network spawn command.");
                            }
                            yield return null;
                        }
                    }
                } else
                {
                    //Update the spawned objects position& rotation based on motives data.
                    OptitrackRigidBodyState orbs = streamingClient.GetLatestRigidBodyState(motiveObject.streamingID);
                    trackedObject.transform.position = new Vector3(orbs.Pose.Position.x * -1, orbs.Pose.Position.y * -1, orbs.Pose.Position.z) + motiveObject.posOffset;
                    Vector3 rot = orbs.Pose.Orientation.eulerAngles;
                    trackedObject.transform.rotation = Quaternion.Euler(new Vector3(rot.x * -1, rot.y * -1, rot.z)) * Quaternion.Euler(motiveObject.rotOffset);                   
                }           
            }
            try
            {
                //Send an update about all tracked objects to the LSL.
                lsl_connector.PushToRecorder(BridgeConnectionManager.PackTransformsAsString(spawnedTrackedObjects.Select(o => o.transform)));
            } catch (Exception e)
            {
                ClientDebugSender.ErrorToServer(e.Message + e.StackTrace);
            }
            yield return new WaitForSeconds(1/spawnChecksPerSecond);
        }
        
    }

    /// <summary>
    /// Spawns the prefab across the network
    /// </summary>
    /// <param name="prefabName">the prefabs name</param>
    /// <param name="trackingID">The tracking ID of the prefab</param>
    public void SendNetworkSpawnCommand(string prefabName, int trackingID)
    {
        spawnOngoing = trackingID;
        ClientDebugSender.DebugToServer("Spawning " + prefabName);
        SynchronizedObjectManager.Instance.SpawnSynchronizedObject(prefabName, NetworkManager.Singleton.LocalClientId, TrackingState.OTTRACKED, trackingID);
    }
}

//Associates a GameObject with a motive-streaming ID and optionally applies a rotation/position offset to it.
[System.Serializable]
public class MotiveObject {
    public int streamingID;
    public GameObject unityObject;
    public Vector3 posOffset;
    public Vector3 rotOffset;
}
