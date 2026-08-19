#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36816, "Clicks pass through ContentView to controls underneath", PlatformAffected.Android)]
public class Issue36816 : ContentPage
{
	int _pressCount;

	public Issue36816()
	{
		AutomationId = "Issue36816Page";

		var pressCountLabel = new Label
		{
			AutomationId = "Issue36816PressCount",
			Text = "Underlying button press count: 0",
			FontSize = 18
		};

		var resultLabel = new Label
		{
			AutomationId = "Issue36816Result",
			Text = "The covered button has not received the overlay tap.",
			FontSize = 18,
			FontAttributes = FontAttributes.Bold
		};

		var coveredButton = new Button
		{
			AutomationId = "Issue36816CoveredButton",
			Text = "Covered button"
		};
		coveredButton.Clicked += (sender, args) =>
		{
			_pressCount++;
			pressCountLabel.Text = $"Underlying button press count: {_pressCount}";
			resultLabel.Text = "The covered button received the overlay tap.";
		};

		var greenOverlay = new ContentView
		{
			AutomationId = "Issue36816GreenOverlay",
			BackgroundColor = Colors.Green,
			WidthRequest = 220,
			HeightRequest = 110,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		var overlapGrid = new Grid
		{
			WidthRequest = 320,
			HeightRequest = 180,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				coveredButton,
				greenOverlay
			}
		};

		var rootGrid = new Grid
		{
			Padding = 24,
			RowSpacing = 18,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Star },
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto }
			}
		};

		rootGrid.Add(new Label
		{
			Text = "Tap the green view centered over the covered button.",
			FontSize = 18
		});
		rootGrid.Add(overlapGrid, 0, 1);
		rootGrid.Add(pressCountLabel, 0, 2);
		rootGrid.Add(resultLabel, 0, 3);

		Content = rootGrid;
	}
}
#endif
