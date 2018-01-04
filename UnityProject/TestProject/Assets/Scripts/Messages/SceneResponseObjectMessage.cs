namespace SharedHolograms.Messages
{
    using System;
    using System.IO;

    public class SceneResponseObjectMessage : CreatedObjectMessage
    {
        public Guid DestinationDeviceId { get; set; }

        public SceneResponseObjectMessage()
        {

        }
        public static SceneResponseObjectMessage FromCreateObjectMessage(CreatedObjectMessage msg)
        {
            return (
                new SceneResponseObjectMessage()
                {
                    ObjectId = msg.ObjectId,
                    ObjectType = msg.ObjectType,
                    ParentAnchorId = msg.ParentAnchorId,
                    LocalPosition = msg.LocalPosition,
                    LocalRotation = msg.LocalRotation,
                    LocalScale = msg.LocalScale
                }
            );
        }
        public SceneResponseObjectMessage(CreatedObjectMessage msg)
        {

        }

        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            this.DestinationDeviceId = new Guid(reader.ReadString());
        }
        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(this.DestinationDeviceId.ToString());
        }
    }
}

