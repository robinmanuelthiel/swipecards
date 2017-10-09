using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

namespace SwipeCards.Demo.Forms
{
    public partial class TabbedDemoPage : Xamarin.Forms.TabbedPage
    {
        public TabbedDemoPage()
        {
            InitializeComponent();
            this.On<Xamarin.Forms.PlatformConfiguration.Android>().SetIsSwipePagingEnabled(false);
            BindingContext = new MainViewModel();
        }
    }
}
