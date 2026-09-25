#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using WebKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.WebView)]
	[Category("Issue36064")]
	[Collection(WebViewsCollection)]
	public class Issue36064 : ControlsHandlerTestBase
	{
		const double ExpectedContentHeight = 120;
		const double HeightTolerance = 1;

		[Fact]
		public async Task DefaultSizedWebViewsMeasureTheirHtmlContent()
		{
			SetupBuilder();

			await VerifyExplicitHeightProbe();

			const string firstMessage = "Hello from the first chat message.";
			const string secondMessage = "This message has enough content to require its declared HTML height.";
			const string thirdMessage = "The WebView should report that content height to its parent.";

			var firstWebView = CreateMessageWebView(firstMessage, "#E3F2FD", "FirstChatWebView");
			var secondWebView = CreateMessageWebView(secondMessage, "#F5F5F5", "SecondChatWebView");
			var thirdWebView = CreateMessageWebView(thirdMessage, "#E8F5E9", "ThirdChatWebView");
			var webViews = new[] { firstWebView, secondWebView, thirdWebView };
			var expectedMessages = new[] { firstMessage, secondMessage, thirdMessage };
			var readyStatusLabel = new Label
			{
				AutomationId = "ReadyStatus",
				Text = "Loading HTML"
			};
			var measurementLabel = new Label
			{
				AutomationId = "Measurement",
				Text = "Measured heights: not checked"
			};
			var sizingStatusLabel = new Label
			{
				AutomationId = "ResultStatus",
				Text = "Sizing not sampled",
				FontAttributes = FontAttributes.Bold
			};
			var checkSizingButton = new Button
			{
				AutomationId = "CheckSizing",
				Text = "Check WebView sizing",
				IsEnabled = false
			};

			int navigationCount = -1;
			var navigationResults = new Dictionary<WebView, WebNavigationResult>();
			var allNavigated = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			foreach (var webView in webViews)
			{
				webView.Navigated += (sender, args) =>
				{
					var navigatedWebView = Assert.IsType<WebView>(sender);
					if (navigationResults.TryAdd(navigatedWebView, args.Result) &&
						Interlocked.Increment(ref navigationCount) == webViews.Length - 1)
					{
						navigatedWebView.Dispatcher.Dispatch(() =>
						{
							readyStatusLabel.Text = "HTML loaded";
							checkSizingButton.IsEnabled = true;
							allNavigated.TrySetResult(true);
						});
					}
				};
			}

			var messages = new VerticalStackLayout
			{
				Spacing = 8,
				Children =
				{
					new Label
					{
						Text = "Static chat rendered by default-sized WebViews",
						FontAttributes = FontAttributes.Bold,
						FontSize = 18
					},
					firstWebView,
					secondWebView,
					thirdWebView
				}
			};

			var status = new VerticalStackLayout
			{
				Spacing = 8,
				Children =
				{
					readyStatusLabel,
					measurementLabel,
					sizingStatusLabel,
					checkSizingButton
				}
			};

			var grid = new Grid
			{
				Padding = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(messages);
			grid.Add(status);
			Grid.SetRow(status, 1);

			var page = new ContentPage
			{
				Title = "WebView sizing",
				Content = grid
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(
				page,
				async _ =>
				{
					Assert.All(webViews, webView => Assert.NotNull(webView.Handler));

					Assert.True(await allNavigated.Task.WaitAsync(TimeSpan.FromSeconds(10)));
					Assert.Equal(webViews.Length - 1, navigationCount);
					Assert.Equal(webViews.Length, navigationResults.Count);
					Assert.All(webViews, webView => Assert.Equal(WebNavigationResult.Success, navigationResults[webView]));
					Assert.Equal("HTML loaded", readyStatusLabel.Text);
					Assert.True(checkSizingButton.IsEnabled);

					var nativeWebViews = new WKWebView[webViews.Length];
					var domHeights = new double[webViews.Length];

					for (int i = 0; i < webViews.Length; i++)
					{
						var handler = Assert.IsType<WebViewHandler>(webViews[i].Handler);
						var nativeWebView = Assert.IsAssignableFrom<WKWebView>(handler.PlatformView);
						nativeWebViews[i] = nativeWebView;

						await WaitForStableFrame(nativeWebView);
						Assert.NotNull(nativeWebView.Window);

						var actualMessage = await webViews[i].EvaluateJavaScriptAsync(
							"document.getElementById('message').textContent");
						Assert.Equal(expectedMessages[i], actualMessage);

						var domHeight = await webViews[i].EvaluateJavaScriptAsync(
							"parseFloat(getComputedStyle(document.getElementById('message')).height)");
						domHeights[i] = Convert.ToDouble(domHeight, CultureInfo.InvariantCulture);
					}

					Assert.Same(nativeWebViews[0].Superview, nativeWebViews[1].Superview);
					Assert.Same(nativeWebViews[0].Superview, nativeWebViews[2].Superview);
					Assert.True(nativeWebViews[0].Frame.Y < nativeWebViews[1].Frame.Y);
					Assert.True(nativeWebViews[1].Frame.Y < nativeWebViews[2].Frame.Y);

					for (int i = 0; i < webViews.Length; i++)
					{
						Assert.Equal(ExpectedContentHeight, domHeights[i]);

						var frameHeight = nativeWebViews[i].Frame.Height;
						Assert.True(
							frameHeight >= ExpectedContentHeight - HeightTolerance,
							$"Issue 36064 WebView native frame height was shorter than its HTML content: " +
							$"message '{expectedMessages[i]}', frame height {frameHeight:F2}, " +
							$"expected at least {ExpectedContentHeight - HeightTolerance:F2}.");
					}
				},
				MauiContext,
				TimeSpan.FromSeconds(30));
		}

		void SetupBuilder()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<WebView, WebViewHandler>();
				});
			});
		}

		async Task VerifyExplicitHeightProbe()
		{
			const string message = "Hello from the first chat message.";
			var webView = CreateMessageWebView(message, "#E3F2FD", "ExplicitHeightProbe");
			webView.HeightRequest = ExpectedContentHeight;

			var navigated = new TaskCompletionSource<WebNavigationResult>(
				TaskCreationOptions.RunContinuationsAsynchronously);
			webView.Navigated += (_, args) => navigated.TrySetResult(args.Result);

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Children = { webView }
				}
			};
			await CreateHandlerAndAddToWindow<IWindowHandler>(
				page,
				async _ =>
				{
					Assert.Equal(
						WebNavigationResult.Success,
						await navigated.Task.WaitAsync(TimeSpan.FromSeconds(10)));

					var handler = Assert.IsType<WebViewHandler>(webView.Handler);
					var nativeWebView = Assert.IsAssignableFrom<WKWebView>(handler.PlatformView);
					await WaitForStableFrame(nativeWebView);

					Assert.NotNull(nativeWebView.Window);
					Assert.Equal(
						message,
						await webView.EvaluateJavaScriptAsync(
							"document.getElementById('message').textContent"));

					var domHeight = await webView.EvaluateJavaScriptAsync(
						"parseFloat(getComputedStyle(document.getElementById('message')).height)");
					Assert.Equal(
						ExpectedContentHeight,
						Convert.ToDouble(domHeight, CultureInfo.InvariantCulture));
					Assert.InRange(
						nativeWebView.Frame.Height,
						ExpectedContentHeight - HeightTolerance,
						ExpectedContentHeight + HeightTolerance);
				},
				MauiContext,
				TimeSpan.FromSeconds(20));
		}

		static WebView CreateMessageWebView(string message, string background, string automationId)
		{
			var html =
				"<html><head><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>" +
				"<body style='margin:0;padding:0;'>" +
				$"<div id='message' style='box-sizing:border-box;height:{ExpectedContentHeight}px;" +
				$"padding:16px;background:{background};font-size:18px;'>{message}</div>" +
				"</body></html>";

			return new WebView
			{
				AutomationId = automationId,
				Source = new HtmlWebViewSource { Html = html }
			};
		}

		static Task WaitForStableFrame(WKWebView webView)
		{
			double previousHeight = -1;
			int stablePolls = 0;

			return AssertHelpers.AssertEventually(
				() =>
				{
					if (webView.Window is null ||
						webView.IsLoading ||
						webView.Frame.Width <= 0 ||
						webView.Frame.Height < 0)
					{
						previousHeight = -1;
						stablePolls = 0;
						return false;
					}

					double currentHeight = webView.Frame.Height;
					if (Math.Abs(currentHeight - previousHeight) <= 0.01)
						stablePolls++;
					else
						stablePolls = 0;

					previousHeight = currentHeight;
					return stablePolls >= 2;
				},
				timeout: 5000,
				interval: 100,
				message: "WKWebView did not attach and reach a stable native frame.");
		}
	}
}
#endif

