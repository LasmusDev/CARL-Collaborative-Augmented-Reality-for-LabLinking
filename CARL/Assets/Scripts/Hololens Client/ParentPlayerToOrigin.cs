using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentPlayerToOrigin : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (MetaDataHolder.IsHLClient)
        {
            transform.SetParent(FindObjectOfType<OriginSeeker>().transform, false);
            //These shouldnt be necessary, but dont ever hurt in case of weird spawning or such.
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
