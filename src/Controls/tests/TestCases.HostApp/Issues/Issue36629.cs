#if WINDOWS
using System.Globalization;
using WAutoSuggestBox = Microsoft.UI.Xaml.Controls.AutoSuggestBox;
using WNavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36629, "SearchHandler font properties are not applied on Windows", PlatformAffected.UWP)]
public class Issue36629 : Shell
{
	readonly SearchHandler _searchHandler;
	readonly Label _fontSizeMeasurementLabel;
	readonly Label _fontFamilyMeasurementLabel;
	readonly Label _verticalAlignmentMeasurementLabel;
	readonly Label _fontAttributesMeasurementLabel;
#if WINDOWS
	int _measurementSequence;
#endif

	public Issue36629()
	{
		_searchHandler = new SearchHandler
		{
			AutomationId = "Issue36629SearchHandler",
			Placeholder = "Search text",
			Query = "Style me",
		};

		_fontSizeMeasurementLabel = CreateMeasurementLabel("FontSizeMeasurement");
		_fontFamilyMeasurementLabel = CreateMeasurementLabel("FontFamilyMeasurement");
		_verticalAlignmentMeasurementLabel = CreateMeasurementLabel("VerticalAlignmentMeasurement");
		_fontAttributesMeasurementLabel = CreateMeasurementLabel("FontAttributesMeasurement");

		var fontSizeButton = new Button
		{
			AutomationId = "FontSizeButton",
			Text = "FontSize",
		};
		fontSizeButton.Clicked += OnFontSizeClicked;

		var fontFamilyButton = new Button
		{
			AutomationId = "FontFamilyButton",
			Text = "FontFamily",
		};
		fontFamilyButton.Clicked += OnFontFamilyClicked;

		var verticalAlignmentButton = new Button
		{
			AutomationId = "VerticalTextAlignmentButton",
			Text = "VerticalTextAlignment",
		};
		verticalAlignmentButton.Clicked += OnVerticalTextAlignmentClicked;

		var fontAttributesButton = new Button
		{
			AutomationId = "FontAttributesButton",
			Text = "FontAttributes",
		};
		fontAttributesButton.Clicked += OnFontAttributesClicked;

		var page = new ContentPage
		{
			Title = "SearchHandler styling",
			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 12,
					Children =
					{
						new Label
						{
							Text = "Use each button to style the SearchHandler shown above.",
							FontSize = 18,
						},
						fontSizeButton,
						fontFamilyButton,
						verticalAlignmentButton,
						fontAttributesButton,
						_fontSizeMeasurementLabel,
						_fontFamilyMeasurementLabel,
						_verticalAlignmentMeasurementLabel,
						_fontAttributesMeasurementLabel,
					},
				},
			},
		};

		SetSearchHandler(page, _searchHandler);
		Items.Add(new ShellContent
		{
			Title = "SearchHandler styling",
			Content = page,
		});
	}

	static Label CreateMeasurementLabel(string automationId) =>
		new()
		{
			AutomationId = automationId,
			Text = "sequence=0;managed=unmeasured;native=unmeasured",
			FontSize = 14,
		};

	void OnFontSizeClicked(object sender, EventArgs e)
	{
		_searchHandler.FontSize = 30;
		Dispatcher.Dispatch(MeasureFontSize);
	}

	void OnFontFamilyClicked(object sender, EventArgs e)
	{
		_searchHandler.FontFamily = "OpenSansRegular";
		Dispatcher.Dispatch(MeasureFontFamily);
	}

	void OnVerticalTextAlignmentClicked(object sender, EventArgs e)
	{
		_searchHandler.VerticalTextAlignment = TextAlignment.End;
		Dispatcher.Dispatch(MeasureVerticalTextAlignment);
	}

	void OnFontAttributesClicked(object sender, EventArgs e)
	{
		_searchHandler.FontAttributes = FontAttributes.Bold;
		Dispatcher.Dispatch(MeasureFontAttributes);
	}

	void MeasureFontSize()
	{
#if WINDOWS
		if (!TryGetNativeSearchBox(out var searchBox))
		{
			RecordUnavailable(_fontSizeMeasurementLabel, _searchHandler.FontSize.ToString(CultureInfo.InvariantCulture));
			return;
		}

		_fontSizeMeasurementLabel.Text = CreateMeasurement(
			_searchHandler.FontSize.ToString(CultureInfo.InvariantCulture),
			searchBox.FontSize.ToString(CultureInfo.InvariantCulture));
#endif
	}

	void MeasureFontFamily()
	{
#if WINDOWS
		var managedFontFamily = _searchHandler.FontFamily ?? "unavailable";

		if (!TryGetNativeSearchBox(out var searchBox))
		{
			RecordUnavailable(_fontFamilyMeasurementLabel, managedFontFamily);
			return;
		}

		var nativeFontFamily = searchBox.FontFamily;
		_fontFamilyMeasurementLabel.Text = CreateMeasurement(
			managedFontFamily,
			nativeFontFamily?.Source ?? "unavailable");
#endif
	}

	void MeasureVerticalTextAlignment()
	{
#if WINDOWS
		if (!TryGetNativeSearchBox(out var searchBox))
		{
			RecordUnavailable(_verticalAlignmentMeasurementLabel, _searchHandler.VerticalTextAlignment.ToString());
			return;
		}

		_verticalAlignmentMeasurementLabel.Text = CreateMeasurement(
			_searchHandler.VerticalTextAlignment.ToString(),
			searchBox.VerticalContentAlignment.ToString());
#endif
	}

	void MeasureFontAttributes()
	{
#if WINDOWS
		if (!TryGetNativeSearchBox(out var searchBox))
		{
			RecordUnavailable(_fontAttributesMeasurementLabel, _searchHandler.FontAttributes.ToString());
			return;
		}

		_fontAttributesMeasurementLabel.Text = CreateMeasurement(
			_searchHandler.FontAttributes.ToString(),
			searchBox.FontWeight.Weight.ToString(CultureInfo.InvariantCulture));
#endif
	}

#if WINDOWS
	bool TryGetNativeSearchBox(out WAutoSuggestBox searchBox)
	{
		var navigationView = CurrentItem?.Handler?.PlatformView as WNavigationView;
		if (navigationView is null || navigationView.AutoSuggestBox is not WAutoSuggestBox nativeSearchBox)
		{
			searchBox = default!;
			return false;
		}

		searchBox = nativeSearchBox;
		return true;
	}

	void RecordUnavailable(Label measurementLabel, string managedValue)
	{
		measurementLabel.Text = CreateMeasurement(managedValue, "unavailable");
	}

	string CreateMeasurement(string managedValue, string nativeValue)
	{
		_measurementSequence++;
		return $"sequence={_measurementSequence};managed={managedValue};native={nativeValue}";
	}
#endif
}

