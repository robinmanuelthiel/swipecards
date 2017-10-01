using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xamarin.Forms.Internals;

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
            else
            {
                // If ItemsSource is nnot INotifyCollectionChanged, we need to 
                // call Setup() whenever the whole collection changes
                ((CardStackView)bindable).Setup();
            }
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
                null,
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

        public static readonly BindableProperty CardMoveDistanceProperty = BindableProperty.Create(nameof(CardMoveDistance), typeof(int), typeof(CardStackView), 0);

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

        #endregion

        public Action<object> SwipedRight = null;
        public Action<object> SwipedLeft = null;
        public Action<object> StartedDragging = null;
        public Action<object> FinishedDragging = null;

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
                var cardView = new CardView(ItemTemplate);
                cardView.IsVisible = false;
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


        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // Recalculate move distance
            // TODO: Check if CardMoveDistance hasn't been set to an individual valu by the developer
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
                    await HandleTouchCompleted();
                    break;
            }
        }

        private void HandleTouchStart()
        {
            if (itemIndex < ItemsSource.Count)
                StartedDragging?.Invoke(ItemsSource[itemIndex]);
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
                // Move card back to the center
                var traslateAnmimation = topCard.TranslateTo((-topCard.X), -topCard.Y, animationLength, Easing.SpringOut);
                var rotateAnimation = topCard.RotateTo(0, animationLength, Easing.SpringOut);

                // Scale the back card down
                var scaleAnimation = backCard.ScaleTo(defaultSubcardScale, animationLength, Easing.SpringOut);

                // Run all animations from above simultaneiously
                await Task.WhenAll(new List<Task> { traslateAnmimation, rotateAnimation, scaleAnimation });
            }

            if (itemIndex < ItemsSource.Count)
                FinishedDragging?.Invoke(ItemsSource[itemIndex]);
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

            // Update cards
            for (int i = 0; i < Math.Min(numberOfCards, ItemsSource.Count); i++)
            {
                var cardView = (CardView)CardStack.Children[i];
                cardView.Rotation = 0;
                cardView.TranslationX = 0;

                if ((itemIndex + 1) - i < ItemsSource.Count)
                {
                    cardView.Update(ItemsSource[(itemIndex + 1) - i]);
                    cardView.IsVisible = true;
                }
            }
        }
    }
}
