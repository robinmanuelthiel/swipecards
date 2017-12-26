# Swipecards
A Tinder control for Xamarin.Forms that supports iOS, Android and UWP.

[![NuGet](https://img.shields.io/nuget/v/Forms.Controls.SwipeCards.svg?label=NuGet&style=flat-square)](https://www.nuget.org/packages/Forms.Controls.SwipeCards/)

## How to use
**Add the [NuGet package](https://www.nuget.org/packages/Forms.Controls.SwipeCards/) to the Xamarin.Forms project**
```
PM> Install-Package Forms.Controls.SwipeCards
```

**Add the XML namespace**
```xml
xmlns:swipecards="clr-namespace:SwipeCards.Controls;assembly=SwipeCards.Controls"
```

**Add the control**
```xml
<swipecards:CardStackView
    x:Name="CardStackView"
    ItemsSource="{Binding Cards}">
    
    <swipecards:CardStackView.ItemTemplate>
        <DataTemplate>
            <Label Text="{Binding}" VerticalOptions="Center" HorizontalOptions="Center" />
        </DataTemplate>
    </swipecards:CardStackView.ItemTemplate>   
</swipecards:CardStackView>
```
## Preview
Take a look a the [Demo Project](/SwipeCards.Demo.Forms) in this repository for a full sample.

![Preview](/Design/Swipecards.gif)

## API Reference
| Method | Description |
|-|-|
| Reset() | Resets the whole card stack |

| Property | Default | Description |
|-|-|-|
| CardMoveDistance | null | How far the card has to be dragged to trigger the swipe. Default is 30% of the control |

| Command | Parameter | Description |
|-|-|-|
| SwipedLeftCommand | Selected Item | Triggered, when card got swiped to the left |
| SwipedRightCommand | Selected Item | Triggered, when card got swiped to the right |

| Event | Arguments | Description |
|-|-|-|
| Swiped | Swiped Item, Swipe direction | Triggered, when card got swiped to the left or right |
| StartedDragging | Dragged Item | Triggered, when card got dragged |
| FinishedDragging | Dragged Item | Triggered, when dragging finished |

