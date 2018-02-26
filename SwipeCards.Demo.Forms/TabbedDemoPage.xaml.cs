using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using SwipeCards.Controls.Arguments;

namespace SwipeCards.Demo.Forms
{
    public partial class TabbedDemoPage : Xamarin.Forms.TabbedPage
    {
        public TabbedDemoPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();

            // Disable swiping between tabs on Android, as it collides 
            // with Swipe Card's swipe gestures
            this.On<Xamarin.Forms.PlatformConfiguration.Android>().SetIsSwipePagingEnabled(false);
        }

        void CardStackView_Swiped(object sender, Controls.Arguments.SwipedEventArgs e)
        {

        }

        void RestartButton_Clicked(object sender, System.EventArgs e)
        {
            CardStackView.Setup();
        }

        void SwipeLeftButton_Clicked(object sender, System.EventArgs e)
        {
            CardStackView.Swipe(SwipeDirection.Left);
        }

        void SwipeRightButton_Clicked(object sender, System.EventArgs e)
        {
            CardStackView.Swipe(SwipeDirection.Right);
        }
    }
}
