using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SynchronizationDetails
{
    public bool SynchronizeTransforms = true;

    // Not sure what the Unity editor does with a TimeSpan so using float.
    public float SynchronizationIntervalSeconds = 1.0f;
}
