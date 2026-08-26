namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29920, "Android tap event passes through containers", PlatformAffected.Android)]
public class Issue29920 : ContentPage
{
	const string ReadyStatus = "Underlying taps: 0";
	int _underlyingTapCount = -1;

	public Issue29920()
	{
		var statusLabel = new Label
		{
			AutomationId = "Issue29920TapCount",
			Text = "Underlying taps: -1",
			BackgroundColor = Colors.White,
			TextColor = Colors.Black,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.End,
			Margin = new Thickness(24)
		};

		var underlyingLayout = new StackLayout
		{
			BackgroundColor = Colors.Red,
			Opacity = 0.1,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill
		};
		underlyingLayout.Children.Add(new Label
		{
			Text = "Underlying StackLayout",
			HorizontalOptions = LayoutOptions.Center
		});

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (_, _) =>
		{
			_underlyingTapCount++;
			statusLabel.Text = $"Underlying taps: {_underlyingTapCount}";
		};
		underlyingLayout.GestureRecognizers.Add(tapGestureRecognizer);

		var overlaidBoxView = new BoxView
		{
			AutomationId = "Issue29920OverlayBoxView",
			Color = Colors.Blue,
			Opacity = 0.1,
			WidthRequest = 240,
			HeightRequest = 240,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		var rootGrid = new Grid();
		rootGrid.Children.Add(underlyingLayout);
		rootGrid.Children.Add(overlaidBoxView);
		rootGrid.Children.Add(statusLabel);
		Content = rootGrid;

		Loaded += (_, _) =>
		{
			_underlyingTapCount = 0;
			statusLabel.Text = ReadyStatus;
		};
	}
}

