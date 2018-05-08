using System;
using Xamarin.Forms;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Specialized;

namespace SwipeCards
{
	public enum SwipeMode
	{
		Tinder,
		Carousel
	}

	public partial class CardStackView : ContentView
	{
		private static NotifyCollectionChangedEventHandler CollectionChangedEventHandler;

		private const int NumberOfCards = 2;
		private const int DefaultAnimationLength = 250;
		private float _defaultSubcardScale = 0.8f;
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

		public SwipeMode SwipeMode { get; set; } = SwipeMode.Tinder;

		public event EventHandler<SwipedEventArgs> Swiped;
		public event EventHandler<DraggingEventArgs> StartedDragging;
		public event EventHandler<DraggingEventArgs> Dragging;
		public event EventHandler<DraggingEventArgs> FinishedDragging;

		public CardStackView()
		{
			InitializeComponent();

			var panGesture = new PanGestureRecognizer();
			panGesture.PanUpdated += OnPanUpdated;

			CardStack.GestureRecognizers.Add(panGesture);
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
					Scale = (i == 0) ? 1.0f : _defaultSubcardScale
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

			// Recalculate move distance. When not set differently, this distance is 1/3 of the control's width
			if (CardMoveDistance == -1 && !width.Equals(-1))
				CardMoveDistance = (int)(width / 3);
		}

		private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
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
				case GestureStatus.Canceled:
					break;
			}
		}

		private void HandleTouchStart()
		{
			if (_itemIndex < ItemsSource.Count)
				StartedDragging?.Invoke(this, new DraggingEventArgs(ItemsSource[_itemIndex], 0));
		}

		private void HandleTouchRunning(float horizontalTraslation)
		{
			if (_itemIndex >= ItemsSource.Count)
				return;

			var topCard = CardStack.Children[NumberOfCards - 1];
			var backCard = CardStack.Children[NumberOfCards - 2];

			if (topCard.IsVisible)
			{
				topCard.TranslationX = (horizontalTraslation);

				if (SwipeMode == SwipeMode.Tinder)
				{
					var rotationAngel = (float)(0.3f * Math.Min(horizontalTraslation / Width, 1.0f));
					topCard.Rotation = rotationAngel * 57.2957795f;
				}

				_cardDistance = horizontalTraslation;

				Dragging?.Invoke(this, new DraggingEventArgs(ItemsSource[_itemIndex], _cardDistance));
			}

			backCard.Scale = Math.Min(_defaultSubcardScale + Math.Abs((_cardDistance / CardMoveDistance) * (1.0f - _defaultSubcardScale)), 1.0f);
		}

		private async Task HandleTouchCompleted()
		{
			if (_itemIndex >= ItemsSource.Count)
				return;

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
					backCard.ScaleTo(_defaultSubcardScale, DefaultAnimationLength, Easing.SpringOut)
				);
			}

			if (_itemIndex < ItemsSource.Count)
				FinishedDragging?.Invoke(this, new DraggingEventArgs(ItemsSource[_itemIndex], _cardDistance));
		}

		private void ShowNextCard()
		{
			if (ItemsSource == null || ItemsSource?.Count == 0)
				return;

			var topCard = CardStack.Children[NumberOfCards - 1];
			var backCard = CardStack.Children[NumberOfCards - 2];

			// Switch cards if this method has been called after a swipe and not at init
			if (_itemIndex != 0)
			{
				CardStack.Children.Remove(topCard);

				// Scale swiped-away card (topcard) down and add it at the bottom of the stack
				topCard.Scale = _defaultSubcardScale;
				CardStack.Children.Insert(0, topCard);
			}

			// Update cards from top to back  // Start with the first card on top which is the last one on the CardStack
			for (var i = NumberOfCards - 1; i >= 0; i--)
			{
				var cardView = (CardView)CardStack.Children[i];

				cardView.Rotation = 0;
				cardView.TranslationX = 0;

				// Check if an item for the card is available
				var index = Math.Min((NumberOfCards - 1), ItemsSource.Count) - i + _itemIndex;

				if (ItemsSource.Count > index)
				{
					cardView.Update(ItemsSource[index]);
					cardView.IsVisible = true;
				}
			}
		}

		public async Task Swipe(SwipeDirection direction, uint animationLength = DefaultAnimationLength)
		{
			if (_itemIndex >= ItemsSource?.Count)
				return;

			var topCard = CardStack.Children[NumberOfCards - 1];
			var backCard = CardStack.Children[NumberOfCards - 2];

			Swiped?.Invoke(this, new SwipedEventArgs(ItemsSource[_itemIndex], direction));

			if (direction == SwipeDirection.Left)
			{
				if (SwipedLeftCommand != null && SwipedLeftCommand.CanExecute(ItemsSource[_itemIndex]))
					SwipedLeftCommand.Execute(ItemsSource[_itemIndex]);
			}
			else if (direction == SwipeDirection.Right)
			{
				if (SwipedRightCommand != null && SwipedRightCommand.CanExecute(ItemsSource[_itemIndex]))
					SwipedRightCommand.Execute(ItemsSource[_itemIndex]);
			}

			// Increase item index // Do that before the animation runs
			_itemIndex++;

			await Task.WhenAll(

				topCard.TranslateTo(direction == SwipeDirection.Right ? Width * 2 : -Width * 2, 0, animationLength, Easing.SinIn),
				topCard.RotateTo(direction == SwipeDirection.Right ? 17.18873385f : -17.18873385f, animationLength, Easing.SinIn),
				backCard.ScaleTo(1.0f, animationLength)
			);

			topCard.IsVisible = false;

			ShowNextCard();
		}
	}
}
