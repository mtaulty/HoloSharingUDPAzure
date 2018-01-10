namespace SharedHolograms
{
    using System;

    public class HologramEventArgs : EventArgs
    {
        public HologramEventArgs(Guid objectId)
        {
            this.ObjectId = objectId;
        }
        public Guid ObjectId { get; private set; }
    }
}
