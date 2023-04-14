using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeLag : MonoBehaviour
{
    [Range(0, 2000)]
    public long frameDurationMs;

    public void Update()
    {
        long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        while(milliseconds + frameDurationMs > DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)
        {

        }
    }
}
