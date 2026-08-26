namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37180, "Label background does not reset when set to null", PlatformAffected.iOS)]
public class Issue37180 : ContentPage
{
	public Issue37180()
	{
		var backgroundLabel = new Label
		{
			Text = "Label Background Test",
			AutomationId = "BackgroundLabel",
			Padding = new Thickness(10),
		};

		var actionStatus = new Label
		{
			Text = "READY: default background",
			AutomationId = "ActionStatus",
		};

		var setRedButton = new Button
		{
			Text = "Set Background to Red",
			AutomationId = "SetRedButton",
		};
		setRedButton.Clicked += (sender, args) =>
		{
			backgroundLabel.Background = Colors.Red;
			actionStatus.Text = "READY: red background applied";
		};

		var setNullButton = new Button
		{
			Text = "Set Background to null",
			AutomationId = "SetNullButton",
		};
		setNullButton.Clicked += (sender, args) =>
		{
			backgroundLabel.Background = null;
			actionStatus.Text = "READY: null background applied";
		};

		Content = new VerticalStackLayout
		{
			Margin = new Thickness(20),
			Spacing = 10,
			Children =
			{
				backgroundLabel,
				setRedButton,
				setNullButton,
				actionStatus,
			},
		};
	}
}

