using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xamarin.Forms.Internals;
using SwipeCards.Controls.Arguments;
using Xamarin.Forms.Xaml;
using System.Reflection;

namespace SwipeCards.Controls
{
    public partial class CardStackView : ContentView
    {
        #region ItemsSource Property

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource), typeof(IList),
                typeof(CardStackView),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnItemsSourcePropertyChanged);

        private static NotifyCollectionChangedEventHandler CollectionChangedEventHandler;
        private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            // (Re-)subscribe to source changes
            if (newValue is INotifyCollectionChanged)
            {
                // If ItemSource is INotifyCollectionChanged, it can notify us about collection changes
                // In this case, we can use this, as a trigger for Setup()

                // Unsubscibe before
                if (CollectionChangedEventHandler != null)
                    ((INotifyCollectionChanged)newValue).CollectionChanged -= CollectionChangedEventHandler;

                // Subscribe event handler
                CollectionChangedEventHandler = (sender, e) => ItemsSource_CollectionChanged(sender, e, (CardStackView)bindable);
                ((INotifyCollectionChanged)newValue).CollectionChanged += CollectionChangedEventHandler;
            }

            // Even if ItemsSource is not INotifyCollectionChanged, we need to 
            // call Setup() whenever the whole collection changes
            ((CardStackView)bindable).Setup();
        }

        static void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e, CardStackView cardStackView)
        {
            cardStackView.Setup();
        }

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        #endregion

        #region ItemTemplate Property

        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(
                nameof(ItemTemplate),
                typeof(DataTemplate),
                typeof(CardStackView),
                new DataTemplate(() =>
                {
                    var label = new Label { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
                    label.SetBinding(Label.TextProperty, "Binding");
                    return new ViewCell { View = label };
                }),
                propertyChanged: OnItemTemplatePropertyChanged);

        private static void OnItemTemplatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((CardStackView)bindable).Setup();
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        #endregion

        #region Misc Properties

        public static readonly BindableProperty CardMoveDistanceProperty = BindableProperty.Create(nameof(CardMoveDistance), typeof(int), typeof(CardStackView), -1);

        /// <summary>
        /// Distance, that a card has to be dragged into one direction to trigger the flip
        /// </summary>
        /// <value>The card move distance.</value>
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

        //public static readonly BindableProperty HasShadowProperty = BindableProperty.Create(nameof(HasShadow), typeof(bool), typeof(CardStackView), false);
        //public bool HasShadow
        //{
        //    get { return (bool)GetValue(HasShadowProperty); }
        //    set { SetValue(HasShadowProperty, value); }
        //}

        #endregion

        public event EventHandler<SwipedEventArgs> Swiped;
        public event EventHandler<DraggingEventArgs> StartedDragging;
        public event EventHandler<DraggingEventArgs> FinishedDragging;

        private const int numberOfCards = 2;
        private const int animationLength = 250;
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

            Setup();
        }

        public void Setup()
        {
            // TODO: Reduce Setup() calls
            // When starting the app, Setup() gets called multiple times (OnItemsSourcePropertyChanged, OnItemTemplatePropertyChanged, ...). Try to reduce that to 1

            // Reset CardStack first
            CardStack.Children.Clear();

            // Add two cards (one for the front, one for the background) to the stack
            // Use inverse direction to ensure that first card is on top
            for (var i = numberOfCards - 1; i >= 0; i--)
            {
                // Create CardView
                var cardView = new CardView(ItemTemplate)
                {
                    IsVisible = false,
                    Scale = (i == 0) ? 1.0f : defaultSubcardScale,
                    IsEnabled = false
                };

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

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // Recalculate move distance
            if (CardMoveDistance == -1 && !width.Equals(-1))
                CardMoveDistance = (int)(width / 3);
        }

        async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    HandleTouchStart();
                    break;
                case GestureStatus.Running:
                    HandleTouchRunning((float)e.TotalX);
                    break;
                case GestureStatus.Completed:
                    //case GestureStatus.Canceled:
                    await HandleTouchCompleted();
                    break;
            }
        }

        private void HandleTouchStart()
        {
            if (itemIndex < ItemsSource.Count)
                StartedDragging?.Invoke(this, new DraggingEventArgs(ItemsSource[itemIndex]));
        }

        private void HandleTouchRunning(float xDiff)
        {
            if (itemIndex >= ItemsSource.Count)
                return;

            var topCard = CardStack.Children[numberOfCards - 1];
            var backCard = CardStack.Children[numberOfCards - 2];

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
            if (itemIndex >= ItemsSource.Count)
                return;

            var topCard = CardStack.Children[numberOfCards - 1];
            var backCard = CardStack.Children[numberOfCards - 2];

            // Check if card has been dragged far enough to trigger action
            if (Math.Abs(cardDistance) >= CardMoveDistance)
            {
                // Move card off the screen
                await topCard.TranslateTo(cardDistance > 0 ? this.Width : -this.Width, 0, animationLength / 2, Easing.SpringOut);
                topCard.IsVisible = false;

                // Fire events
                if (cardDistance > 0)
                {
                    Swiped?.Invoke(this, new SwipedEventArgs(ItemsSource[itemIndex], SwipeDirection.Right));
                    if (SwipedRightCommand != null && SwipedRightCommand.CanExecute(ItemsSource[itemIndex]))
                        SwipedRightCommand.Execute(ItemsSource[itemIndex]);
                }
                else
                {
                    Swiped?.Invoke(this, new SwipedEventArgs(ItemsSource[itemIndex], SwipeDirection.Left));
                    if (SwipedLeftCommand != null && SwipedLeftCommand.CanExecute(ItemsSource[itemIndex]))
                        SwipedLeftCommand.Execute(ItemsSource[itemIndex]);
                }

                // Next card
                itemIndex++;
                ShowNextCard();
            }
            else
            {
                // Run all animations from above simultaniously
                await Task.WhenAll(
                    // Move card back to the center
                    topCard.TranslateTo((-topCard.X), -topCard.Y, animationLength, Easing.SpringOut),
                    topCard.RotateTo(0, animationLength, Easing.SpringOut),

                    // Scale the back card down
                    backCard.ScaleTo(defaultSubcardScale, animationLength, Easing.SpringOut)
                );
            }

            if (itemIndex < ItemsSource.Count)
                FinishedDragging?.Invoke(this, new DraggingEventArgs(ItemsSource[itemIndex]));
        }

        private void ShowNextCard()
        {
            if (ItemsSource == null || ItemsSource?.Count == 0)
                return;

            var topCard = CardStack.Children[numberOfCards - 1];
            var backCard = CardStack.Children[numberOfCards - 2];

            // Switch cards if this method has been called after a swipe and not at init
            if (itemIndex != 0)
            {
                // Remove swiped-away card (topcard) from stack
                CardStack.Children.Remove(topCard);

                // Scale swiped-away card (topcard) down and add it at the bottom of the stack
                topCard.Scale = defaultSubcardScale;
                CardStack.Children.Insert(0, topCard);
            }

            // Update cards from top to back
            // Start with the first card on top which is the last one on the CardStack
            for (var i = numberOfCards - 1; i >= 0; i--)
            {
                var cardView = (CardView)CardStack.Children[i];
                cardView.Rotation = 0;
                cardView.TranslationX = 0;

                // Check if an item for the card is available
                var index = Math.Min((numberOfCards - 1), ItemsSource.Count) - i + itemIndex;
                if (ItemsSource.Count > index)
                {
                    cardView.Update(ItemsSource[index]);
                    cardView.IsVisible = true;
                }
            }
        }

        public async void Swipe(SwipeDirection direction, uint animationLength = 500)
        {
            var topCard = CardStack.Children[numberOfCards - 1];
            var backCard = CardStack.Children[numberOfCards - 2];


            double animationWidth = this.Width * 2;
            double rotation = 0.3f * 57.2957795f;
            if (direction == SwipeDirection.Left)
            {
                animationWidth *= -1;
                rotation *= -1;
            }

            // Run all animations from above simultaniously
            await Task.WhenAll(
                topCard.TranslateTo(animationWidth, 0, animationLength * 2, Easing.SpringOut),
                topCard.RotateTo(rotation, animationLength, Easing.SpringOut),
                backCard.ScaleTo(1.0f, animationLength)
            );
            topCard.IsVisible = false;

            Swiped?.Invoke(this, new SwipedEventArgs(ItemsSource[itemIndex], direction));
            if (direction == SwipeDirection.Left)
            {
                if (SwipedLeftCommand != null && SwipedLeftCommand.CanExecute(ItemsSource[itemIndex]))
                    SwipedLeftCommand.Execute(ItemsSource[itemIndex]);
            }
            else if (direction == SwipeDirection.Right)
            {
                if (SwipedRightCommand != null && SwipedRightCommand.CanExecute(ItemsSource[itemIndex]))
                    SwipedRightCommand.Execute(ItemsSource[itemIndex]);
            }

            // Next card
            itemIndex++;
            ShowNextCard();
        }
    }
}
