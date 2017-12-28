namespace SharedHolograms.Messages
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.IO;
    using BroadcastMessaging;

    public class IdentifiedObjectMessage : Message
    {
        public IdentifiedObjectMessage()
        {
        }
        public override void Load(BinaryReader reader)
        {
            this.ObjectId = reader.ReadString();
        }
        public override void Save(BinaryWriter writer)
        {
            writer.Write(this.ObjectId);
        }
        public string ObjectId { get; set; }
    }
}
