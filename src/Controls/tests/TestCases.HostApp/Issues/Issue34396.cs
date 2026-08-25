using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34396, "UI becomes unresponsive when adding more than 200 Entry children to AbsoluteLayout", PlatformAffected.Android)]
public class Issue34396 : ContentPage
{
	const int EntryCount = 201;

	readonly AbsoluteLayout _entryCanvas;
	readonly Label _metricsLabel;
	double _baselineMilliseconds = -1;
	double _postAddMilliseconds = -1;
	int _callbackToken = -1;

	public Issue34396()
	{
		var addEditorsButton = new Button
		{
			Text = "Add 201 Editors",
			AutomationId = "AddEditorsButton"
		};
		addEditorsButton.Clicked += OnAddEditorsClicked;

		var clickedButton = new Button
		{
			Text = "Clicked 0"
		};
		var clickCount = 1;
		clickedButton.Clicked += (_, _) =>
		{
			clickedButton.Text = $"Clicked {clickCount}";
			clickCount++;
		};

		_entryCanvas = new AbsoluteLayout
		{
			WidthRequest = 2000,
			HeightRequest = 3000,
			BackgroundColor = Color.FromArgb("#202020")
		};

		_metricsLabel = new Label
		{
			AutomationId = "TimingMetrics"
		};
		UpdateMetrics();

		var root = new Grid
		{
			Padding = new Thickness(12),
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star }
			}
		};

		var toolbar = new HorizontalStackLayout
		{
			Spacing = 8,
			Children =
			{
				addEditorsButton,
				clickedButton
			}
		};

		root.Add(toolbar);
		Grid.SetRow(_metricsLabel, 1);
		root.Add(_metricsLabel);

		var scroller = new ScrollView
		{
			Content = _entryCanvas
		};
		Grid.SetRow(scroller, 2);
		root.Add(scroller);

		Content = root;
		Loaded += OnLoaded;
	}

	void OnLoaded(object sender, EventArgs e)
	{
		Loaded -= OnLoaded;
		var started = DateTime.UtcNow;

		Dispatcher.Dispatch(() =>
		{
			_baselineMilliseconds = Math.Max(1, (DateTime.UtcNow - started).TotalMilliseconds);
			_callbackToken = 0;
			UpdateMetrics();
		});
	}

	void OnAddEditorsClicked(object sender, EventArgs e)
	{
		var started = DateTime.UtcNow;

		Dispatcher.Dispatch(() =>
		{
			_postAddMilliseconds = (DateTime.UtcNow - started).TotalMilliseconds;
			_callbackToken = 1;
			UpdateMetrics();
		});

		for (var i = 0; i < EntryCount; i++)
		{
			var entry = new Entry();
			_entryCanvas.Children.Add(entry);
			AbsoluteLayout.SetLayoutBounds(
				entry,
				new Rect((i % 10) * 190, (i / 10) * 60, 180, 48));
		}

		UpdateMetrics();
	}

	void UpdateMetrics()
	{
		_metricsLabel.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"CallbackToken={_callbackToken};ChildCount={_entryCanvas.Children.Count};BaselineMs={_baselineMilliseconds:F3};PostAddMs={_postAddMilliseconds:F3}");
	}
}

