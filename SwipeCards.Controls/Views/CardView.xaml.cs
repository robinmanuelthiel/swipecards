using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SwipeCards.Controls
{
    public partial class CardView : ContentView
    {
        public object item;
        public object Item
        {
            get { return item; }
            set { item = value; OnPropertyChanged(); }
        }

        public CardView(int i)
        {
            InitializeComponent();
        }

        public void UpdateUi()
        {
            TextLabel.Text = (string)item;
        }
    }
}
