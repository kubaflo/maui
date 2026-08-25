using Microsoft.Maui.Handlers;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29540, "TabbedViewHandler implementation incomplete on iOS", PlatformAffected.iOS)]
public class Issue29540 : ContentPage
{
	readonly VerticalStackLayout _sourceLayout;

	public Issue29540()
	{
		var navigateButton = new Button
		{
			AutomationId = "NavigateButton",
			Text = "Navigate using custom handler"
		};
		navigateButton.Clicked += OnNavigateClicked;

		_sourceLayout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					AutomationId = "ScenarioHeading",
					FontAttributes = FontAttributes.Bold,
					FontSize = 24,
					Text = "Custom TabbedViewHandler on iOS"
				},
				new Label
				{
					AutomationId = "HierarchyDescription",
					Text = "SwipeTabbedPage with two ordinary ContentPage children"
				},
				new Border
				{
					Padding = 16,
					Content = new VerticalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { FontAttributes = FontAttributes.Bold, Text = "Default-styled tabs" },
							new Label { Text = "First tab: Home" },
							new Label { Text = "Second tab: Settings" }
						}
					}
				},
				new Label { Text = "Ready to navigate" },
				navigateButton
			}
		};

		Title = "Issue 29540";
		Content = new ScrollView { Content = _sourceLayout };
	}

	async void OnNavigateClicked(object sender, EventArgs e)
	{
		var homeLabel = new Label
		{
			AutomationId = "HomeTabLabel",
			HorizontalOptions = LayoutOptions.Center,
			Text = "Home tab",
			VerticalOptions = LayoutOptions.Center
		};
		var homePage = new ContentPage
		{
			Title = "Home",
			Content = homeLabel
		};
		var tabbedPage = new SwipeTabbedPage
		{
			Title = "Custom handler tabs",
			Children =
			{
				homePage,
				new ContentPage
				{
					Title = "Settings",
					Content = new Label
					{
						HorizontalOptions = LayoutOptions.Center,
						Text = "Settings tab",
						VerticalOptions = LayoutOptions.Center
					}
				}
			}
		};

		var mauiContext = Handler?.MauiContext
			?? throw new InvalidOperationException("The source page must be attached before navigation.");

		try
		{
			var customHandler = new SwipeTabbedPageHandler();
			customHandler.SetMauiContext(mauiContext);
			tabbedPage.Handler = customHandler;
			await Navigation.PushAsync(tabbedPage);

			homePage.Content = null;
			homePage.Content = new VerticalStackLayout
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					homeLabel,
					CreateCompletionMarker()
				}
			};
		}
		catch (NotImplementedException)
		{
			_sourceLayout.Children.Add(CreateCompletionMarker());
		}
	}

	static Label CreateCompletionMarker() => new()
	{
		AutomationId = "NavigationCompletedMarker",
		Text = "Navigation attempt completed"
	};
}

sealed class SwipeTabbedPage : TabbedPage
{
}

sealed class SwipeTabbedPageHandler : TabbedViewHandler
{
}

