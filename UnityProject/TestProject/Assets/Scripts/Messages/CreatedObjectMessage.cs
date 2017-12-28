namespace SharedHolograms.Messages
{
    using BroadcastMessaging;
    using System.IO;
    using UnityEngine;

    public class CreatedObjectMessage : TransformMessage
    {
        public CreatedObjectMessage()
        {        
        }
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            this.ObjectType = reader.ReadString();
            this.ParentAnchorId = reader.ReadString();
        }
        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(this.ObjectType);
            writer.Write(this.ParentAnchorId);
        }
        public string ObjectType { get; set; }
        public string ParentAnchorId { get; set; }

        public static void Send(
            MessageService messageService, 
            string objectType, 
            GameObject gameObject,
            GameObject worldAnchorParent)
        {
            messageService.Send(
                new CreatedObjectMessage()
                {
                    ObjectId = gameObject.name,
                    ObjectType = objectType,
                    ParentAnchorId = worldAnchorParent.name,
                    LocalPosition = gameObject.transform.localPosition,
                    LocalScale = gameObject.transform.localScale,
                    LocalRotation = gameObject.transform.localRotation
                }
            );
        }
    }
}