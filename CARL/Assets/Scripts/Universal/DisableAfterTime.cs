using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Attached to pings. Makes sure the ping is orientated towards the player, and disables the ping after a short time.
/// </summary>
public class DisableAfterTime : MonoBehaviour
{
    public float time;
    private void OnEnable()
    {
        transform.LookAt(Camera.main.transform);
        StartCoroutine(DisableAfter());
    }
    public IEnumerator DisableAfter()
    {
        yield return new WaitForSeconds(time);
        this.gameObject.SetActive(false);
    }
}
