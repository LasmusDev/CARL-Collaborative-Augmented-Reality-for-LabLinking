using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unused script. Previously used to snap objects to the virtual table
/// if they were placed close above it. Removed because user feedback suggested that this feature 
/// caused more interaction problems than it solved.
/// </summary>
public class SnapToTable : MonoBehaviour
{
    public List<Collider> collisions;

    public void Start()
    {
        collisions = new List<Collider>();
    }

    public void OnTriggerEnter(Collider other)
    {
        collisions.Add(other);
    }

    public void OnTriggerExit(Collider other)
    {
        collisions.Remove(other);
    }

    public void CheckAndSnap()
    {
        Collider snapCollider = collisions.Find(x => x.CompareTag("SnapCollider"));
        if (snapCollider != null)
        {
            Debug.Log("Snapping to Table");
            //this.transform.up = snapCollider.transform.forward;
            this.transform.position = new Vector3(this.transform.position.x, snapCollider.transform.position.y, this.transform.position.z);
        }
    }
}
