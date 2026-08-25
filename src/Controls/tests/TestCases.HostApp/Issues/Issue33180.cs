using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33180, "WebView scroll position is not updated after scrolling", PlatformAffected.iOS)]
public class Issue33180 : ContentPage
{
	readonly WebView _affectedWebView;
	readonly Label _readyStatusLabel;
	readonly Label _measurementSequenceLabel;
	readonly Label _scrollHostIdentityLabel;
	readonly Label _domOffsetLabel;
	readonly Label _nativeOffsetLabel;
	int _measurementSequence = -1;

	public Issue33180()
	{
		_affectedWebView = new WebView
		{
			AutomationId = "AffectedWebView",
			Source = new HtmlWebViewSource { Html = ScrollableHtml }
		};
		_affectedWebView.Navigated += OnWebViewNavigated;

		_readyStatusLabel = new Label
		{
			AutomationId = "WebViewReady",
			Text = "WebView loading"
		};
		_measurementSequenceLabel = new Label
		{
			AutomationId = "MeasurementSequence",
			Text = "Measurement sequence: -1"
		};
		_scrollHostIdentityLabel = new Label
		{
			AutomationId = "ScrollHostIdentity",
			Text = "Scroll host: unmeasured"
		};
		_domOffsetLabel = new Label
		{
			AutomationId = "DomOffset",
			Text = "-1"
		};
		_nativeOffsetLabel = new Label
		{
			AutomationId = "NativeOffset",
			Text = "-1"
		};

		var measureButton = new Button
		{
			AutomationId = "ShowScrollOffsetButton",
			Text = "Show ScrollOffset"
		};
		measureButton.Clicked += OnShowScrollOffsetClicked;

		var statusLayout = new VerticalStackLayout
		{
			Children =
			{
				_readyStatusLabel,
				_measurementSequenceLabel,
				_scrollHostIdentityLabel,
				_domOffsetLabel,
				_nativeOffsetLabel
			}
		};

		var grid = new Grid
		{
			Padding = 16,
			RowSpacing = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
		grid.Add(new Label { Text = "Scroll the WebView content, then show its scroll offset." });
		grid.Add(_affectedWebView, 0, 1);
		grid.Add(measureButton, 0, 2);
		grid.Add(statusLayout, 0, 3);
		grid.Add(new Label
		{
			AutomationId = "ExpectedResult",
			Text = "Native ContentOffset.Y should follow the scrolled web content."
		}, 0, 4);

		Content = grid;
	}

	void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
	{
		_readyStatusLabel.Text = "WebView ready";
	}

	async void OnShowScrollOffsetClicked(object sender, EventArgs e)
	{
		var scriptResult = await _affectedWebView.EvaluateJavaScriptAsync(
			"(() => { const host = document.getElementById('scrollHost'); return host ? host.id + '|' + host.scrollTop.toString() : 'missing|-1'; })();");
		var normalizedResult = scriptResult is null ? "missing|-1" : scriptResult.Trim('"');
		var resultParts = normalizedResult.Split('|');
		var hostIdentity = resultParts.Length > 0 ? resultParts[0] : "missing";
		var domOffset = resultParts.Length > 1
			&& double.TryParse(resultParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedOffset)
				? parsedOffset
				: -1;
		var nativeOffset = -1d;

#if IOS
		if (_affectedWebView.Handler?.PlatformView is WebKit.WKWebView platformWebView)
			nativeOffset = platformWebView.ScrollView.ContentOffset.Y;
#endif

		_measurementSequence++;
		_scrollHostIdentityLabel.Text = $"Scroll host: {hostIdentity}";
		_domOffsetLabel.Text = domOffset.ToString("R", CultureInfo.InvariantCulture);
		_nativeOffsetLabel.Text = nativeOffset.ToString("R", CultureInfo.InvariantCulture);
		_measurementSequenceLabel.Text = $"Measurement sequence: {_measurementSequence}";
	}

	const string ScrollableHtml = """
		<!doctype html>
		<html>
		<head>
			<meta name="viewport" content="width=device-width, initial-scale=1" />
			<style>
				html, body {
					height: 100%;
					margin: 0;
					overflow: hidden;
					font-family: sans-serif;
				}
				#scrollHost {
					height: 100%;
					overflow-y: auto;
					-webkit-overflow-scrolling: touch;
					box-sizing: border-box;
					padding: 16px;
				}
				.section {
					min-height: 180px;
					margin-bottom: 12px;
					padding: 16px;
					box-sizing: border-box;
					background: #e8f0fe;
				}
			</style>
		</head>
		<body>
			<div id="scrollHost">
				<h2>WebView scroll content</h2>
				<div class="section">Section 1</div>
				<div class="section">Section 2</div>
				<div class="section">Section 3</div>
				<div class="section">Section 4</div>
				<div class="section">Section 5</div>
				<div class="section">Section 6</div>
				<div class="section">Section 7</div>
				<div class="section">Section 8</div>
				<div class="section">Section 9</div>
				<div class="section">Section 10</div>
			</div>
		</body>
		</html>
		""";
}

