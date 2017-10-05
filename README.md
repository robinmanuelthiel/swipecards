> **Work in progress - not ready for productive use!**

![Logo](/Design/Logo-150.png)

# Swipecards - A Tinder control for Xamarin.Forms

## How to use
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
