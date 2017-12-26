using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace SwipeCards.Demo.Forms
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> cards;
        public ObservableCollection<string> Cards
        {
            get { return cards; }
            set { cards = value; RaisePropertyChanged(); }
        }

        public MainViewModel()
        {
            cards = new ObservableCollection<string>
            {
                "Card No 1",
                "Card No 2",
                "Card No 3",
                "Card No 4"
            };
        }

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
