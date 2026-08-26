using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 27665, "Flickering when hiding or showing elements in a ScrollView Scrolled event", PlatformAffected.Android)]
public class Issue27665 : ContentPage
{
	readonly Entry headerEntry;
	readonly Image headerImage;
	readonly Button diagnosticButton;
	int scrollEventCount;
	int visibilityTransitionCount;

	public Issue27665()
	{
		var fillAndExpand = new LayoutOptions(LayoutAlignment.Fill, true);

		diagnosticButton = new Button
		{
			AutomationId = "Issue27665Counts",
			Text = FormatCounts()
		};

		headerEntry = new Entry
		{
			AutomationId = "Issue27665Entry",
			BackgroundColor = Colors.Green,
			Placeholder = "This is my entry",
			HorizontalOptions = fillAndExpand,
			VerticalOptions = fillAndExpand
		};

		headerImage = new Image
		{
			AutomationId = "Issue27665Image",
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
		headerGrid.Add(headerEntry);
		headerGrid.Add(headerImage, 1);

		var list = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 10,
			HorizontalOptions = fillAndExpand,
			VerticalOptions = fillAndExpand
		};
		list.Add(new Label
		{
			Text = "Element's list",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			FontAttributes = FontAttributes.Bold
		});

		for (var index = 1; index <= 20; index++)
		{
			list.Add(new Label
			{
				AutomationId = $"Issue27665Row{index}",
				Text = $"Elemento {index}",
				FontSize = 18
			});
		}

		var scrollView = new ScrollView
		{
			AutomationId = "Issue27665ScrollView",
			Content = list,
			VerticalOptions = fillAndExpand,
			HorizontalOptions = fillAndExpand
		};
		scrollView.Scrolled += (_, e) => OnScrolled(e);

		Content = new StackLayout
		{
			Children =
			{
				diagnosticButton,
				headerGrid,
				scrollView
			}
		};
	}

	void OnScrolled(ScrolledEventArgs e)
	{
		scrollEventCount++;
		var shouldShowHeader = e.ScrollY <= 0;

		if (headerEntry.IsVisible != shouldShowHeader)
		{
			headerEntry.IsVisible = shouldShowHeader;
			headerImage.IsVisible = shouldShowHeader;
			visibilityTransitionCount++;
		}

		diagnosticButton.Text = FormatCounts();
	}

	string FormatCounts() =>
		$"Events={scrollEventCount.ToString(CultureInfo.InvariantCulture)};Transitions={visibilityTransitionCount.ToString(CultureInfo.InvariantCulture)}";
}

