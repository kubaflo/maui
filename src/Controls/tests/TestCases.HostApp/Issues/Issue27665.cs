using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27665, "Flickering when hiding or showing elements from ScrollView.Scrolled", PlatformAffected.Android)]
public class Issue27665 : ContentPage
{
	readonly Entry _headerEntry;
	readonly Image _headerImage;
	readonly Entry _measurementsEntry;
	readonly ScrollView _scrollView;
	readonly List<double> _scrollYSamples = [];
	bool _mutateHeader;
	bool _resetting;
	double _resetScrollY = -1;

	public Issue27665()
	{
		_headerEntry = new Entry
		{
			AutomationId = "HeaderEntry",
			BackgroundColor = Colors.Green,
			Placeholder = "This is my entry",
			HorizontalOptions = new LayoutOptions(LayoutAlignment.Fill, true),
			VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true)
		};

		_headerImage = new Image
		{
			AutomationId = "HeaderImage",
			Source = "dotnet_bot.png",
			HeightRequest = 24
		};

		var headerGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(new GridLength(9, GridUnitType.Star)),
				new ColumnDefinition(new GridLength(1, GridUnitType.Star))
			}
		};
		headerGrid.Add(_headerEntry, 0, 0);
		headerGrid.Add(_headerImage, 1, 0);

		_measurementsEntry = new Entry
		{
			AutomationId = "ScrollMeasurements",
			IsReadOnly = true
		};

		var resetRecognizer = new TapGestureRecognizer();
		resetRecognizer.Tapped += async (sender, args) =>
		{
			_resetting = true;
			await _scrollView.ScrollToAsync(0, 0, false);
			_resetScrollY = _scrollView.ScrollY;
			_headerEntry.IsVisible = true;
			_headerImage.IsVisible = true;
			_mutateHeader = true;
			_resetting = false;
			ResetMeasurements();
		};
		_measurementsEntry.GestureRecognizers.Add(resetRecognizer);

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
			var itemLabel = new Label
			{
				Text = $"Elemento {index}",
				FontSize = 18
			};

			if (index == 7)
				itemLabel.AutomationId = "Elemento7";

			scrollContent.Add(itemLabel);
		}

		_scrollView = new ScrollView
		{
			AutomationId = "Issue27665ScrollView",
			Content = scrollContent,
			HorizontalOptions = new LayoutOptions(LayoutAlignment.Fill, true),
			VerticalOptions = new LayoutOptions(LayoutAlignment.Fill, true)
		};
		_scrollView.Scrolled += OnScrolled;

		Content = new StackLayout
		{
			Children =
			{
				headerGrid,
				_measurementsEntry,
				_scrollView
			}
		};

		UpdateMeasurements();
	}

	void OnScrolled(object sender, ScrolledEventArgs args)
	{
		if (_resetting)
			return;

		if (_mutateHeader)
		{
			var showHeader = args.ScrollY <= 0;
			_headerEntry.IsVisible = showHeader;
			_headerImage.IsVisible = showHeader;
		}

		_scrollYSamples.Add(args.ScrollY);
		UpdateMeasurements();
	}

	void ResetMeasurements()
	{
		_scrollYSamples.Clear();
		UpdateMeasurements();
	}

	void UpdateMeasurements()
	{
		var samples = string.Join(",", _scrollYSamples.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
		_measurementsEntry.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"MutationEnabled={(_mutateHeader ? 1 : 0)};ResetY={_resetScrollY};Samples={samples}");
	}
}

