namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33180, "WebView scroll position is not updated after scrolling", PlatformAffected.iOS)]
public partial class Issue33180 : ContentPage
{
#if IOS
	bool _isObservingNativeScroll;
#endif

	public Issue33180()
	{
		InitializeComponent();

		WebContent.Source = new HtmlWebViewSource
		{
			Html = """
				<!doctype html>
				<html>
				<body>
					<h1>WebView scroll content</h1>
					<p>Section 1</p><br><br><br><br><br><br>
					<h2>Section 2</h2>
					<p>The page continues below the visible WebView.</p><br><br><br><br><br><br>
					<h2>Section 3</h2>
					<p>Scroll through this content before reading the native offset.</p><br><br><br><br><br><br>
					<h2>Section 4</h2>
					<p>End of WebView scroll content.</p>
				</body>
				</html>
				"""
		};
	}

	void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
	{
#if IOS
		if (WebContent.Source is HtmlWebViewSource &&
			WebContent.Handler is Microsoft.Maui.Handlers.WebViewHandler webViewHandler &&
			webViewHandler.PlatformView is WebKit.WKWebView nativeWebView)
		{
			var initialOffset = (double)nativeWebView.ScrollView.ContentOffset.Y;
			InitialOffsetLabel.Text = $"Initial ContentOffset.Y: {FormatOffset(initialOffset)}";

			if (!_isObservingNativeScroll)
			{
				nativeWebView.ScrollView.Scrolled += OnNativeScrolled;
				_isObservingNativeScroll = true;
			}
		}
#endif
	}

#if IOS
	void OnNativeScrolled(object sender, EventArgs e)
	{
		if (sender is UIKit.UIScrollView scrollView && (scrollView.Dragging || scrollView.Decelerating))
			ScrollInputLabel.Text = "Scroll input: received";
	}
#endif

	void OnShowScrollOffsetClicked(object sender, EventArgs e)
	{
#if IOS
		if (WebContent.Handler?.PlatformView is WebKit.WKWebView nativeWebView)
		{
			var reportedOffset = (double)nativeWebView.ScrollView.ContentOffset.Y;
			ReportedOffsetLabel.Text = $"Reported ContentOffset.Y: {FormatOffset(reportedOffset)}";
			ResultLabel.Text = reportedOffset == 0 ? "BUG REPRODUCED:" : "NO BUG:";
		}
#endif
	}

	static string FormatOffset(double value) =>
		value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
