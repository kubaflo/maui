namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35929, "[iOS] Debugging breakpoint never binds on device", PlatformAffected.iOS)]
public class Issue35929 : ContentPage
{
	int _clickCount;
	readonly Button _counterButton;

	public Issue35929()
	{
		Title = "Breakpoint binding";

		_counterButton = new Button
		{
			Text = "Click me",
			AutomationId = "Issue35929CounterButton",
			HorizontalOptions = LayoutOptions.Fill
		};
		_counterButton.Clicked += OnCounterClicked;

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(30, 0),
				Spacing = 25,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label
					{
						Text = "Counter breakpoint target",
						FontSize = 24,
						HorizontalOptions = LayoutOptions.Center
					},
					_counterButton,
					new Label
					{
						Text = "Tap the button to increment the counter.",
						FontSize = 18,
						HorizontalOptions = LayoutOptions.Center
					}
				}
			}
		};
	}

	void OnCounterClicked(object sender, EventArgs e)
	{
		_clickCount++;
		_counterButton.Text = _clickCount == 1
			? $"Clicked {_clickCount} time"
			: $"Clicked {_clickCount} times";
	}
}

