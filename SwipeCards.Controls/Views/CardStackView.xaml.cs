using System;
using System.Linq;
using Xamarin.Forms;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Specialized;
using System.Diagnostics;

namespace SwipeCards
{
    public partial class CardStackView : ContentView
    {
        private static NotifyCollectionChangedEventHandler CollectionChangedEventHandler;

        private const int NumberOfCards = 2;
        private const int DefaultAnimationLength = 250;
        private float _defaultSubcardScale = 0.9f;
        private float _defaultSubcardTranslationX = -30;
        private float _defaultSubcardOpacity = .6f;
        private float _cardDistance;
        private int _itemIndex;

        public static BindableProperty SwipedRightCommandProperty = BindableProperty.Create(nameof(SwipedRightCommand), typeof(ICommand), typeof(CardStackView), null);
        public static BindableProperty SwipedLeftCommandProperty = BindableProperty.Create(nameof(SwipedLeftCommand), typeof(ICommand), typeof(CardStackView), null);
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(CardStackView), null, BindingMode.TwoWay, propertyChanged: OnItemsSourcePropertyChanged);
        public static readonly BindableProperty CardMoveDistanceProperty = BindableProperty.Create(nameof(CardMoveDistance), typeof(int), typeof(CardStackView), -1);
        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(CardStackView));

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public int CardMoveDistance
        {
            get { return (int)GetValue(CardMoveDistanceProperty); }
            set { SetValue(CardMoveDistanceProperty, value); }
        }

        public ICommand SwipedRightCommand
        {
            get { return (ICommand)GetValue(SwipedRightCommandProperty); }
            set { SetValue(SwipedRightCommandProperty, value); }
        }

        public ICommand SwipedLeftCommand
        {
            get { return (ICommand)GetValue(SwipedLeftCommandProperty); }
            set { SetValue(SwipedLeftCommandProperty, value); }
        }

        public event EventHandler<SwipedEventArgs> Swiped;
        public event EventHandler<DraggingEventArgs> StartedDragging;
        public event EventHandler<DraggingEventArgs> Dragging;
        public event EventHandler<DraggingEventArgs> FinishedDragging;
        public event EventHandler<EventArgs> NoMoreCards;

        public CardStackView()
        {
            InitializeComponent();

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;

            CardStack.GestureRecognizers.Add(panGesture);

            MessagingCenter.Subscribe<object>(this, "UP", async (arg) =>
            {
                Debug.WriteLine($"FakeCompleted: {_cardDistance}");

                await HandleTouchCompleted();
            });

            /* put this in MainActivity for workaround to work -> https://github.com/xamarin/Xamarin.Forms/issues/1495

                public override bool DispatchTouchEvent(MotionEvent ev)
            {
            if (ev.Action == MotionEventActions.Up)
            {
                MessagingCenter.Send<object>(this, "UP");
            }

            return base.DispatchTouchEvent(ev);
        }

            */
        }

        public void Setup()
        {
            CardStack.Children.Clear();

            if (ItemsSource != null && ItemsSource.Count == 0)
                return;

            for (var i = NumberOfCards - 1; i >= 0; i--)
            {
                var cardView = new CardView(ItemTemplate)
                {
                    IsVisible = false,
                    Scale = (i == 0) ? 1 : _defaultSubcardScale,
                    TranslationX = (i == 0) ? 0 : _defaultSubcardTranslationX,
                    Opacity = (i == 0) ? 0 : _defaultSubcardOpacity
                };

                CardStack.Children.Add(cardView, Constraint.Constant(0), Constraint.Constant(0), Constraint.RelativeToParent((parent) => { return parent.Width; }),
                                                                                                 Constraint.RelativeToParent((parent) => { return parent.Height; })
                );
            }

            _itemIndex = 0;

            ShowNextCard();
        }

        private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue is INotifyCollectionChanged)
            {
                if (CollectionChangedEventHandler != null)
                    ((INotifyCollectionChanged)newValue).CollectionChanged -= CollectionChangedEventHandler;

                CollectionChangedEventHandler = (sender, e) => ItemsSource_CollectionChanged(sender, e, (CardStackView)bindable);

                ((INotifyCollectionChanged)newValue).CollectionChanged += CollectionChangedEventHandler;
            }

            ((CardStackView)bindable).Setup();
        }

        private static void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e, CardStackView cardStackView)
        {
            cardStackView.Setup();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (CardMoveDistance == -1 && !width.Equals(-1))
                CardMoveDistance = (int)(width / 3);
        }

        private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:

                    HandleTouchStart();

                    Debug.WriteLine($"Started: {_cardDistance}, x:{e.TotalX} y:{e.TotalY}");
                    break;

                case GestureStatus.Running:

                    HandleTouchRunning((float)e.TotalX);

                    Debug.WriteLine($"Running: {_cardDistance}, x:{e.TotalX} y:{e.TotalY}, -> _lastX: {_lastX}");
                    break;

                case GestureStatus.Completed:

                    await HandleTouchCompleted();

                    Debug.WriteLine($"Completed: {_cardDistance}, x:{e.TotalX} y:{e.TotalY}");
                    break;

                case GestureStatus.Canceled:
                    break;
            }
        }

        private bool _isDragging;
        private double _lastX;
        private const double DeltaX = 100;

        private void HandleTouchStart()
        {
            if (_itemIndex >= ItemsSource.Count)
                return;

            if (_cardDistance != 0)
                return;

            _lastX = 0;

            _isDragging = true;

            StartedDragging?.Invoke(this, new DraggingEventArgs(ItemsSource[_itemIndex], 0));
        }

        private void HandleTouchRunning(float horizontalTraslation)
        {
            if (_itemIndex >= ItemsSource.Count)
                return;

            if (!_isDragging)
                return;

            if (Math.Abs(horizontalTraslation - _lastX) > DeltaX)
                return;

            _lastX = horizontalTraslation;

            var topCard = CardStack.Children[NumberOfCards - 1];
            var backCard = CardStack.Children[NumberOfCards - 2];

            if (topCard.IsVisible)
            {
                topCard.TranslationX = horizontalTraslation;

                var rotationAngle = (float)(0.3f * Math.Min(horizontalTraslation / Width, 1.0f));
                topCard.Rotation = rotationAngle * 57.2957795f;

                _cardDistance = horizontalTraslation;

                Dragging?.Invoke(this, new DraggingEventArgs(ItemsSource[_itemIndex], _cardDistance));
            }

            backCard.Scale = Math.Min(_defaultSubcardScale + Math.Abs((_cardDistance / CardMoveDistance) * (1 - _defaultSubcardScale)), 1);
            backCard.TranslationX = Math.Min(_defaultSubcardTranslationX + Math.Abs((_cardDistance / CardMoveDistance) * _defaultSubcardTranslationX), 0);
            backCard.Opacity = Math.Min(_defaultSubcardOpacity + Math.Abs((_cardDistance / CardMoveDistance) * _defaultSubcardOpacity), 1);
        }

        private async Task HandleTouchCompleted()
        {
            if (_itemIndex >= ItemsSource.Count || !_isDragging)
                return;

            _lastX = 0;
            _isDragging = false;

            var topCard = CardStack.Children[NumberOfCards - 1];
            var backCard = CardStack.Children[NumberOfCards - 2];

            if (Math.Abs(_cardDistance) >= CardMoveDistance)
            {
                await topCard.TranslateTo(_cardDistance > 0 ? Width * 2 : -Width * 2, 0, DefaultAnimationLength, Easing.SinIn);

                topCard.IsVisible = false;

                if (_cardDistance > 0)
                {
                    Swiped?.Invoke(this, new SwipedEventArgs(ItemsSource[_itemIndex], SwipeDirection.Right));

                    if (SwipedRightCommand != null && SwipedRightCommand.CanExecute(ItemsSource[_itemIndex]))
                        SwipedRightCommand.Execute(ItemsSource[_itemIndex]);
                }
                else
                {
                    Swiped?.Invoke(this, new SwipedEventArgs(ItemsSource[_itemIndex], SwipeDirection.Left));

                    if (SwipedLeftCommand != null && SwipedLeftCommand.CanExecute(ItemsSource[_itemIndex]))
                        SwipedLeftCommand.Execute(ItemsSource[_itemIndex]);
                }


                _itemIndex++;

                ShowNextCard();
            }
            else
            {
                await Task.WhenAll(

                    topCard.TranslateTo((-topCard.X), -topCard.Y, DefaultAnimationLength, Easing.SpringOut),
                    topCard.RotateTo(0, DefaultAnimationLength, Easing.SpringOut),
                    backCard.ScaleTo(_defaultSubcardScale, DefaultAnimationLength, Easing.SpringOut),
                    backCard.TranslateTo(_defaultSubcardTranslationX, 0, DefaultAnimationLength, Easing.SpringOut),
                    backCard.FadeTo(_defaultSubcardOpacity, DefaultAnimationLength, Easing.SpringOut)
                );
            }

            _cardDistance = 0;

            if (_itemIndex < ItemsSource.Count)
                FinishedDragging?.Invoke(this, new DraggingEventArgs(ItemsSource[_itemIndex], _cardDistance));
        }

        public async Task Swipe(SwipeDirection direction)
        {
            if (_itemIndex >= ItemsSource.Count)
                return;

            _lastX = 0;
            _isDragging = false;

            var topCard = CardStack.Children[NumberOfCards - 1];
            var backCard = CardStack.Children[NumberOfCards - 2];

            await Task.WhenAll(
                    topCard.TranslateTo(direction == SwipeDirection.Right ? Width * 2 : -Width * 2, 0, DefaultAnimationLength, Easing.SinIn),
                    backCard.ScaleTo(1, DefaultAnimationLength, Easing.SpringOut),
                    backCard.TranslateTo(0, 0, DefaultAnimationLength, Easing.SpringOut),
                    backCard.FadeTo(1, DefaultAnimationLength, Easing.SpringOut)
                );

            topCard.IsVisible = false;

            _itemIndex++;

            ShowNextCard();

            _cardDistance = 0;
        }

        private void ShowNextCard()
        {
            if (ItemsSource == null || ItemsSource?.Count == 0)
                return;

            var topCard = CardStack.Children[NumberOfCards - 1];

            if (_itemIndex != 0)
            {
                CardStack.Children.Remove(topCard);

                topCard.Scale = _defaultSubcardScale;
                CardStack.Children.Insert(0, topCard);
            }

            for (var i = NumberOfCards - 1; i >= 0; i--)
            {
                var cardView = (CardView)CardStack.Children[i];

                cardView.Rotation = 0;
                cardView.TranslationX = _defaultSubcardTranslationX * (NumberOfCards - 1 - i);
                cardView.Opacity = i == NumberOfCards - 1 ? 1 : _defaultSubcardOpacity;

                var index = Math.Min((NumberOfCards - 1), ItemsSource.Count) - i + _itemIndex;

                if (ItemsSource.Count > index)
                {
                    cardView.Update(ItemsSource[index]);

                    if (!cardView.IsVisible)
                    {
                        cardView.TranslationX = -cardView.Width + _defaultSubcardTranslationX;
                        cardView.IsVisible = true;

                        cardView.TranslateTo(_defaultSubcardTranslationX * (NumberOfCards - 1 - i), 0, DefaultAnimationLength, Easing.Linear);
                    }
                }
            }
        }
    }
}
