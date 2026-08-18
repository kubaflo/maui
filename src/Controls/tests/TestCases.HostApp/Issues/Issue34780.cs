#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34780, "iOS 26 TabBar has opaque background", PlatformAffected.iOS)]
public class Issue34780 : ContentPage
{
	readonly TabbedPage _issueTabs;
	bool _rootInstalled;

	public Issue34780()
	{
		var transitionMarker = new Label
		{
			AutomationId = "Issue34780Transition",
			Text = "-1"
		};

		var correctGrid = CreateContentGrid("Correct");
		correctGrid.Margin = new Thickness(0, 0, 0, -100);
		correctGrid.Padding = new Thickness(0, 0, 0, 100);
		correctGrid.Children.Add(new Label
		{
			AutomationId = "CorrectStyle",
			Text = "MarginBottom=-100;PaddingBottom=100"
		});

		var incorrectGrid = CreateContentGrid("Incorrect");
		incorrectGrid.Children.Add(transitionMarker);

		var correctPage = new ContentPage
		{
			Title = "Correct",
			Content = correctGrid
		};
		var incorrectPage = new ContentPage
		{
			Title = "Incorrect",
			Content = incorrectGrid
		};

		_issueTabs = new TabbedPage();
		_issueTabs.Children.Add(correctPage);
		_issueTabs.Children.Add(incorrectPage);
		_issueTabs.CurrentPageChanged += (_, _) =>
		{
			if (_issueTabs.CurrentPage == incorrectPage)
				transitionMarker.Text = "1";
		};

		Loaded += OnLoaded;
	}

	static Grid CreateContentGrid(string prefix)
	{
		var stack = new VerticalStackLayout
		{
			Spacing = 0
		};
		stack.Children.Add(new Label
		{
			AutomationId = $"{prefix}Heading",
			BackgroundColor = Color.FromArgb("#E8F0FE"),
			FontAttributes = FontAttributes.Bold,
			FontSize = 22,
			Padding = 16,
			Text = $"{prefix}: content beneath the Liquid Glass tab bar"
		});

		(Color Color, string Name)[] colors =
		[
			(Colors.Red, "Red"),
			(Colors.Gold, "Gold"),
			(Colors.Green, "Green"),
			(Colors.Blue, "Blue"),
			(Colors.Purple, "Purple"),
			(Colors.Orange, "Orange")
		];

		foreach (var color in colors)
		{
			stack.Children.Add(new BoxView
			{
				AutomationId = $"{prefix}{color.Name}Box",
				BackgroundColor = color.Color,
				HeightRequest = 120
			});
		}

		stack.Children.Add(new Label
		{
			AutomationId = $"{prefix}Footer",
			BackgroundColor = Color.FromArgb("#E8F0FE"),
			FontAttributes = FontAttributes.Bold,
			FontSize = 20,
			Padding = 20,
			Text = "Color remains visible beneath the floating tab bar"
		});

		var scrollView = new ScrollView
		{
			AutomationId = $"{prefix}ScrollView",
			Content = stack
		};
		var grid = new Grid
		{
			AutomationId = $"{prefix}Grid",
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			}
		};
		grid.Children.Add(scrollView);
		Grid.SetRow(scrollView, 1);
		return grid;
	}

	void OnLoaded(object sender, EventArgs e)
	{
		if (_rootInstalled)
			return;

		_rootInstalled = true;
		Window.Page = _issueTabs;
	}
}
#endif
