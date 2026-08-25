namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 29575, "iOS WebView cancellation after an awaited Navigating handler", PlatformAffected.iOS)]
public partial class Issue29575 : ContentPage
{
	TaskCompletionSource<bool> _returnSignal = new();
	bool _initialHashRequested;

	public Issue29575()
	{
		InitializeComponent();

		IssueWebView.Navigating += OnWebViewNavigating;
		IssueWebView.Navigated += OnWebViewNavigated;
		IssueWebView.Source = new HtmlWebViewSource
		{
			Html = """
				<!doctype html>
				<html>
				<head>
					<meta name="viewport" content="width=device-width, initial-scale=1">
					<style>
						html, body { height: 100%; margin: 0; font-family: sans-serif; }
						#account { display: none; height: 100%; text-align: center; padding-top: 90px; box-sizing: border-box; }
						#account:target { display: block; }
						#account:target + #search { display: none; }
						#search { height: 100%; display: flex; flex-direction: column; align-items: center; justify-content: center; }
						a { display: block; padding: 35px 90px; font-size: 24px; }
					</style>
				</head>
				<body>
					<section id="account"><h1>Google Account</h1><p>Account destination loaded in the WebView.</p></section>
					<section id="search"><h1>Google Search</h1><a href="#account">Sign In</a></section>
				</body>
				</html>
				"""
		};
	}

	async void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
	{
		if (_initialHashRequested)
			return;

		_initialHashRequested = true;
		var hash = await IssueWebView.EvaluateJavaScriptAsync("document.location.hash");
		InitialHashLabel.Text = $"Initial hash: {DisplayHash(hash)}";
	}

	async void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
	{
		if (!e.Url.Contains("#account", StringComparison.Ordinal))
			return;

		_returnSignal = new TaskCompletionSource<bool>();
		TriggerStatusLabel.Text = "Navigation received";
		ReturnButton.IsVisible = true;

		await _returnSignal.Task;
		e.Cancel = true;
		TriggerStatusLabel.Text = "Cancel set";
		CheckButton.IsVisible = true;
	}

	void OnReturnClicked(object sender, EventArgs e)
	{
		ReturnButton.Text = "Returned to app";
		ReturnButton.IsEnabled = false;
		_returnSignal.TrySetResult(true);
	}

	async void OnCheckClicked(object sender, EventArgs e)
	{
		var hash = await IssueWebView.EvaluateJavaScriptAsync("document.location.hash");
		ResultLabel.Text = DisplayHash(hash);
		MeasurementStatusLabel.Text = "Hash measured";
	}

	static string DisplayHash(string hash) =>
		string.IsNullOrEmpty(hash) ? "<empty>" : hash;
}
