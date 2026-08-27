namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29920, "Android tap events pass through covering containers", PlatformAffected.Android)]
public class Issue29920 : ContentPage
{
	public Issue29920()
	{
		var resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = "Not tapped",
			FontSize = 18,
			BackgroundColor = Colors.White,
			TextColor = Colors.Black,
			Padding = 12,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.End,
			Margin = new Thickness(0, 0, 0, 16)
		};

		var bottomLayer = new StackLayout
		{
			AutomationId = "BottomLayer",
			BackgroundColor = Colors.Red,
			Opacity = 0.1
		};
		bottomLayer.Children.Add(new BoxView
		{
			AutomationId = "BottomTapTarget",
			Color = Colors.Red,
			WidthRequest = 160,
			HeightRequest = 100,
			HorizontalOptions = LayoutOptions.Start,
			Margin = new Thickness(24, 80, 0, 0)
		});
		var bottomTapGesture = new TapGestureRecognizer();
		bottomTapGesture.Tapped += (_, _) => resultLabel.Text = "Bottom tapped";
		bottomLayer.GestureRecognizers.Add(bottomTapGesture);

		var middleLayer = new StackLayout
		{
			AutomationId = "MiddleLayer",
			BackgroundColor = Colors.Green,
			Opacity = 0.1
		};

		var topLayer = new BoxView
		{
			AutomationId = "TopLayer",
			Color = Colors.Blue,
			Opacity = 0.1
		};

		var rootGrid = new Grid
		{
			AutomationId = "LayeredRoot"
		};
		rootGrid.Add(bottomLayer);
		rootGrid.Add(middleLayer);
		rootGrid.Add(topLayer);
		rootGrid.Add(resultLabel);

		Content = rootGrid;
	}
}

