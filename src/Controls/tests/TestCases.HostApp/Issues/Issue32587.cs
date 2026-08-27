using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.UWP)]
public class Issue32587 : ContentPage
{
	public Issue32587()
	{
		var renderedStateLabel = new Label
		{
			AutomationId = "RenderedStateLabel",
			Text = "Waiting for rendered bounds"
		};
		var gestureStateLabel = new Label
		{
			AutomationId = "GestureStateLabel",
			Text = "Gesture received: 0"
		};

		var itemsCollection = new CollectionView
		{
			AutomationId = "ItemsCollection",
			SelectionMode = SelectionMode.None,
			ItemsSource = new[] { "Direct ContentView item" },
			ItemTemplate = new DataTemplate(() => new Issue32587ItemView(
				() => renderedStateLabel.Text = "ContentView is loaded and visible",
				() => gestureStateLabel.Text = "Gesture received: 1"))
		};

		var collectionArea = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 12
		};
		collectionArea.Add(itemsCollection);
		collectionArea.Add(new Label
		{
			Text = "Tap the item to read its gesture-time bounds"
		}, 0, 1);

		var pageLayout = new Grid
		{
			Padding = 24,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			RowSpacing = 12
		};
		pageLayout.Add(new Label
		{
			Text = "CollectionView direct ContentView bounds",
			FontSize = 24
		});
		pageLayout.Add(new Label
		{
			Text = "Tap the item after it reports that it is loaded and visible."
		}, 0, 1);
		pageLayout.Add(renderedStateLabel, 0, 2);
		pageLayout.Add(gestureStateLabel, 0, 3);
		pageLayout.Add(collectionArea, 0, 4);

		Content = pageLayout;
	}
}

public class Issue32587ItemView : ContentView
{
	public Issue32587ItemView(Action itemReady, Action gestureTapped)
	{
		var renderedBoundsLabel = new Label
		{
			Text = "ContentView is loading"
		};
		var tappedBoundsLabel = new Label
		{
			AutomationId = "TappedBoundsLabel",
			Text = "Tapped bounds: not measured"
		};

		Content = new VerticalStackLayout
		{
			Spacing = 4,
			Children =
			{
				new Label { Text = "Tap the direct ContentView item" },
				renderedBoundsLabel,
				tappedBoundsLabel
			}
		};

		AutomationId = "GestureItem";
		Loaded += (_, _) => Dispatcher.Dispatch(() =>
		{
			renderedBoundsLabel.Text = "ContentView is loaded and visible";
			itemReady();
		});

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += (_, _) =>
		{
			tappedBoundsLabel.Text = $"Tapped bounds: Width={Width.ToString(CultureInfo.InvariantCulture)}, Height={Height.ToString(CultureInfo.InvariantCulture)}";
			gestureTapped();
		};
		GestureRecognizers.Add(tapGesture);
	}
}

