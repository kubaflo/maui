#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 17877, "TabbedPage does not notify when the current tab is reselected", PlatformAffected.Android)]
public class Issue17877 : ContentPage
{
	public Issue17877()
	{
		var currentPageReselectedCount = -1;
		var countBeforeReselection = -1;
		var hostApplied = false;
		var reselectionEvent = typeof(TabbedPage).GetEvent("CurrentPageReselected");

		var resultLabel = new Label
		{
			AutomationId = "Issue17877Result",
			Text = "Waiting for reselection",
			FontSize = 20,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		var eventCountLabel = new Label
		{
			AutomationId = "Issue17877Count",
			Text = "CurrentPageReselected count: unavailable",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		var armButton = new Button
		{
			AutomationId = "Issue17877Arm",
			Text = "Arm reselection check"
		};

		var checkButton = new Button
		{
			AutomationId = "Issue17877Check",
			Text = "Check reselection result"
		};

		var tabOneContent = new Grid
		{
			Padding = 16,
			RowSpacing = 8,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
		tabOneContent.Add(resultLabel, 0, 0);
		tabOneContent.Add(new Label
		{
			AutomationId = "Issue17877TabOneContent",
			Text = "Tab 1 content",
			FontSize = 24,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		}, 0, 1);
		tabOneContent.Add(eventCountLabel, 0, 2);
		tabOneContent.Add(armButton, 0, 3);
		tabOneContent.Add(checkButton, 0, 4);

		var tabOne = new ContentPage
		{
			Title = "Tab 1",
			Content = tabOneContent
		};

		var tabTwo = new ContentPage
		{
			Title = "Tab 2",
			Content = new Grid
			{
				Children =
				{
					new Label
					{
						AutomationId = "Issue17877TabTwoContent",
						Text = "Tab 2 content",
						FontSize = 24,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					}
				}
			}
		};

		var tabbedPage = new TabbedPage
		{
			Children =
			{
				tabOne,
				tabTwo
			}
		};
		Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage.SetToolbarPlacement(
			tabbedPage,
			Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement.Bottom);

		if (reselectionEvent is not null && reselectionEvent.EventHandlerType == typeof(EventHandler))
		{
			currentPageReselectedCount = 0;
			eventCountLabel.Text = "CurrentPageReselected count: 0";
			reselectionEvent.AddEventHandler(tabbedPage, new EventHandler((_, _) =>
			{
				currentPageReselectedCount++;
				eventCountLabel.Text = $"CurrentPageReselected count: {currentPageReselectedCount}";
			}));
		}

		armButton.Clicked += (_, _) =>
		{
			countBeforeReselection = currentPageReselectedCount;
			resultLabel.Text = $"Armed at count {countBeforeReselection}";
		};

		checkButton.Clicked += (_, _) =>
		{
			resultLabel.Text = reselectionEvent is null
				? "CurrentPageReselected event is unavailable"
				: currentPageReselectedCount > countBeforeReselection
					? $"Reselection notified at count {currentPageReselectedCount}"
					: $"No reselection notification at count {currentPageReselectedCount}";
		};

		Loaded += (_, _) =>
		{
			if (hostApplied || Window is null)
				return;

			hostApplied = true;
			Window.Page = tabbedPage;
		};
	}
}
#endif

