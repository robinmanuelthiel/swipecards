using System;

namespace SwipeCards
{
	public class DraggingEventArgs : EventArgs
	{
		public object Item { get; private set; }
		public double Distance { get; private set; }

		public DraggingEventArgs(object item, double distance)
		{
			Item = item;
			Distance = distance;
		}
	}
}
