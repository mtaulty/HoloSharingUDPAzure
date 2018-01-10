namespace SharedHolograms
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public enum SceneReadyStatus
    {
        Waiting,
        NoOtherDevicesInScene,
        OtherDevicesInScene
    }
    public class SceneReadyEventArgs : EventArgs
    {
        public SceneReadyEventArgs(SceneReadyStatus status)
        {
            this.Status = status;
        }
        public SceneReadyStatus Status { get; private set; }
    }
}
