namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30949, "SwipeView swipe events do not fire on Windows", PlatformAffected.UWP)]
public class Issue30949 : ContentPage
{
	public Issue30949()
	{
		var startedCount = 0;
		var changingCount = 0;
		var endedCount = 0;

		var eventCounts = new Label
		{
			AutomationId = "Issue30949EventCounts",
			Text = "Started: 0; Changing: 0; Ended: 0"
		};

		void UpdateEventCounts()
		{
			eventCounts.Text = $"Started: {startedCount}; Changing: {changingCount}; Ended: {endedCount}";
		}

		var swipeView = new SwipeView
		{
			AutomationId = "Issue30949SwipeView",
			LeftItems = new SwipeItems
			{
				new SwipeItem
				{
					AutomationId = "Issue30949LeftItem",
					Text = "Left",
					BackgroundColor = Colors.Red
				}
			},
			Content = new Label
			{
				AutomationId = "Issue30949SwipeContent",
				Text = "Swipe me"
			}
		};

		swipeView.SwipeStarted += (sender, args) =>
		{
			startedCount++;
			UpdateEventCounts();
		};
		swipeView.SwipeChanging += (sender, args) =>
		{
			changingCount++;
			UpdateEventCounts();
		};
		swipeView.SwipeEnded += (sender, args) =>
		{
			endedCount++;
			UpdateEventCounts();
		};

		var root = new Grid
		{
			AutomationId = "Issue30949Root",
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};

		root.Add(new Label
		{
			Text = "Issue 30949: SwipeView events",
			FontSize = 24
		});
		root.Add(swipeView, 0, 1);
		root.Add(eventCounts, 0, 2);

		Content = root;
	}
}

