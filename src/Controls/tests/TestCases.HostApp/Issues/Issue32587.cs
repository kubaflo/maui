namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.UWP)]
public class Issue32587 : ContentPage
{
	public Issue32587()
	{
		var gestureStatus = new Label
		{
			AutomationId = "GestureStatus",
			Text = "NOT_TAPPED"
		};

		var resultStatus = new Label
		{
			AutomationId = "ResultStatus",
			Text = "WAITING"
		};

		var itemsView = new CollectionView
		{
			AutomationId = "ItemsView",
			HeightRequest = 160,
			SelectionMode = SelectionMode.None,
			ItemTemplate = new DataTemplate(() => new Issue32587BoundsContentView((width, height) =>
			{
				gestureStatus.Text = FormattableString.Invariant($"TAPPED: Width={width:R}, Height={height:R}");
				resultStatus.Text = "TAP_COMPLETED";
			})),
			ItemsSource = new[] { "item" }
		};

		var layout = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 16
		};

		layout.Add(new Label
		{
			Text = "Issue 32587: tap the direct CollectionView item",
			FontSize = 20
		});
		layout.Add(itemsView, 0, 1);
		layout.Add(gestureStatus, 0, 2);
		layout.Add(resultStatus, 0, 3);

		Content = layout;
	}
}

sealed class Issue32587BoundsContentView : ContentView
{
	readonly Action<double, double> _reportTappedBounds;

	public Issue32587BoundsContentView(Action<double, double> reportTappedBounds)
	{
		_reportTappedBounds = reportTappedBounds;

		Content = new Label
		{
			AutomationId = "BoundsItemText",
			Text = "Tap the direct ContentView item"
		};

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += OnTapped;
		GestureRecognizers.Add(tapGesture);
	}

	void OnTapped(object sender, TappedEventArgs e)
	{
		_reportTappedBounds(Width, Height);
	}
}

