namespace SharedHolograms
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using System.Linq;
    using UnityEngine.XR.WSA;

    public class WorldAnchorMap
    {
        public WorldAnchorMap()
        {
            this.anchors = new List<GameObject>();
        }
        public bool GetOrAddWorldAnchorForPosition(
            Vector3 position, 
            Vector3 forward,
            out GameObject anchorObject)
        {
            bool added = false;

            anchorObject = null;

            anchorObject =
                this.anchors.Select(
                    a => new
                    {
                        Instance = a,
                        Distance = Vector3.Distance(a.transform.position, position)
                    }
                )
                .Where(
                    d => d.Distance < ANCHOR_DISTANCE)
                .OrderBy(
                    d => d.Distance)
                .Select(
                    d => d.Instance)
                .FirstOrDefault();

            added = (anchorObject == null);

            if (added)
            {
                anchorObject = this.AddAnchorWithNewIdAtPosition(position, forward);
            }
            return (added);
        }
        public GameObject GetById(string anchorId)
        {
            GameObject anchor = this.anchors.FirstOrDefault(g => g.name == anchorId);

            return (anchor);
        }
        public GameObject AddAnchorWithExistingIdAtOrigin(string id)
        {
            var anchor = new GameObject(id);
            anchor.transform.position = Vector3.zero;
            anchor.transform.forward = Vector3.forward;
            this.anchors.Add(anchor);
            return (anchor);
        }
        GameObject AddAnchorWithNewIdAtPosition(
            Vector3 position,
            Vector3 forward)
        {
            var anchor = this.AddAnchorWithExistingIdAtOrigin(Guid.NewGuid().ToString());
            anchor.transform.position = position;
            anchor.transform.forward = forward;
            anchor.AddComponent<WorldAnchor>();
            return (anchor);
        }
        static readonly float ANCHOR_DISTANCE = 3.0f;
        List<GameObject> anchors;
    }
}
