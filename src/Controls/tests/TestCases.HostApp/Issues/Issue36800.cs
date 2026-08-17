namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 36800, "ScrollView reserves the safe area twice on iOS", PlatformAffected.iOS)]
public class Issue36800 : ContentPage
{
	const string Sentinel = "SENTINEL: diagnostic not run";
	const string PassResult = "PASS: Inset-aware native ScrollView range is non-positive.";
	const string FailResult = "FAIL: Inset-aware native ScrollView range is positive.";

	public Issue36800()
	{
		SafeAreaEdges = SafeAreaEdges.None;

		var resultLabel = new Label
		{
			Text = Sentinel,
			FontSize = 10,
			AutomationId = "Issue36800Result"
		};

		var scrollView = new ScrollView
		{
			SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container),
			AutomationId = "Issue36800Scroll"
		};

		var diagnosticButton = new Button
		{
			Text = "Dump native state",
			AutomationId = "Issue36800DumpButton"
		};

		diagnosticButton.Clicked += (_, _) =>
		{
#if IOS
			if (scrollView.Handler is not Microsoft.Maui.Handlers.ScrollViewHandler scrollViewHandler
				|| scrollViewHandler.PlatformView is not Microsoft.Maui.Platform.MauiScrollView nativeScrollView)
			{
				resultLabel.Text = "SETUP FAILED: Native ScrollView is unavailable.";
				return;
			}

			if (nativeScrollView.Window is null)
			{
				resultLabel.Text = "SETUP FAILED: Native ScrollView is not attached to a window.";
				return;
			}

			if (nativeScrollView.Bounds.Width <= 0 || nativeScrollView.Bounds.Height <= 0)
			{
				resultLabel.Text = "SETUP FAILED: Native ScrollView has empty bounds.";
				return;
			}

			if (nativeScrollView.Bounds.Height <= nativeScrollView.Bounds.Width)
			{
				resultLabel.Text = "SETUP FAILED: Native ScrollView is not in portrait orientation.";
				return;
			}

			var adjustedInsets = nativeScrollView.AdjustedContentInset;
			if (adjustedInsets.Top + adjustedInsets.Bottom <= 0)
			{
				resultLabel.Text = "SETUP FAILED: Native safe-area insets are zero.";
				return;
			}

			var verticalRange = nativeScrollView.ContentSize.Height
				+ adjustedInsets.Top
				+ adjustedInsets.Bottom
				- nativeScrollView.Bounds.Height;

			resultLabel.Text = verticalRange <= 0 ? PassResult : FailResult;
#else
			resultLabel.Text = "SETUP FAILED: This scenario requires iOS.";
#endif
		};

		scrollView.Content = new VerticalStackLayout
		{
			Padding = 16,
			Spacing = 12,
			Children =
			{
				new Label
				{
					Text = "Small content",
					FontSize = 22,
					AutomationId = "Issue36800Content"
				},
				diagnosticButton,
				resultLabel
			}
		};

		Content = scrollView;
	}
}
