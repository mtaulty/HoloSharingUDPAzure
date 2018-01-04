namespace SharedHolograms.Messages
{
    using BroadcastMessaging;
    using System;
    using System.IO;

    public class SceneRequestMessage : Message
    {
        public Guid RequestingDeviceId { get; set; }
        public Guid ProvidingDeviceId { get; set; }

        public override void Load(BinaryReader reader)
        {
            this.RequestingDeviceId = new Guid(reader.ReadString());
            this.ProvidingDeviceId = new Guid(reader.ReadString());
        }
        public override void Save(BinaryWriter writer)
        {
            writer.Write(this.RequestingDeviceId.ToString());
            writer.Write(this.ProvidingDeviceId.ToString());
        }
    }
}
