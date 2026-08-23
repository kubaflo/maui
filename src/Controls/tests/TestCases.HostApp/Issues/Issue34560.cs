#if IOS
using CoreGraphics;
using UIKit;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34560, "Switch iOS Liquid glass rendering issue", PlatformAffected.iOS)]
public class Issue34560 : ContentPage
{
	public Issue34560()
	{
		Label instructionLabel = new Label
		{
			Text = "Tap the default iOS switch to show its toggled-on rendering.",
			HorizontalTextAlignment = TextAlignment.Center,
		};

		Switch switchUnderTest = new Switch
		{
			AutomationId = "SwitchUnderTest",
			HorizontalOptions = LayoutOptions.Center,
		};

		Label callbackLabel = new Label
		{
			AutomationId = "CallbackLabel",
			Text = "Callback token: -1; native rendering equivalent: -1",
			HorizontalTextAlignment = TextAlignment.Center,
		};

		switchUnderTest.Toggled += (_, e) =>
		{
			callbackLabel.Text = $"Callback token: {(e.Value ? 1 : 0)}; native rendering equivalent: -1";
			Dispatcher.Dispatch(() =>
			{
				bool equivalent = HasPlatformDefaultTrackRendering(switchUnderTest);
				callbackLabel.Text = $"Callback token: {(e.Value ? 1 : 0)}; native rendering equivalent: {(equivalent ? 1 : 0)}";
			});
		};

		Grid grid = new Grid
		{
			Padding = new Thickness(24),
			RowSpacing = 24,
			VerticalOptions = LayoutOptions.Center,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
			},
		};

		grid.Add(instructionLabel, 0, 0);
		grid.Add(switchUnderTest, 0, 1);
		grid.Add(callbackLabel, 0, 2);
		Content = grid;
	}

	static bool HasPlatformDefaultTrackRendering(Switch mauiSwitch)
	{
		if (mauiSwitch.Handler?.PlatformView is not UISwitch platformSwitch)
			return false;

		using var defaultSwitch = new UISwitch(CGRect.Empty);
		defaultSwitch.SetState(true, false);
		defaultSwitch.SizeToFit();
		defaultSwitch.LayoutIfNeeded();

		return Math.Abs(platformSwitch.Bounds.Width - defaultSwitch.Bounds.Width) < 0.001
			&& Math.Abs(platformSwitch.Bounds.Height - defaultSwitch.Bounds.Height) < 0.001
			&& ColorsAreEquivalent(platformSwitch.OnTintColor, defaultSwitch.OnTintColor)
			&& ColorsAreEquivalent(GetTrackView(platformSwitch)?.BackgroundColor, GetTrackView(defaultSwitch)?.BackgroundColor);
	}

	static bool ColorsAreEquivalent(UIColor first, UIColor second)
	{
		if (first is null || second is null)
			return first is null && second is null;

		first.GetRGBA(out nfloat platformRed, out nfloat platformGreen, out nfloat platformBlue, out nfloat platformAlpha);
		second.GetRGBA(out nfloat defaultRed, out nfloat defaultGreen, out nfloat defaultBlue, out nfloat defaultAlpha);

		return Math.Abs(platformRed - defaultRed) < 0.001
			&& Math.Abs(platformGreen - defaultGreen) < 0.001
			&& Math.Abs(platformBlue - defaultBlue) < 0.001
			&& Math.Abs(platformAlpha - defaultAlpha) < 0.001;
	}

	static UIView GetTrackView(UISwitch uiSwitch)
		=> uiSwitch.Subviews.FirstOrDefault()?.Subviews.FirstOrDefault();
}
#endif

