namespace SharedTestAppCode
{
    using BroadcastMessaging;
    using System;
    using System.IO;

    public class TestGuidMessage : Message
    {
        public override void Load(BinaryReader reader)
        {
            this.Id = new Guid(reader.ReadString());
        }
        public override void Save(BinaryWriter writer)
        {
            writer.Write(this.Id.ToString());
        }
        public Guid Id { get; set; }
    }
}
