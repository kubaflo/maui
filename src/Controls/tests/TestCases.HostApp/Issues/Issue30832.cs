#if IOS
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30832, "TapGestureRecognizer does not activate after a long press", PlatformAffected.iOS)]
public class Issue30832 : ContentPage
{
	int _tapCount;

	public Issue30832()
	{
		var tapCountLabel = new Label
		{
			AutomationId = "Issue30832TapCount",
			FontSize = 18,
			Text = "Tap count: 0"
		};
		var inputStateLabel = new Label
		{
			AutomationId = "Issue30832InputState",
			FontSize = 18,
			Text = "Target input: -1"
		};
		var pointerPressedLabel = new Label
		{
			AutomationId = "Issue30832PointerPressed",
			FontSize = 18,
			Text = "Pointer pressed: 0"
		};
		var target = new TapTargetLayout30832
		{
			AutomationId = "Issue30832Target",
			BackgroundColor = Color.FromArgb("#DCEEFF"),
			HeightRequest = 140,
			HorizontalOptions = LayoutOptions.Fill
		};

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (_, _) =>
		{
			_tapCount++;
			tapCountLabel.Text = $"Tap count: {_tapCount}";
		};
		target.GestureRecognizers.Add(tapGestureRecognizer);

		var pointerGestureRecognizer = new PointerGestureRecognizer();
		pointerGestureRecognizer.PointerPressed += (_, _) =>
		{
			pointerPressedLabel.Text = "Pointer pressed: 1";
			inputStateLabel.Text = "Target input: 1";
		};
		pointerGestureRecognizer.PointerReleased += (_, _) =>
		{
			inputStateLabel.Text = "Target input: 2";
		};
		target.GestureRecognizers.Add(pointerGestureRecognizer);

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = "iOS long-press tap test"
					},
					new Label
					{
						Text = "Press and hold the blue item for more than two seconds, then release."
					},
					target,
					tapCountLabel,
					inputStateLabel,
					pointerPressedLabel
				}
			}
		};
	}
}

abstract class ControlLayout30832 : Grid
{
}

sealed class TapTargetLayout30832 : ControlLayout30832
{
	readonly Label _caption;

	public TapTargetLayout30832()
	{
		_caption = new Label
		{
			FontSize = 20,
			HorizontalTextAlignment = TextAlignment.Center,
			Text = "Tap to Expand/Collapse",
			VerticalTextAlignment = TextAlignment.Center
		};
		Children.Add(_caption);
	}
}
#endif

