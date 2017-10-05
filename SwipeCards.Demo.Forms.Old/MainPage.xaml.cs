using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Collections.ObjectModel;

namespace SwipeCards.Demo.Forms
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var contents = new ObservableCollection<string>
            {
                "Card No 1",
                "Card No 2",
                "Card No 3",
                "Card No 4"
            };

            CardStackView.ItemsSource = contents;
        }
    }
}
