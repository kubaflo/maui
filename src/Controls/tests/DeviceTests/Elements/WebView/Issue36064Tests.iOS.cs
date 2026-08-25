using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using WebKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Collection(ControlsHandlerTestBase.WebViewsCollection)]
	[Category(TestCategory.WebView)]
	[Category("Issue36064")]
	public class Issue36064 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task WebViewsWithoutSizeRequestsMeasureHtmlContent()
		{
			const double expectedWidth = 260;
			const double expectedHeight = 80;
			const double tolerance = 0.5;
			const int expectedWebViewCount = 5;

			static HtmlWebViewSource CreateSource(string text, string background, string foreground)
			{
				string html = "<!DOCTYPE html>"
					+ "<html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">"
					+ "<style>"
					+ "html, body { margin: 0; padding: 0; width: 260px; height: 80px; overflow: hidden; }"
					+ ".bubble { box-sizing: border-box; width: 260px; height: 80px; padding: 14px;"
					+ " border-radius: 12px; font: 16px sans-serif; background: " + background
					+ "; color: " + foreground + "; }"
					+ "</style></head><body><div class=\"bubble\">" + text + "</div></body></html>";

				return new HtmlWebViewSource { Html = html };
			}

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<WebView, WebViewHandler>();
				});
			});

			var oracleNavigated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var oracleWebView = new WebView
			{
				Source = CreateSource("Oracle", "#EEEEEE", "#202020"),
				WidthRequest = expectedWidth,
				HeightRequest = expectedHeight
			};
			oracleWebView.Navigated += (_, _) => oracleNavigated.TrySetResult();

			await CreateHandlerAndAddToWindow(new ContentPage { Content = oracleWebView }, async () =>
			{
				await oracleNavigated.Task.WaitAsync(TimeSpan.FromSeconds(10));

				Assert.NotNull(oracleWebView.Handler);
				var nativeOracle = Assert.IsAssignableFrom<WKWebView>(oracleWebView.Handler.PlatformView);
				Assert.NotNull(nativeOracle.Window);
				Assert.InRange((double)nativeOracle.Frame.Width, expectedWidth - tolerance, expectedWidth + tolerance);
				Assert.InRange((double)nativeOracle.Frame.Height, expectedHeight - tolerance, expectedHeight + tolerance);
			});

			var messages = new[]
			{
				new ChatMessage { Alignment = LayoutOptions.Start, Source = CreateSource("Hello! This message is rendered by HTML.", "#EEEEEE", "#202020") },
				new ChatMessage { Alignment = LayoutOptions.End, Source = CreateSource("The WebView should size to this bubble.", "#DCEEFF", "#174A7E") },
				new ChatMessage { Alignment = LayoutOptions.Start, Source = CreateSource("No WidthRequest or HeightRequest is set.", "#EEEEEE", "#202020") },
				new ChatMessage { Alignment = LayoutOptions.End, Source = CreateSource("Its content requests 260 by 80 pixels.", "#DCEEFF", "#174A7E") },
				new ChatMessage { Alignment = LayoutOptions.Start, Source = CreateSource("All five bubbles should remain readable.", "#EEEEEE", "#202020") }
			};

			int navigatedCount = -1;
			var navigatedWebViews = new HashSet<WebView>();
			var collectionView = new CollectionView
			{
				ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
				ItemsSource = messages,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 10 },
				ItemTemplate = new DataTemplate(() =>
				{
					var webView = new WebView { Margin = 4 };
					webView.SetBinding(WebView.HorizontalOptionsProperty, nameof(ChatMessage.Alignment));
					webView.SetBinding(WebView.SourceProperty, nameof(ChatMessage.Source));
					webView.Navigated += (_, _) =>
					{
						if (navigatedWebViews.Add(webView))
							Volatile.Write(ref navigatedCount, navigatedWebViews.Count);
					};
					return webView;
				})
			};

			var header = new VerticalStackLayout
			{
				Padding = 12,
				BackgroundColor = Color.FromArgb("#1976D2"),
				Spacing = 2,
				Children =
				{
					new Label { FontAttributes = FontAttributes.Bold, FontSize = 18, Text = "WebView Sizing Issue Demo", TextColor = Colors.White },
					new Label { Text = "Each HTML bubble requests 260 x 80 CSS pixels", TextColor = Colors.White }
				}
			};
			var status = new VerticalStackLayout
			{
				Spacing = 4,
				Children =
				{
					new Label { Text = "Waiting for WebViews to navigate" },
					new Label { Text = "Measured: pending" },
					new Label { FontAttributes = FontAttributes.Bold, Text = "WebView frame measurements" }
				}
			};
			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				}
			};
			grid.Add(new Label { FontAttributes = FontAttributes.Bold, FontSize = 24, Text = "WebView Sizing Demo - Chatbot UI" }, 0, 0);
			grid.Add(header, 0, 1);
			grid.Add(collectionView, 0, 2);
			grid.Add(status, 0, 3);
			grid.Add(new Button { IsEnabled = false, Text = "Check WebView sizing" }, 0, 4);

			await CreateHandlerAndAddToWindow(new ContentPage { Content = grid }, async () =>
			{
				await AssertEventually(
					() => Volatile.Read(ref navigatedCount) == expectedWebViewCount,
					timeout: 10000,
					message: "Five distinct WebViews did not report Navigated.");

				Assert.True(double.IsFinite(grid.Width) && grid.Width >= expectedWidth + 8, $"Host content width was {grid.Width:F2}.");

				var realizedWebViews = collectionView
					.GetVisualTreeDescendants()
					.OfType<WebView>()
					.Distinct()
					.ToArray();
				Assert.Equal(expectedWebViewCount, realizedWebViews.Length);

				for (int i = 0; i < messages.Length; i++)
				{
					var webView = Assert.Single(realizedWebViews.Where(view => ReferenceEquals(view.BindingContext, messages[i])));
					Assert.Same(messages[i].Source, webView.Source);
					Assert.NotNull(webView.Handler);

					var nativeWebView = Assert.IsAssignableFrom<WKWebView>(webView.Handler.PlatformView);
					Assert.NotNull(nativeWebView.Window);

					double width = nativeWebView.Frame.Width;
					double height = nativeWebView.Frame.Height;
					Assert.True(
						width >= expectedWidth - tolerance && height >= expectedHeight - tolerance,
						$"Issue36064 WebView native size mismatch for item {i + 1}: observed {width:F2}x{height:F2}, expected at least {expectedWidth:F2}x{expectedHeight:F2} after {navigatedCount} Navigated callbacks.");
				}
			});
		}

		sealed class ChatMessage
		{
			public LayoutOptions Alignment { get; set; }

			public HtmlWebViewSource Source { get; set; }
		}
	}
#endif
}

