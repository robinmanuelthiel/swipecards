using System;

namespace SwipeCards.Controls.Arguments
{
    public class SwipedEventArgs : EventArgs
    {
        public readonly object Item;
        public readonly SwipeDirection Direction;

        public SwipedEventArgs(object item, SwipeDirection direction)
        {
            this.Item = item;
            this.Direction = direction;
        }
    }

    public enum SwipeDirection
    {
        Left, Right
    }
}
