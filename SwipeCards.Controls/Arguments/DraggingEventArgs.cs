using System;

namespace SwipeCards.Controls.Arguments
{
    public class DraggingEventArgs : EventArgs
    {
        public readonly object Item;

        public DraggingEventArgs(object item)
        {
            this.Item = item;
        }
    }
}
