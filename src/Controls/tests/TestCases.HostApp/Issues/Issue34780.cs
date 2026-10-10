namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34780, "iOS 26 TabBar has opaque background", PlatformAffected.iOS)]
public class Issue34780 : ContentPage
{
	const double TabBarCompensation = 85;
	bool _installed;

	public Issue34780()
	{
		var correctGrid = CreateTabContent("CorrectGrid", "CorrectContent", "Correct", Colors.CornflowerBlue);
		correctGrid.Margin = new Thickness(0, 0, 0, -TabBarCompensation);
		correctGrid.Padding = new Thickness(0, 0, 0, TabBarCompensation);

		var incorrectGrid = CreateTabContent("IncorrectGrid", "IncorrectContent", "Incorrect", Colors.Orange);

		var tabbedPage = new TabbedPage
		{
			Children =
			{
				new ContentPage
				{
					Title = "Correct",
					Content = correctGrid
				},
				new ContentPage
				{
					Title = "Incorrect",
					Content = incorrectGrid
				}
			}
		};

		Content = new Label
		{
			Text = "Preparing TabbedPage",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		Loaded += (_, _) =>
		{
			if (_installed || Window is null)
				return;

			_installed = true;
			Window.Page = tabbedPage;
		};
	}

	static Grid CreateTabContent(string gridAutomationId, string contentAutomationId, string caption, Color headerColor)
	{
		var stack = new VerticalStackLayout
		{
			AutomationId = contentAutomationId,
			Spacing = 0
		};

		stack.Children.Add(new Label
		{
			AutomationId = $"{caption}Header",
			BackgroundColor = headerColor,
			FontSize = 28,
			HeightRequest = 180,
			Padding = new Thickness(24),
			Text = $"{caption} tab content",
			TextColor = Colors.White
		});

		for (var index = 1; index <= 8; index++)
		{
			stack.Children.Add(new Label
			{
				AutomationId = $"{caption}Row{index}",
				BackgroundColor = index % 2 == 0 ? Colors.LightBlue : Colors.LightGoldenrodYellow,
				FontSize = 20,
				HeightRequest = 120,
				Padding = new Thickness(24),
				Text = $"{caption} row {index}",
				TextColor = Colors.Black
			});
		}

		return new Grid
		{
			AutomationId = gridAutomationId,
			Children =
			{
				new ScrollView
				{
					Content = stack
				}
			}
		};
	}
}

