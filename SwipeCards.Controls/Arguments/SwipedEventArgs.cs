using System;

namespace SwipeCards
{
	public class SwipedEventArgs : EventArgs
	{
		public object Item { get; private set; }
		public SwipeDirection Direction { get; private set; }

		public SwipedEventArgs(object item, SwipeDirection direction)
		{
			Item = item;
			Direction = direction;
		}
	}

	public enum SwipeDirection
	{
		Left,
		Right
	}
}
