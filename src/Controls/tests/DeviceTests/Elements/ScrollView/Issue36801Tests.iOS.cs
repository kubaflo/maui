#if IOS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
#if !MACCATALYST
	[Category("Issue36801")]
	[Category(TestCategory.ScrollView)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36801 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ProgrammaticScrollToEndUsesAdjustedContentInset()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var triggerButton = new Button
			{
				Text = "Scroll to end"
			};
			var resultLabel = new Label
			{
				Text = "NO BUG:",
				BackgroundColor = Colors.White,
				FontAttributes = FontAttributes.Bold,
				ZIndex = 10
			};
			var metricsLabel = new Label
			{
				Text = "Waiting for programmatic scroll",
				BackgroundColor = Colors.White,
				ZIndex = 10
			};
			var bottomProbe = new Label
			{
				Text = "BOTTOM PROBE",
				FontSize = 18,
				FontAttributes = FontAttributes.Bold
			};
			var contentStack = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 6
			};

			contentStack.Add(triggerButton);
			contentStack.Add(resultLabel);
			contentStack.Add(metricsLabel);
			for (var index = 1; index <= 40; index++)
			{
				contentStack.Add(new Label
				{
					Text = $"Filler row {index}",
					FontSize = 18
				});
			}
			contentStack.Add(bottomProbe);

			var scrollView = new ScrollView
			{
				Content = contentStack
			};
			var page = new ContentPage
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = scrollView
			};

			var triggerCompleted = new TaskCompletionSource<bool>();
			triggerButton.Clicked += async (sender, args) =>
			{
				await scrollView.ScrollToAsync(0, scrollView.ContentSize.Height, false);
				triggerCompleted.TrySetResult(true);
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var nativeScrollView = Assert.IsAssignableFrom<UIScrollView>(scrollView.Handler.PlatformView);

				await AssertHelpers.AssertEventually(
					() => nativeScrollView.Bounds.Height > 0
						&& nativeScrollView.Bounds.Width > 0
						&& nativeScrollView.ContentSize.Height > nativeScrollView.Bounds.Height
						&& nativeScrollView.ContentSize.Width > 0
						&& nativeScrollView.AdjustedContentInset.Bottom > 0,
					timeout: 5000,
					message: "The real iOS window did not produce scrollable content with a bottom adjusted inset.");

				Assert.Equal(UIScrollViewContentInsetAdjustmentBehavior.Automatic, nativeScrollView.ContentInsetAdjustmentBehavior);
				Assert.Equal(SafeAreaEdges.None, page.SafeAreaEdges);
				Assert.Equal(new Thickness(16), contentStack.Padding);
				Assert.Equal(6, contentStack.Spacing);
				Assert.Equal(44, contentStack.Children.Count);
				Assert.Same(bottomProbe, contentStack.Children[contentStack.Children.Count - 1]);
				Assert.Equal("BOTTOM PROBE", bottomProbe.Text);
				Assert.Equal(18, bottomProbe.FontSize);
				Assert.Equal(FontAttributes.Bold, bottomProbe.FontAttributes);

				var nativeStack = Assert.IsAssignableFrom<UIView>(contentStack.Handler.PlatformView);
				var nativeProbe = Assert.IsAssignableFrom<UIView>(bottomProbe.Handler.PlatformView);
				Assert.Same(nativeStack, nativeProbe.Superview);
				Assert.True(nativeProbe.Frame.Y > 0);

				var observedOffset = double.NaN;
				var postTriggerScroll = new TaskCompletionSource<bool>();
				var triggerStarted = false;
				scrollView.Scrolled += (sender, args) =>
				{
					if (triggerStarted)
					{
						observedOffset = args.ScrollY;
						postTriggerScroll.TrySetResult(true);
					}
				};

				triggerStarted = true;
				Assert.IsAssignableFrom<UIButton>(triggerButton.Handler.PlatformView)
					.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertHelpers.AssertEventually(
					() => postTriggerScroll.Task.IsCompleted,
					timeout: 5000,
					message: "The post-trigger ScrollView.Scrolled callback did not occur.");
				Assert.True(postTriggerScroll.Task.IsCompleted, "The post-trigger ScrollView.Scrolled callback must complete.");
				Assert.False(double.IsNaN(observedOffset), "The post-trigger scroll offset sentinel must be replaced.");

				await AssertHelpers.AssertEventually(
					() => triggerCompleted.Task.IsCompleted,
					timeout: 5000,
					message: "The button action did not complete ScrollToAsync.");
				Assert.True(triggerCompleted.Task.IsCompleted, "The button action must complete ScrollToAsync.");

				var expectedOffset = nativeScrollView.ContentSize.Height
					+ nativeScrollView.AdjustedContentInset.Bottom
					- nativeScrollView.Bounds.Height;
				var actualOffset = nativeScrollView.ContentOffset.Y;

				Assert.True(
					Math.Abs(actualOffset - expectedOffset) <= 0.5,
					$"Issue36801 programmatic end offset: expected {expectedOffset:F1}, actual {actualOffset:F1}; "
					+ $"content height {nativeScrollView.ContentSize.Height:F1}, bottom inset {nativeScrollView.AdjustedContentInset.Bottom:F1}, "
					+ $"bounds height {nativeScrollView.Bounds.Height:F1}.");
			});
		}
	}
#endif
}
#endif
