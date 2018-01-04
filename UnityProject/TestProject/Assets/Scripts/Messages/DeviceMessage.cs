namespace SharedHolograms.Messages
{
    using BroadcastMessaging;
    using System;
    using System.IO;

    public class DeviceMessage : Message
    {
        public Guid DeviceId { get; set; }

        public override void Load(BinaryReader reader)
        {
            this.DeviceId = new Guid(reader.ReadString());
        }
        public override void Save(BinaryWriter writer)
        {
            writer.Write(this.DeviceId.ToString());
        }
    }
}
