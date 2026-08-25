using System.Diagnostics;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35929, "[iOS] Debugging breakpoint never binds on device", PlatformAffected.iOS)]
public class Issue35929 : ContentPage
{
	readonly Button _counterButton;
	readonly Label _debuggerStatus;
	int _count;

	public Issue35929()
	{
		_counterButton = new Button
		{
			AutomationId = "CounterButton",
			Text = "Click me",
			HorizontalOptions = LayoutOptions.Fill
		};
		_counterButton.Clicked += (_, _) => OnCounterClicked();

		_debuggerStatus = new Label
		{
			AutomationId = "DebuggerStatus",
			Text = "OnCounterClicked has not completed"
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(30, 0),
				Spacing = 25,
				Children =
				{
					new Image
					{
						Source = "dotnet_bot.png",
						HeightRequest = 185,
						Aspect = Aspect.AspectFit
					},
					new Label
					{
						Text = "Hello, World!",
						FontSize = 32,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "Welcome to \n.NET Multi-platform App UI",
						FontSize = 18
					},
					_counterButton,
					_debuggerStatus
				}
			}
		};
	}

	void OnCounterClicked()
	{
		_count++;
		_counterButton.Text = _count == 1
			? $"Clicked {_count} time"
			: $"Clicked {_count} times";
		_debuggerStatus.Text = $"OnCounterClicked completed; Debugger.IsAttached={Debugger.IsAttached}";
	}
}

