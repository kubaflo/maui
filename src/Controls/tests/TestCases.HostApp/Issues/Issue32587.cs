#if WINDOWS
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.UWP)]
public class Issue32587 : ContentPage
{
	readonly Label _renderStatusLabel;
	readonly Label _interactionStatusLabel;
	readonly Label _tappedBoundsLabel;

	public Issue32587()
	{
		_renderStatusLabel = new Label
		{
			AutomationId = "RenderStatusLabel",
			Text = "WAITING: direct item has not loaded"
		};
		_interactionStatusLabel = new Label
		{
			AutomationId = "InteractionStatusLabel",
			Text = "WAITING: tap not received"
		};
		_tappedBoundsLabel = new Label
		{
			AutomationId = "TappedBoundsLabel",
			Text = "TAPPED BOUNDS: unavailable"
		};

		var itemsCollection = new CollectionView
		{
			ItemsSource = new[] { "Direct template item" },
			ItemTemplate = new DataTemplate(() => new Issue32587GestureBoundsContentView(OnItemLoaded, OnItemTapped))
		};

		var grid = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 12
		};

		grid.Add(new Label { Text = "Issue 32587: tap the direct ContentView item" });
		grid.Add(_renderStatusLabel, 0, 1);
		grid.Add(new Label { Text = "Expected bounds: Width > 0; Height > 0" }, 0, 2);
		grid.Add(_interactionStatusLabel, 0, 3);
		grid.Add(_tappedBoundsLabel, 0, 4);
		grid.Add(itemsCollection, 0, 5);
		Content = grid;
	}

	void OnItemLoaded()
	{
		_renderStatusLabel.Text = "READY: direct item loaded";
	}

	void OnItemTapped(double width, double height)
	{
		_interactionStatusLabel.Text = "TAP RECEIVED:";
		_tappedBoundsLabel.Text = $"TAPPED BOUNDS: Width={width.ToString(CultureInfo.InvariantCulture)}; Height={height.ToString(CultureInfo.InvariantCulture)}";
	}
}

sealed class Issue32587GestureBoundsContentView : ContentView
{
	bool _reportedLoaded;

	public Issue32587GestureBoundsContentView(Action loaded, Action<double, double> tapped)
	{
		AutomationId = "DirectGestureItem";

		var contentLabel = new Label
		{
			Text = "Tap this direct ContentView item"
		};
		Content = contentLabel;

		Loaded += (_, _) =>
		{
			if (_reportedLoaded)
				return;

			_reportedLoaded = true;
			loaded();
		};

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += (_, _) =>
		{
			var width = Width;
			var height = Height;
			contentLabel.Text = $"Tapped bounds: Width={width.ToString(CultureInfo.InvariantCulture)}; Height={height.ToString(CultureInfo.InvariantCulture)}";
			tapped(width, height);
		};
		GestureRecognizers.Add(tapGesture);
	}
}
#endif

