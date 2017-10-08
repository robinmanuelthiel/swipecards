# Swipecards
**A Tinder control for Xamarin.Forms**

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
![Preview](/Design/Swipecards.gif)

## API Reference
