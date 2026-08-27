#if WINDOWS
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32587, "ContentView inside CollectionView reports invalid bounds during gesture events", PlatformAffected.UWP)]
public class Issue32587 : ContentPage
{
	public Issue32587()
	{
		Content = new CollectionView
		{
			ItemsSource = new[] { "Only item" },
			ItemTemplate = new DataTemplate(() => new Issue32587BoundsProbeView())
		};
	}
}

public class Issue32587BoundsProbeView : ContentView
{
	readonly Label _loadedStateLabel;
	readonly Label _tapCountLabel;
	readonly Label _measurementStateLabel;
	readonly Label _tapWidthLabel;
	readonly Label _tapHeightLabel;
	int _tapCount;

	public Issue32587BoundsProbeView()
	{
		AutomationId = "Issue32587BoundsProbe";

		_loadedStateLabel = new Label
		{
			AutomationId = "Issue32587LoadedState",
			Text = "Waiting"
		};
		_tapCountLabel = new Label
		{
			AutomationId = "Issue32587TapCount",
			Text = "0"
		};
		_measurementStateLabel = new Label
		{
			AutomationId = "Issue32587MeasurementState",
			Text = "Not measured"
		};
		_tapWidthLabel = new Label
		{
			AutomationId = "Issue32587TapWidth",
			Text = "Not measured"
		};
		_tapHeightLabel = new Label
		{
			AutomationId = "Issue32587TapHeight",
			Text = "Not measured"
		};

		Content = new VerticalStackLayout
		{
			Children =
			{
				new Label { Text = "Tap this directly templated ContentView" },
				_loadedStateLabel,
				_tapCountLabel,
				_measurementStateLabel,
				_tapWidthLabel,
				_tapHeightLabel
			}
		};

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += OnTapped;
		GestureRecognizers.Add(tapGesture);
		Loaded += OnLoaded;
	}

	void OnLoaded(object sender, EventArgs e)
	{
		_loadedStateLabel.Text = "Loaded";
	}

	void OnTapped(object sender, TappedEventArgs e)
	{
		_tapCount++;
		_tapCountLabel.Text = _tapCount.ToString(CultureInfo.InvariantCulture);
		_tapWidthLabel.Text = Width.ToString("R", CultureInfo.InvariantCulture);
		_tapHeightLabel.Text = Height.ToString("R", CultureInfo.InvariantCulture);
		_measurementStateLabel.Text = "Dimensions captured";
	}
}
#endif

