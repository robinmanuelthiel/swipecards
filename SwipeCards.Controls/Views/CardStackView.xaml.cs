using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SwipeCards.Controls
{
    public partial class CardStackView : ContentView
    {
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(CardStackView), null, BindingMode.TwoWay,
            propertyChanged: OnItemsSourcePropertyChanged);

        private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((CardStackView)bindable).Setup();
        }

        void OnItemsSourcePropertyChanged(IList oldValue, IList newValue)
        {
            Setup();
        }

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(CardStackView), null, propertyChanged: (bindable, oldValue, newValue) => ((CardStackView)bindable).Setup());
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly BindableProperty CardMoveDistanceProperty = BindableProperty.Create(nameof(CardMoveDistance), typeof(int), typeof(CardStackView), 0);
        public int CardMoveDistance
        {
            get { return (int)GetValue(CardMoveDistanceProperty); }
            set { SetValue(CardMoveDistanceProperty, value); }
        }

        public static BindableProperty SwipedRightCommandProperty = BindableProperty.Create(nameof(SwipedRightCommand), typeof(ICommand), typeof(CardStackView), null);
        public ICommand SwipedRightCommand
        {
            get { return (ICommand)GetValue(SwipedRightCommandProperty); }
            set { SetValue(SwipedRightCommandProperty, value); }
        }

        public static BindableProperty SwipedLeftCommandProperty = BindableProperty.Create(nameof(SwipedLeftCommand), typeof(ICommand), typeof(CardStackView), null);
        public ICommand SwipedLeftCommand
        {
            get { return (ICommand)GetValue(SwipedLeftCommandProperty); }
            set { SetValue(SwipedLeftCommandProperty, value); }
        }

        // Called when a card is swiped left/right
        public Action<object> SwipedRight = null;
        public Action<object> SwipedLeft = null;



        private const int numberOfCards = 2;
        private const int animationLength = 250;

        private CardView[] cardViews = new CardView[numberOfCards];
        private float defaultSubcardScale = 0.8f;
        private float cardDistance = 0;
        private int itemIndex = 0;

        public CardStackView()
        {
            InitializeComponent();

            // Register pan gesture
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            TouchObserber.GestureRecognizers.Add(panGesture);
        }

        public void Setup()
        {
            // Add two cards to stack
            // Use inverse direction to ensure that first card is on top
            for (var i = numberOfCards - 1; i >= 0; i--)
            {
                // Create CardView
                var cardView = new CardView(ItemTemplate);
                cardView.IsVisible = false;
                cardViews[i] = cardView;
                cardView.Scale = (i == 0) ? 1.0f : defaultSubcardScale;
                cardView.IsEnabled = false;

                // Add CardView to UI
                CardStack.Children.Add(
                    cardView,
                    Constraint.Constant(0), // X
                    Constraint.Constant(0), // Y
                    Constraint.RelativeToParent((parent) => { return parent.Width; }), // Width
                    Constraint.RelativeToParent((parent) => { return parent.Height; }) // Height
                );
            }

            itemIndex = 0;

            ShowNextCard();
        }

        async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    //HandleTouchStart();
                    break;
                case GestureStatus.Running:
                    HandleTouchRunning((float)e.TotalX);
                    break;
                case GestureStatus.Completed:
                    await HandleTouchCompleted();
                    break;
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // Recalculate move distance
            CardMoveDistance = (int)(width / 3);
        }

        private void HandleTouchRunning(float xDiff)
        {
            var topCard = cardViews[0];
            var backCard = cardViews[1];

            // Move the top card
            if (topCard.IsVisible)
            {
                // Move the card
                topCard.TranslationX = (xDiff);

                // Calculate a angle for the card
                float rotationAngel = (float)(0.3f * Math.Min(xDiff / this.Width, 1.0f));
                topCard.Rotation = rotationAngel * 57.2957795f;

                // Keep a record of how far its moved
                cardDistance = xDiff;
            }

            // Scale the backcard
            if (backCard.IsVisible)
            {
                backCard.Scale = Math.Min(defaultSubcardScale + Math.Abs((cardDistance / CardMoveDistance) * (1.0f - defaultSubcardScale)), 1.0f);
            }
        }

        private async Task HandleTouchCompleted()
        {
            var topCard = cardViews[0];
            var backCard = cardViews[1];

            // Check if card has been dragged far enough to trigger action
            if (Math.Abs(cardDistance) >= CardMoveDistance)
            {
                // Move card off the screen
                await topCard.TranslateTo(cardDistance > 0 ? this.Width : -this.Width, 0, animationLength / 2, Easing.SpringOut);
                topCard.IsVisible = false;

                // Fire events
                if (cardDistance > 0)
                {
                    SwipedRight?.Invoke(ItemsSource[itemIndex]);
                    if (SwipedRightCommand != null && SwipedRightCommand.CanExecute(ItemsSource[itemIndex]))
                        SwipedRightCommand.Execute(ItemsSource[itemIndex]);
                }
                else
                {
                    SwipedLeft?.Invoke(ItemsSource[itemIndex]);
                    if (SwipedLeftCommand != null && SwipedLeftCommand.CanExecute(ItemsSource[itemIndex]))
                        SwipedLeftCommand.Execute(ItemsSource[itemIndex]);
                }

                // Next card
                itemIndex++;
                ShowNextCard();
            }
            else
            {
                //// Move card back to the center
                //await topCard.TranslateTo((-topCard.X), -topCard.Y, animationLength, Easing.SpringOut);
                //await topCard.RotateTo(0, animationLength, Easing.SpringOut);

                //// Scale the back card down
                //await backCard.ScaleTo(defaultSubcardScale, animationLength, Easing.SpringOut);


                // Move card back to the center
                var traslateAnmimation = topCard.TranslateTo((-topCard.X), -topCard.Y, animationLength, Easing.SpringOut);
                var rotateAnimation = topCard.RotateTo(0, animationLength, Easing.SpringOut);

                // Scale the back card down
                var scaleAnimation = backCard.ScaleTo(defaultSubcardScale, animationLength, Easing.SpringOut);

                await Task.WhenAll(new List<Task> { traslateAnmimation, rotateAnimation, scaleAnimation });
            }
        }

        private void ShowNextCard()
        {
            if (ItemsSource == null)
                return;

            for (int i = 0; i < Math.Min(numberOfCards, ItemsSource.Count); i++)
            {
                var cardView = cardViews[i];
                cardView.IsVisible = false;
                cardView.Scale = (i == 0) ? 1.0f : defaultSubcardScale;
                cardView.Rotation = 0;
                cardView.TranslationX = 0;

                // Check if next item is available
                if (itemIndex + i < ItemsSource.Count)
                {
                    cardView.Update(ItemsSource[itemIndex + i]);
                    cardView.IsVisible = true;
                }
            }
        }
    }
}
