using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SwipeCards.Controls
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
