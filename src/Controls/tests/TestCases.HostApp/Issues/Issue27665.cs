using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27665, "Flickering when hiding and showing elements from ScrollView.Scrolled on Android", PlatformAffected.Android)]
public class Issue27665 : ContentPage
{
	readonly Entry _headerEntry;
	readonly Image _headerImage;
	readonly Label _telemetryLabel;
	bool _headerHidden;
	int _events = -1;
	int _hiddenTransitions;
	int _shownTransitions;
	double _maximumScrollY;

	public Issue27665()
	{
		_telemetryLabel = new Label
		{
			AutomationId = "Issue27665Telemetry",
			Text = FormatTelemetry(0)
		};

		_headerEntry = new Entry
		{
			AutomationId = "Issue27665Entry",
			BackgroundColor = Colors.Green,
			Placeholder = "This is my entry",
			HorizontalOptions = new LayoutOptions(LayoutAlignment.Fill, true),
			VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true)
		};

		_headerImage = new Image
		{
			AutomationId = "Issue27665Image",
			Source = "dotnet_bot.png",
			HeightRequest = 24
		};

		var header = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(new GridLength(9, GridUnitType.Star)),
				new ColumnDefinition(new GridLength(1, GridUnitType.Star))
			}
		};
		header.Add(_headerEntry);
		header.Add(_headerImage, 1);

		var scrollContent = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 10,
			HorizontalOptions = new LayoutOptions(LayoutAlignment.Fill, true),
			VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true)
		};
		scrollContent.Add(new Label
		{
			Text = "Element's list",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			FontAttributes = FontAttributes.Bold
		});

		for (var index = 1; index <= 20; index++)
		{
			scrollContent.Add(new Label
			{
				Text = $"Elemento {index}",
				FontSize = 18
			});
		}

		var scrollView = new ScrollView
		{
			AutomationId = "Issue27665ScrollView",
			Content = scrollContent,
			HorizontalOptions = new LayoutOptions(LayoutAlignment.Fill, true),
			VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true)
		};
		scrollView.Scrolled += OnScrolled;

		Content = new StackLayout
		{
			Children =
			{
				_telemetryLabel,
				header,
				scrollView
			}
		};
	}

	void OnScrolled(object sender, ScrolledEventArgs e)
	{
		_events++;
		_maximumScrollY = Math.Max(_maximumScrollY, e.ScrollY);

		if (e.ScrollY > 0 && !_headerHidden)
		{
			_headerHidden = true;
			_hiddenTransitions++;
			_headerEntry.IsVisible = false;
			_headerImage.IsVisible = false;
		}
		else if (e.ScrollY <= 0 && _headerHidden)
		{
			_headerHidden = false;
			_shownTransitions++;
			_headerEntry.IsVisible = true;
			_headerImage.IsVisible = true;
		}

		_telemetryLabel.Text = FormatTelemetry(e.ScrollY);
	}

	string FormatTelemetry(double scrollY) =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"Events={_events};CallbackObserved={_events >= 0};Hidden={_hiddenTransitions};Shown={_shownTransitions};ScrollY={scrollY:F2};MaxScrollY={_maximumScrollY:F2};MaxPositive={_maximumScrollY > 0}");
}

