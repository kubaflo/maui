#if IOS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if !MACCATALYST
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.ScrollView)]
	[Category("Issue36801")]
	public class Issue36801 : ControlsHandlerTestBase
	{
		const double OffsetTolerance = 0.5;

		[Fact]
		public async Task ScrollToEndReachesInsetAwareMaximum()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var scrollToEndButton = new Button { Text = "Scroll to end" };
			var instructionLabel = new Label
			{
				Text = "Scroll to the bottom with a finger first, then capture that healthy endpoint.",
				FontSize = 14
			};
			var captureFingerEndButton = new Button { Text = "Capture finger end" };
			var returnToTopButton = new Button { Text = "Return to top" };
			var diagnosticsLabel = new Label
			{
				Text = "Finger endpoint not captured",
				FontSize = 13
			};
			var probe = new Label
			{
				Text = "BOTTOM PROBE",
				FontAttributes = FontAttributes.Bold,
				FontSize = 18
			};

			var stack = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 6,
				Children =
				{
					scrollToEndButton,
					instructionLabel
				}
			};

			for (int row = 1; row <= 40; row++)
				stack.Add(new Label { Text = $"Row {row:00}" });

			stack.Add(captureFingerEndButton);
			stack.Add(returnToTopButton);
			stack.Add(diagnosticsLabel);
			stack.Add(probe);

			var scrollView = new ScrollView { Content = stack };
			var page = new ContentPage
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = scrollView
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<ScrollViewHandler>(scrollView.Handler);
				var nativeScrollView = Assert.IsAssignableFrom<UIScrollView>(handler.PlatformView);
				var nativeProbe = Assert.IsAssignableFrom<UILabel>(probe.Handler.PlatformView);

				await AssertEventually(
					() => nativeScrollView.Window is not null &&
						nativeProbe.Window is not null &&
						nativeScrollView.Bounds.Height > 0 &&
						nativeScrollView.ContentSize.Height > nativeScrollView.Bounds.Height,
					timeout: 5000,
					message: "The attached ScrollView did not finish laying out scrollable content");

				Assert.Equal("BOTTOM PROBE", nativeProbe.Text);

				var adjustedInset = nativeScrollView.AdjustedContentInset;
				Assert.True(
					adjustedInset.Bottom > 0,
					$"The attached ScrollView requires a positive adjusted bottom inset; observed {adjustedInset.Bottom:F1}");

				var legacyMaximum = nativeScrollView.ContentSize.Height - nativeScrollView.Bounds.Height;
				var calibrationOffset = legacyMaximum / 2;

				await scrollView.ScrollToAsync(0, calibrationOffset, animated: false);
				await AssertEventually(
					() => Math.Abs(nativeScrollView.ContentOffset.Y - calibrationOffset) <= OffsetTolerance,
					timeout: 2000,
					message: $"ScrollToAsync did not reach the calibration offset {calibrationOffset:F1}");

				await scrollView.ScrollToAsync(0, 0, animated: false);
				await AssertEventually(
					() => nativeScrollView.ContentOffset.Y < calibrationOffset - OffsetTolerance,
					timeout: 2000,
					message: "ScrollToAsync did not return from the calibration offset");

				double scrolledOffset = -1;
				scrollView.Scrolled += (_, args) => scrolledOffset = args.ScrollY;

				await scrollView.ScrollToAsync(0, scrollView.ContentSize.Height, animated: false);
				await AssertEventually(
					() => scrolledOffset >= 0,
					timeout: 2000,
					message: "ScrollToAsync did not raise a post-trigger Scrolled event");

				var expectedMaximum = nativeScrollView.ContentSize.Height +
					nativeScrollView.AdjustedContentInset.Bottom -
					nativeScrollView.Bounds.Height;
				var observedOffset = nativeScrollView.ContentOffset.Y;

				Assert.True(
					Math.Abs(observedOffset - expectedMaximum) <= OffsetTolerance,
					$"ScrollToAsync end offset did not reach the inset-aware maximum: " +
					$"observed={observedOffset:F1}, expected={expectedMaximum:F1}, " +
					$"contentHeight={nativeScrollView.ContentSize.Height:F1}, " +
					$"boundsHeight={nativeScrollView.Bounds.Height:F1}, " +
					$"adjustedTop={nativeScrollView.AdjustedContentInset.Top:F1}, " +
					$"adjustedBottom={nativeScrollView.AdjustedContentInset.Bottom:F1}");
			});
		}
	}
#endif
}
#endif

