namespace SharedHolograms
{
    using UnityEngine;
    using System;
    using System.Collections;

    public class CoRoutineRunner : MonoBehaviour
    {
        public static CoRoutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new InvalidOperationException("Did you add a CoRoutineRunner to your sccene?");
                }
                return (instance);
            }
            private set
            {
                Instance = value;
            }
        }
        public CoRoutineRunner()
        {
            if (instance != null)
            {
                throw new InvalidOperationException("We're expecting only one of these in a project");
            }
            instance = this;
        }
        public void Run(IEnumerator enumerator)
        {
            StartCoroutine(enumerator);
        }
        static CoRoutineRunner instance;
    }
}
