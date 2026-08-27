#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29920, "Android tap event passes through overlapping containers", PlatformAffected.Android)]
public class Issue29920 : ContentPage
{
	public Issue29920()
	{
		var topTapCount = -1;
		var bottomTapCount = -1;
		var metricsLabel = new Label
		{
			AutomationId = "Issue29920Metrics",
			Text = "Not ready",
			InputTransparent = true,
			BackgroundColor = Colors.White,
			TextColor = Colors.Black,
			Padding = 12,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Start,
			Margin = 24
		};

		void UpdateMetrics() =>
			metricsLabel.Text = $"Top taps: {topTapCount}; Bottom taps: {bottomTapCount}";

		var bottomBox = new BoxView
		{
			AutomationId = "Issue29920BottomBox",
			Color = Colors.Red,
			HeightRequest = 80
		};
		var bottomTapGesture = new TapGestureRecognizer();
		bottomTapGesture.Tapped += (_, _) =>
		{
			bottomTapCount++;
			UpdateMetrics();
		};
		bottomBox.GestureRecognizers.Add(bottomTapGesture);

		var middleBox = new BoxView
		{
			AutomationId = "Issue29920MiddleBox",
			Color = Colors.Green,
			HeightRequest = 80
		};

		var topBox = new BoxView
		{
			AutomationId = "Issue29920TopBox",
			Color = Colors.Blue,
			HeightRequest = 80
		};
		var topTapGesture = new TapGestureRecognizer();
		topTapGesture.Tapped += (_, _) =>
		{
			topTapCount++;
			UpdateMetrics();
		};
		topBox.GestureRecognizers.Add(topTapGesture);

		Content = new Grid
		{
			Children =
			{
				new StackLayout
				{
					BackgroundColor = Colors.Red,
					Opacity = 0.1,
					Padding = new Thickness(24, 160, 24, 0),
					Children = { bottomBox }
				},
				new StackLayout
				{
					BackgroundColor = Colors.Green,
					Opacity = 0.1,
					Padding = new Thickness(24, 300, 24, 0),
					Children = { middleBox }
				},
				new StackLayout
				{
					BackgroundColor = Colors.Blue,
					Opacity = 0.1,
					Padding = new Thickness(24, 440, 24, 0),
					Children = { topBox }
				},
				metricsLabel
			}
		};

		Loaded += (_, _) =>
		{
			topTapCount = 0;
			bottomTapCount = 0;
			UpdateMetrics();
		};
	}
}
#endif

