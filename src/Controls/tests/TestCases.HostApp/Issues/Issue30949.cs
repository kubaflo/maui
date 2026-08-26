namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30949, "[Windows] SwipeView gesture events are not raised", PlatformAffected.UWP)]
public class Issue30949 : ContentPage
{
	int swipeStartedCount;
	int swipeChangingCount;
	int swipeEndedCount;

	public Issue30949()
	{
		var eventStatusLabel = new Label
		{
			AutomationId = "Issue30949EventStatus",
			Text = GetEventStatus()
		};

		var resultLabel = new Label
		{
			AutomationId = "Issue30949Result",
			Text = "Check the event counts after swiping."
		};

		var swipeView = new SwipeView
		{
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
				AutomationId = "Issue30949SwipeTarget",
				Text = "Swipe me"
			}
		};

		swipeView.SwipeStarted += (sender, args) =>
		{
			swipeStartedCount++;
			eventStatusLabel.Text = GetEventStatus();
		};
		swipeView.SwipeChanging += (sender, args) =>
		{
			swipeChangingCount++;
			eventStatusLabel.Text = GetEventStatus();
		};
		swipeView.SwipeEnded += (sender, args) =>
		{
			swipeEndedCount++;
			eventStatusLabel.Text = GetEventStatus();
		};

		var checkButton = new Button
		{
			AutomationId = "Issue30949CheckEvents",
			Text = "Check events"
		};
		checkButton.Clicked += (sender, args) =>
		{
			resultLabel.Text = swipeStartedCount > 0 && swipeChangingCount > 0 && swipeEndedCount > 0
				? "All SwipeView events fired."
				: "One or more SwipeView events did not fire.";
		};

		Content = new VerticalStackLayout
		{
			Children =
			{
				new Label { Text = "Swipe the Swipe me area to the right, then select Check events." },
				swipeView,
				eventStatusLabel,
				checkButton,
				resultLabel
			}
		};
	}

	string GetEventStatus() =>
		$"Events: started={swipeStartedCount}, changing={swipeChangingCount}, ended={swipeEndedCount}";
}

