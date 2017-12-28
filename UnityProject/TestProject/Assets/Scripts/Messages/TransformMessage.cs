namespace SharedHolograms.Messages
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using BroadcastMessaging;
    using System.IO;

    public class TransformMessage : IdentifiedObjectMessage
    {
        public TransformMessage()
        {

        }
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            this.LocalPosition = reader.ReadVector3();
            this.LocalScale = reader.ReadVector3();
            this.LocalRotation = reader.ReadQuaternion();
        }
        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(this.LocalPosition);
            writer.Write(this.LocalScale);
            writer.Write(this.LocalRotation);
        }
        public Vector3 LocalPosition { get; set; }
        public Vector3 LocalScale { get; set; }
        public Quaternion LocalRotation { get; set; }
    }
}
