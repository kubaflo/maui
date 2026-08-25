namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 24987, "Shell TabBar is slow to open for the first time when combined with Grid and Border", PlatformAffected.Android)]
public partial class Issue24987 : Shell
{
	readonly List<Issue24987Page> _pages = [];
	readonly Dictionary<string, List<double>> _transitionMilliseconds = new()
	{
		["NewPage1"] = [],
		["NewPage2"] = []
	};

	long? _transitionStarted;

	public Issue24987()
	{
		InitializeComponent();
		RootTabBar.CurrentItem = MiddlePageTab;
	}

	internal string CurrentTabTitle =>
		CurrentItem?.CurrentItem?.Title ?? string.Empty;

	internal void RegisterPage(Issue24987Page page)
	{
		if (!_pages.Contains(page))
			_pages.Add(page);
	}

	internal void RecordTransitionStart()
	{
		_transitionStarted = System.Diagnostics.Stopwatch.GetTimestamp();
	}

	internal int RecordTransitionComplete(string destination)
	{
		if (_transitionStarted is not long started ||
			!_transitionMilliseconds.TryGetValue(destination, out List<double> measurements))
		{
			return 0;
		}

		_transitionStarted = null;
		measurements.Add(System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);

		if (_transitionMilliseconds["NewPage1"].Count < 2 ||
			_transitionMilliseconds["NewPage2"].Count < 2)
		{
			return measurements.Count;
		}

		long newPage1First = (long)Math.Ceiling(_transitionMilliseconds["NewPage1"][0]);
		long newPage2First = (long)Math.Ceiling(_transitionMilliseconds["NewPage2"][0]);
		long newPage1Repeat = (long)Math.Ceiling(_transitionMilliseconds["NewPage1"][1]);
		long newPage2Repeat = (long)Math.Ceiling(_transitionMilliseconds["NewPage2"][1]);
		string metrics = $"NewPage1First={newPage1First};NewPage2First={newPage2First};NewPage1Repeat={newPage1Repeat};NewPage2Repeat={newPage2Repeat}";

		foreach (Issue24987Page page in _pages)
			page.SetMeasurements(metrics);

		return measurements.Count;
	}
}

public class Issue24987Page : ContentPage
{
	static int s_nextInstanceId;

	readonly Label _pageTitleLabel;
	readonly Label _contentLabel;
	readonly Label _transitionLabel;
	readonly Label _metricsLabel;
	readonly int _instanceId = Interlocked.Increment(ref s_nextInstanceId);

	public Issue24987Page()
	{
		_pageTitleLabel = new Label
		{
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center
		};
		_contentLabel = new Label
		{
			AutomationId = "Issue24987Content",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};
		_transitionLabel = new Label
		{
			AutomationId = "Issue24987Transition",
			Text = "Transition pending"
		};
		_metricsLabel = new Label
		{
			AutomationId = "Issue24987Metrics",
			Text = "Measurements pending"
		};

		var resultLabel = new Label
		{
			Text = "First-open and repeat-open transition measurements will appear here."
		};
		var measurementLabels = new VerticalStackLayout
		{
			Children =
			{
				_transitionLabel,
				_metricsLabel,
				resultLabel
			}
		};
		var border = new Border
		{
			Content = new Grid
			{
				Children = { _contentLabel }
			}
		};
		var root = new Grid
		{
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star },
				new RowDefinition { Height = GridLength.Auto }
			},
			Children =
			{
				_pageTitleLabel
			}
		};

		root.Add(border, 0, 1);
		root.Add(measurementLabels, 0, 2);
		Content = root;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (Shell.Current is not Issue24987 shell)
			return;

		string pageName = shell.CurrentTabTitle;
		_pageTitleLabel.Text = pageName;
		_contentLabel.Text = $"{pageName} content";
		_transitionLabel.Text = $"{pageName} transition pending";
		shell.RegisterPage(this);

		Dispatcher.Dispatch(() =>
		{
			int transition = shell.RecordTransitionComplete(pageName);
			_transitionLabel.Text = transition == 0
				? $"{pageName} ready; instance {_instanceId}"
				: $"{pageName} transition {transition} measured; instance {_instanceId}";
		});
	}

	protected override void OnDisappearing()
	{
		if (Shell.Current is Issue24987 shell)
			shell.RecordTransitionStart();

		base.OnDisappearing();
	}

	internal void SetMeasurements(string metrics)
	{
		_metricsLabel.Text = metrics;
	}
}
