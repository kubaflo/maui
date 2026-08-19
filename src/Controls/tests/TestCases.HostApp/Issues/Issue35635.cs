#if ANDROID
using System.Diagnostics;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35635, "MapElements.Add repeatedly re-syncs the full element collection", PlatformAffected.Android)]
public class Issue35635 : ContentPage
{
	const int ElementCount = 1000;

	readonly Map _testMap;
	readonly Button _runButton;
	readonly Label _readyLabel;
	readonly Label _referenceLabel;
	readonly Label _timingLabel;
	readonly Label _statusLabel;
	long _detachedDurationMilliseconds;
	Stopwatch _liveStopwatch;

	public Issue35635()
	{
		_readyLabel = new Label
		{
			AutomationId = "ReadyLabel",
			Text = "Ready: waiting for attached map"
		};
		_referenceLabel = new Label
		{
			AutomationId = "ReferenceLabel",
			Text = "Reference: waiting for detached population"
		};
		_runButton = new Button
		{
			AutomationId = "RunButton",
			IsEnabled = false,
			Text = "Run live burst add"
		};
		_timingLabel = new Label
		{
			AutomationId = "TimingLabel",
			Text = "Live burst: not started"
		};
		_statusLabel = new Label
		{
			AutomationId = "StatusLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "NO BUG:"
		};

		_runButton.Clicked += OnRunClicked;

		var actionLayout = new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				_runButton,
				_timingLabel,
				_statusLabel
			}
		};

		_testMap = new Map();
		_testMap.Loaded += OnMapLoaded;

		var mapHost = new Grid
		{
			MinimumHeightRequest = 240,
			Children = { _testMap }
		};

		var layout = new Grid
		{
			Padding = 16,
			RowSpacing = 10,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		layout.Add(new Label
		{
			FontAttributes = FontAttributes.Bold,
			Text = "MapElements live-add performance"
		}, 0, 0);
		layout.Add(_readyLabel, 0, 1);
		layout.Add(_referenceLabel, 0, 2);
		layout.Add(actionLayout, 0, 3);
		layout.Add(mapHost, 0, 4);

		Content = layout;
	}

	void OnMapLoaded(object sender, EventArgs e)
	{
		_testMap.Loaded -= OnMapLoaded;
		Dispatcher.Dispatch(WaitForNativeMap);
	}

	void WaitForNativeMap()
	{
		if (_testMap.Handler is not MapHandler { Map: not null } ||
			_testMap.VisibleRegion is null ||
			_testMap.MapElements.Count != 0)
		{
			Dispatcher.Dispatch(WaitForNativeMap);
			return;
		}

		var detachedMap = new Map();
		var stopwatch = Stopwatch.StartNew();

		for (var index = 0; index < ElementCount; index++)
			detachedMap.MapElements.Add(CreateCircle(index));

		stopwatch.Stop();
		_detachedDurationMilliseconds = Math.Max(1, stopwatch.ElapsedMilliseconds);
		_referenceLabel.Text = $"Reference: {detachedMap.MapElements.Count} detached circles populated";
		_timingLabel.Text = $"Detached population: {_detachedDurationMilliseconds} ms; live: not started";
		_readyLabel.Text = "Ready: attached map empty";
		_runButton.IsEnabled = true;
	}

	void OnRunClicked(object sender, EventArgs e)
	{
		_runButton.IsEnabled = false;
		_liveStopwatch = Stopwatch.StartNew();

		for (var index = 0; index < ElementCount; index++)
			_testMap.MapElements.Add(CreateCircle(index));

		Dispatcher.Dispatch(PublishLiveResult);
	}

	void PublishLiveResult()
	{
		_liveStopwatch.Stop();
		_timingLabel.Text = $"Detached: {_detachedDurationMilliseconds} ms; live: {_liveStopwatch.ElapsedMilliseconds} ms";
		_statusLabel.Text = $"Completed: {_testMap.MapElements.Count} circles";
	}

	static Circle CreateCircle(int index)
	{
		var row = index / 40;
		var column = index % 40;

		return new Circle
		{
			Center = new Location(
				20.793062527 + (row * 0.0002),
				-156.336394697 + (column * 0.0002)),
			Radius = Distance.FromMeters(10)
		};
	}
}
#endif
