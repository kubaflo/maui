#if MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using WebKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.WebViewsCollection)]
	[Category(TestCategory.WebView)]
	[Category("Issue36064")]
	public class Issue36064 : ControlsHandlerTestBase
	{
		const double ExpectedContentHeight = 320;
		const double HeightTolerance = 2;

		const string ChatHtml = """
			<!doctype html>
			<html>
			<head>
			<meta name="viewport" content="width=device-width, initial-scale=1">
			<style>
			html, body { margin: 0; padding: 0; height: auto; }
			.chat { box-sizing: border-box; height: 320px; padding: 12px; font: 16px sans-serif; background: white; }
			.message { box-sizing: border-box; width: 72%; margin-bottom: 12px; padding: 12px; border-radius: 10px; }
			.received { background: #eeeeee; }
			.sent { margin-left: 28%; background: #dceeff; }
			</style>
			</head>
			<body>
			<div class="chat">
			  <div class="message received">Hello! How can I help?</div>
			  <div class="message sent">Show the sizing behavior.</div>
			  <div class="message received">This content has a fixed intrinsic height.</div>
			  <div class="message sent">The WebView should fit this chat.</div>
			</div>
			</body>
			</html>
			""";

		[Fact]
		public async Task UnconstrainedWebViewMeasuresToHtmlContentHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<WebView, WebViewHandler>();
				});
			});

			await VerifyChatScene(ExpectedContentHeight);
			await VerifyChatScene(null);
		}

		async Task VerifyChatScene(double? explicitHeight)
		{
			var scene = CreateChatScene(explicitHeight);
			int navigationCount = -1;
			WebNavigationResult navigationResult = (WebNavigationResult)(-1);

			scene.ChatWebView.Navigated += (sender, args) =>
			{
				navigationCount = navigationCount < 0 ? 1 : navigationCount + 1;
				navigationResult = args.Result;
			};

			await CreateHandlerAndAddToWindow(scene.Page, async () =>
			{
				Assert.Same(scene.ChatWebView, scene.RootGrid.Children[4]);
				Assert.Equal(4, Grid.GetRow(scene.ChatWebView));
				Assert.Null(scene.ChatWebView.Source);
				Assert.Equal(-1d, scene.ChatWebView.WidthRequest);

				if (explicitHeight.HasValue)
					Assert.Equal(explicitHeight.Value, scene.ChatWebView.HeightRequest);
				else
					Assert.Equal(-1d, scene.ChatWebView.HeightRequest);

				var webViewHandler = Assert.IsType<WebViewHandler>(scene.ChatWebView.Handler);
				var nativeWebView = Assert.IsAssignableFrom<WKWebView>(webViewHandler.PlatformView);
				var buttonHandler = Assert.IsType<ButtonHandler>(scene.LoadChatButton.Handler);

				buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				Assert.IsType<HtmlWebViewSource>(scene.ChatWebView.Source);
				await AssertEventually(
					() => navigationCount == 1,
					timeout: 5000,
					message: $"Expected one successful WebView navigation, but navigation count was {navigationCount}.");
				Assert.Equal(1, navigationCount);
				Assert.Equal(WebNavigationResult.Success, navigationResult);

				string htmlMeasurement = await scene.ChatWebView.EvaluateJavaScriptAsync(
					"document.querySelectorAll('.chat > .message').length + '|' + Math.round(document.querySelector('.chat').getBoundingClientRect().height)");
				Assert.Equal("4|320", htmlMeasurement);

				double nativeHeight = await WaitForNativeHeightToSettle(nativeWebView);
				Assert.True(
					Math.Abs(nativeHeight - ExpectedContentHeight) <= HeightTolerance,
					$"WebView native height was {nativeHeight:F1}; HTML height was {htmlMeasurement}; expected height was {ExpectedContentHeight:F1}.");
			});
		}

		async Task<double> WaitForNativeHeightToSettle(WKWebView nativeWebView)
		{
			double nativeHeight = -1;
			double previousHeight = -2;
			int stableSamples = 0;

			await AssertEventually(
				async () =>
				{
					nativeHeight = await InvokeOnMainThreadAsync(() => (double)nativeWebView.Frame.Height);
					stableSamples = nativeHeight > 0 && Math.Abs(nativeHeight - previousHeight) < 0.1
						? stableSamples + 1
						: 0;
					previousHeight = nativeHeight;
					return stableSamples >= 2;
				},
				timeout: 5000,
				interval: 100,
				message: "Native WebView layout did not settle.");

			return nativeHeight;
		}

		static (ContentPage Page, Grid RootGrid, Button LoadChatButton, WebView ChatWebView) CreateChatScene(double? explicitHeight)
		{
			var titleLabel = new Label
			{
				Margin = new Thickness(16, 12),
				FontAttributes = FontAttributes.Bold,
				FontSize = 28,
				Text = "WebView Sizing Demo - Chatbot UI",
			};

			var headerGrid = new Grid
			{
				Padding = new Thickness(16, 12),
				BackgroundColor = Color.FromArgb("#1976D2"),
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
			};
			headerGrid.Add(new Label
			{
				Text = "WebView Sizing Issue Demo",
				TextColor = Colors.White,
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
			});
			headerGrid.Add(new Label
			{
				Text = "Chat interface with static HTML content",
				TextColor = Colors.White,
			}, row: 1);

			var resultLabel = new Label
			{
				Margin = new Thickness(16, 0),
				FontAttributes = FontAttributes.Bold,
				Text = "WebView sizing status",
			};

			var loadChatButton = new Button
			{
				Text = "Render static chat",
			};
			var measurementLabel = new Label
			{
				VerticalTextAlignment = TextAlignment.Center,
				Text = "Measured WebView height: pending",
			};
			var buttonGrid = new Grid
			{
				Margin = new Thickness(16, 0),
				ColumnSpacing = 12,
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Auto),
					new ColumnDefinition(GridLength.Star),
				},
			};
			buttonGrid.Add(loadChatButton);
			buttonGrid.Add(measurementLabel, column: 1);

			var chatWebView = new WebView
			{
				Margin = new Thickness(16, 0),
				BackgroundColor = Color.FromArgb("#FAFAFA"),
			};
			if (explicitHeight.HasValue)
				chatWebView.HeightRequest = explicitHeight.Value;

			loadChatButton.Clicked += (sender, args) =>
				chatWebView.Source = new HtmlWebViewSource { Html = ChatHtml };

			var footerLabel = new Label
			{
				Margin = new Thickness(16, 0, 16, 12),
				Text = "The WebView has no WidthRequest or HeightRequest and should size to its HTML content.",
			};

			var rootGrid = new Grid
			{
				RowSpacing = 10,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
			};
			rootGrid.Add(titleLabel);
			rootGrid.Add(headerGrid, row: 1);
			rootGrid.Add(resultLabel, row: 2);
			rootGrid.Add(buttonGrid, row: 3);
			rootGrid.Add(chatWebView, row: 4);
			rootGrid.Add(footerLabel, row: 5);

			return (
				new ContentPage
				{
					Title = "WebViewSize",
					Content = rootGrid,
				},
				rootGrid,
				loadChatButton,
				chatWebView);
		}
	}
}
#endif

