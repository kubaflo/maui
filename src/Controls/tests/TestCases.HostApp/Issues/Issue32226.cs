namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32226, "TapGestureRecognizer is suppressed by Android native Touch with Handled false", PlatformAffected.Android)]
public class Issue32226 : ContentPage
{
	public Issue32226()
	{
		int nativeTouchCount = 0;

		var nativeTouchStatus = new Label
		{
			Text = "Touch received: 0",
			AutomationId = "NativeTouchStatus"
		};

		var tapTarget = new Label
		{
			Text = "Click me (Label TapGestureRecognizer)",
			AutomationId = "TapTarget"
		};

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += (_, _) => tapTarget.Text = "TapGestureRecognizer invoked";
		tapTarget.GestureRecognizers.Add(tapGestureRecognizer);

#if ANDROID
		tapTarget.HandlerChanged += (_, _) =>
		{
			if (tapTarget.Handler?.PlatformView is not global::Android.Views.View platformTarget)
				return;

			platformTarget.Touch += (_, touchEvent) =>
			{
				if (touchEvent.Event?.ActionMasked == global::Android.Views.MotionEventActions.Down)
				{
					nativeTouchCount++;
					nativeTouchStatus.Text = $"Touch received: {nativeTouchCount}";
				}

				touchEvent.Handled = false;
			};
		};
#endif

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label { Text = "Tap the label once." },
				tapTarget,
				nativeTouchStatus
			}
		};
	}
}

