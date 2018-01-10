namespace SharedHolograms
{
    using System;

    public class BusyStatusChangedEventArgs : EventArgs
    {
        public BusyStatusChangedEventArgs(bool busy = true)
        {
            this.Busy = busy;
        }
        public bool Busy { get; private set; }
    }
}
