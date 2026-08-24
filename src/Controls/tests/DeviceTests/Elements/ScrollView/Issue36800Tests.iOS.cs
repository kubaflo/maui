#if IOS
#if !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36800")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36800Tests : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ContainerSafeAreaDoesNotCreatePhantomVerticalRange()
		{
			const double tolerance = 0.5;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
				});
			});

			var cleanContent = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 12,
				Children =
				{
					new Label { Text = "Small content", FontSize = 22 },
					new Button { Text = "Dump native state" },
					new Label { FontSize = 10 }
				}
			};
			var cleanScrollView = new ScrollView
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = cleanContent
			};
			var cleanPage = new ContentPage
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = cleanScrollView
			};
			double cleanReachableRange = double.NaN;

			await CreateHandlerAndAddToWindow<ScrollViewHandler>(cleanPage, async handler =>
			{
				var nativeScrollView = handler.PlatformView;
				await AssertHelpers.AssertEventually(
					() => nativeScrollView.Window is not null &&
						nativeScrollView.Bounds.Height > 0 &&
						nativeScrollView.ContentSize.Height > 0,
					message: "The clean ScrollView did not complete native layout.");

				var adjustedInset = nativeScrollView.AdjustedContentInset;
				cleanReachableRange = Math.Max(
					0,
					nativeScrollView.ContentSize.Height +
						adjustedInset.Top +
						adjustedInset.Bottom -
						nativeScrollView.Bounds.Height);
			});

			Assert.False(double.IsNaN(cleanReachableRange), "The clean ScrollView measurement callback did not run.");
			Assert.InRange(cleanReachableRange, 0, tolerance);

			var primaryLabel = new Label { Text = "Small content", FontSize = 22 };
			var dumpButton = new Button { Text = "Dump native state" };
			var diagnosticLabel = new Label { FontSize = 10 };
			var affectedContent = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 12,
				Children =
				{
					primaryLabel,
					dumpButton,
					diagnosticLabel
				}
			};
			var affectedScrollView = new ScrollView
			{
				SafeAreaEdges = SafeAreaEdges.Container,
				Content = affectedContent
			};
			var affectedPage = new ContentPage
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = affectedScrollView
			};

			bool callbackSeen = false;
			double measuredRange = double.NaN;
			double contentSizeHeight = double.NaN;
			double adjustedInsetTop = double.NaN;
			double adjustedInsetBottom = double.NaN;
			double boundsHeight = double.NaN;
			double managedContentHeight = double.NaN;
			double runtimeSafeArea = double.NaN;

			await CreateHandlerAndAddToWindow<ScrollViewHandler>(affectedPage, async handler =>
			{
				callbackSeen = true;
				Assert.Same(affectedScrollView, handler.VirtualView);
				Assert.Same(handler, affectedScrollView.Handler);
				Assert.Same(handler.PlatformView, affectedScrollView.Handler.PlatformView);
				Assert.Equal(UIScrollViewContentInsetAdjustmentBehavior.Always, handler.PlatformView.ContentInsetAdjustmentBehavior);
				Assert.Equal(SafeAreaEdges.None, affectedPage.SafeAreaEdges);
				Assert.Same(affectedScrollView, affectedPage.Content);
				Assert.Equal(SafeAreaEdges.Container, affectedScrollView.SafeAreaEdges);
				Assert.Same(affectedContent, affectedScrollView.Content);

				Assert.Equal(3, affectedContent.Children.Count);
				Assert.Same(primaryLabel, affectedContent.Children[0]);
				Assert.Same(dumpButton, affectedContent.Children[1]);
				Assert.Same(diagnosticLabel, affectedContent.Children[2]);
				Assert.Equal("Small content", primaryLabel.Text);
				Assert.Equal(22d, primaryLabel.FontSize);
				Assert.Equal("Dump native state", dumpButton.Text);
				Assert.Equal(10d, diagnosticLabel.FontSize);
				Assert.Equal(new Thickness(16), affectedContent.Padding);
				Assert.Equal(12d, affectedContent.Spacing);

				var nativeScrollView = handler.PlatformView;
				await AssertHelpers.AssertEventually(
					() => nativeScrollView.Window is not null &&
						nativeScrollView.Bounds.Height > 0 &&
						nativeScrollView.ContentSize.Height > 0,
					message: "The affected ScrollView did not complete native layout.");

				runtimeSafeArea = nativeScrollView.Window.SafeAreaInsets.Top + nativeScrollView.Window.SafeAreaInsets.Bottom;
				if (runtimeSafeArea > tolerance)
				{
					await AssertHelpers.AssertEventually(
						() => nativeScrollView.AdjustedContentInset.Top + nativeScrollView.AdjustedContentInset.Bottom > tolerance,
						message: "The runtime safe-area inset did not propagate to the affected ScrollView.");
				}

				contentSizeHeight = nativeScrollView.ContentSize.Height;
				adjustedInsetTop = nativeScrollView.AdjustedContentInset.Top;
				adjustedInsetBottom = nativeScrollView.AdjustedContentInset.Bottom;
				boundsHeight = nativeScrollView.Bounds.Height;
				managedContentHeight = affectedScrollView.ContentSize.Height;
				measuredRange = contentSizeHeight + adjustedInsetTop + adjustedInsetBottom - boundsHeight;
			});

			Assert.True(callbackSeen, "The affected ScrollView measurement callback did not run.");
			Assert.False(double.IsNaN(measuredRange), "The affected ScrollView range was not measured after attachment.");

			var verticalSafeArea = adjustedInsetTop + adjustedInsetBottom;
			Assert.True(runtimeSafeArea > tolerance, "The attached iOS window should provide a nonzero vertical safe-area inset.");
			Assert.True(verticalSafeArea > tolerance, "The affected ScrollView should receive the nonzero runtime safe-area inset.");
			Assert.True(
				managedContentHeight <= boundsHeight - verticalSafeArea + tolerance,
				$"Managed content should fit the inset-adjusted viewport; managed={managedContentHeight:F2}, viewport={boundsHeight - verticalSafeArea:F2}.");
			Assert.True(
				measuredRange <= tolerance,
				$"Safe-area ScrollView should have no reachable phantom vertical range; range={measuredRange:F2}, contentSize={contentSizeHeight:F2}, adjustedTop={adjustedInsetTop:F2}, adjustedBottom={adjustedInsetBottom:F2}, bounds={boundsHeight:F2}, tolerance={tolerance:F2}.");
		}
	}
}
#endif
#endif

