using Xamarin.Forms;

namespace SwipeCards
{
	public partial class CardView : ContentView
	{
		public CardView(DataTemplate itemTemplate)
		{
			InitializeComponent();

			Container.Content = itemTemplate.CreateContent() as View;
		}

		public void Update(object item)
		{
			Container.Content.BindingContext = item;
		}
	}
}
