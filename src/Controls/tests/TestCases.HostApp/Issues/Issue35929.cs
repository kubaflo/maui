using System.Diagnostics;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35929, "[iOS] Managed breakpoint does not bind on a physical device", PlatformAffected.iOS)]
public class Issue35929 : ContentPage
{
	readonly Label _counterLabel;
	readonly Label _handlerStatusLabel;
	readonly Label _debuggerStatusLabel;
	readonly Button _counterButton;
	int _count;

	public Issue35929()
	{
		_counterLabel = new Label
		{
			Text = "Current count: 0",
			AutomationId = "CounterLabel",
			HorizontalOptions = LayoutOptions.Center
		};

		_handlerStatusLabel = new Label
		{
			Text = "Handler invoked: 0",
			AutomationId = "HandlerStatusLabel",
			HorizontalOptions = LayoutOptions.Center
		};

		_debuggerStatusLabel = new Label
		{
			Text = "Managed debugger attached: not observed",
			AutomationId = "DebuggerStatusLabel",
			HorizontalOptions = LayoutOptions.Center
		};

		_counterButton = new Button
		{
			Text = "Click me",
			AutomationId = "CounterButton"
		};
		_counterButton.Clicked += OnCounterClicked;

		Content = new VerticalStackLayout
		{
			Padding = 30,
			Spacing = 18,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "iOS device breakpoint binding",
					FontSize = 24,
					HorizontalOptions = LayoutOptions.Center
				},
				_counterLabel,
				_counterButton,
				_handlerStatusLabel,
				_debuggerStatusLabel
			}
		};
	}

	void OnCounterClicked(object sender, EventArgs e)
	{
		_count++;
		_counterLabel.Text = $"Current count: {_count}";
		_counterButton.Text = _count == 1 ? "Clicked 1 time" : $"Clicked {_count} times";
		_handlerStatusLabel.Text = $"Handler invoked: {_count}";
		_debuggerStatusLabel.Text = $"Managed debugger attached: {Debugger.IsAttached}";
	}
}

