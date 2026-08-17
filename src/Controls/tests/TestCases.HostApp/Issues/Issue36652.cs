namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36652, "Border containing SwipeView causes a native crash on Windows", PlatformAffected.UWP)]
public class Issue36652 : ContentPage
{
	public Issue36652()
	{
		var reproduceButton = new Button
		{
			AutomationId = "ReproduceButton",
			Text = "Create reported hierarchy"
		};
		reproduceButton.Clicked += OnReproduceClicked;

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					AutomationId = "InstructionsLabel",
					Text = "Issue 36652: Border containing SwipeView and Editor"
				},
				new Label
				{
					AutomationId = "ReadyStatus",
					Text = "Ready to create the reported hierarchy."
				},
				reproduceButton
			}
		};
	}

	void OnReproduceClicked(object sender, EventArgs e)
	{
		var leftItems = new SwipeItems
		{
			Mode = SwipeMode.Execute
		};
		leftItems.Add(new SwipeItem { Text = "Back" });

		var border = new Border
		{
			AutomationId = "ReportedHierarchyNotCompleted",
			Stroke = Colors.DarkGray,
			StrokeThickness = 1,
			Content = new SwipeView
			{
				Threshold = 80,
				LeftItems = leftItems,
				Content = new Editor()
			}
		};

		border.SizeChanged += OnReportedHierarchySizeChanged;
		Content = border;
	}

	void OnReportedHierarchySizeChanged(object sender, EventArgs e)
	{
		var border = (Border)sender;
		if (border.Width > 0 && border.Height > 0)
		{
			border.SizeChanged -= OnReportedHierarchySizeChanged;
			border.AutomationId = "ReportedHierarchyCompleted";
		}
	}
}
