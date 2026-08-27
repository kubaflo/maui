using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.WinRT)]
public class Issue32587 : ContentPage
{
	public Issue32587()
	{
		var referenceStatus = CreateStatusLabel("Issue32587ReferenceStatus");
		var directStatus = CreateStatusLabel("Issue32587DirectStatus");

		var referenceTarget = new Issue32587BoundsContentView(
			"Issue32587ReferenceTarget",
			"Issue32587ReferenceLabel",
			(count, width, height) => UpdateStatus(referenceStatus, count, width, height));

		var referenceWrapper = new Grid();
		referenceWrapper.Add(referenceTarget);

		var collectionView = new CollectionView
		{
			ItemsSource = new[] { "Item" },
			ItemTemplate = new DataTemplate(() => new Issue32587BoundsContentView(
				"Issue32587DirectTarget",
				"Issue32587DirectLabel",
				(count, width, height) => UpdateStatus(directStatus, count, width, height)))
		};

		var rootGrid = new Grid
		{
			Padding = 24,
			RowDefinitions = new RowDefinitionCollection(
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)),
			RowSpacing = 12
		};

		rootGrid.Add(new Label
		{
			FontSize = 18,
			Text = "Issue 32587: tap the CollectionView item"
		});
		rootGrid.Add(referenceWrapper, 0, 1);
		rootGrid.Add(referenceStatus, 0, 2);
		rootGrid.Add(directStatus, 0, 3);
		rootGrid.Add(collectionView, 0, 4);

		Content = rootGrid;
	}

	static Label CreateStatusLabel(string automationId) =>
		new()
		{
			AutomationId = automationId,
			Text = "Callbacks=0; Width=NaN; Height=NaN"
		};

	static void UpdateStatus(Label statusLabel, int count, double width, double height)
	{
		statusLabel.Text = string.Format(
			CultureInfo.InvariantCulture,
			"Callbacks={0}; Width={1}; Height={2}",
			count,
			width,
			height);
	}
}

public class Issue32587BoundsContentView : ContentView
{
	readonly Action<int, double, double> _boundsCaptured;
	int _tapCount;

	public Issue32587BoundsContentView(
		string automationId,
		string labelAutomationId,
		Action<int, double, double> boundsCaptured)
	{
		AutomationId = automationId;
		_boundsCaptured = boundsCaptured;
		Content = new Label
		{
			AutomationId = labelAutomationId,
			Text = "Tap this custom ContentView item",
			FontSize = 22,
			Padding = new Thickness(20)
		};

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += OnTapped;
		GestureRecognizers.Add(tapGesture);
	}

	void OnTapped(object sender, TappedEventArgs e)
	{
		_tapCount++;
		_boundsCaptured(_tapCount, Width, Height);
	}
}

