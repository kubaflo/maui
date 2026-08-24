#if MACCATALYST
using System;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Layout)]
	[Category("Issue17673")]
	public class Issue17673 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ProportionalChildrenPreserveNaturalAbsoluteLayoutHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
				});
			});

			var cleanBottom = new Button { Text = "Bottom Button" };
			var cleanTop = new Button { Text = "Click Me!", InputTransparent = false };
			var cleanLayout = new AbsoluteLayout
			{
				cleanBottom,
				cleanTop
			};

			var autoSizeBounds = new Rect(0, 0, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize);
			AbsoluteLayout.SetLayoutBounds(cleanBottom, autoSizeBounds);
			AbsoluteLayout.SetLayoutBounds(cleanTop, autoSizeBounds);
			AbsoluteLayout.SetLayoutFlags(cleanBottom, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutFlags(cleanTop, AbsoluteLayoutFlags.None);

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					cleanLayout
				}
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = stack
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(() => cleanLayout.Height > 0);

				Assert.NotNull(cleanLayout.Handler);
				Assert.NotNull(cleanBottom.Handler);
				Assert.NotNull(cleanTop.Handler);
				var cleanLayoutView = Assert.IsType<LayoutView>(cleanLayout.Handler.PlatformView);
				var cleanBottomView = Assert.IsType<UIButton>(cleanBottom.Handler.PlatformView);
				var cleanTopView = Assert.IsType<UIButton>(cleanTop.Handler.PlatformView);
				var cleanBottomFit = cleanBottomView.SizeThatFits(CGSize.Empty);
				var cleanTopFit = cleanTopView.SizeThatFits(CGSize.Empty);
				var cleanExpectedHeight = Math.Max(cleanBottomFit.Height, cleanTopFit.Height);
				var cleanBottomFrame = cleanBottomView.ConvertRectToView(cleanBottomView.Bounds, cleanLayoutView);
				var cleanTopFrame = cleanTopView.ConvertRectToView(cleanTopView.Bounds, cleanLayoutView);
				const double tolerance = 0.5;

				Assert.True(cleanBottomFit.Width > 0 && cleanBottomFit.Height > 0);
				Assert.True(cleanTopFit.Width > 0 && cleanTopFit.Height > 0);
				Assert.True(cleanLayoutView.Bounds.Height + tolerance >= cleanExpectedHeight);
				AssertContained(cleanLayoutView.Bounds, cleanBottomFrame, tolerance);
				AssertContained(cleanLayoutView.Bounds, cleanTopFrame, tolerance);

				var bottom = new Button { Text = "Bottom Button" };
				var top = new Button { Text = "Click Me!", InputTransparent = false };
				var reportedLayout = new AbsoluteLayout
				{
					bottom,
					top
				};

				var proportionalBounds = new Rect(0, 0, 1, 1);
				AbsoluteLayout.SetLayoutBounds(bottom, proportionalBounds);
				AbsoluteLayout.SetLayoutFlags(bottom, AbsoluteLayoutFlags.All);
				AbsoluteLayout.SetLayoutBounds(top, proportionalBounds);
				AbsoluteLayout.SetLayoutFlags(top, AbsoluteLayoutFlags.All);

				var sizeChanged = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
				var callbackObserved = false;
				var observedHeight = -1d;
				reportedLayout.SizeChanged += (_, _) =>
				{
					callbackObserved = true;
					observedHeight = reportedLayout.Height;
					sizeChanged.TrySetResult(observedHeight);
				};

				stack.Add(reportedLayout);
				await sizeChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.True(callbackObserved);
				Assert.NotEqual(-1d, observedHeight);
				Assert.NotNull(reportedLayout.Handler);
				Assert.NotNull(bottom.Handler);
				Assert.NotNull(top.Handler);

				var layoutView = Assert.IsType<LayoutView>(reportedLayout.Handler.PlatformView);
				var bottomView = Assert.IsType<UIButton>(bottom.Handler.PlatformView);
				var topView = Assert.IsType<UIButton>(top.Handler.PlatformView);

				await AssertEventually(() =>
				{
					var currentBottomFrame = bottomView.ConvertRectToView(bottomView.Bounds, layoutView);
					var currentTopFrame = topView.ConvertRectToView(topView.Bounds, layoutView);
					return layoutView.Bounds.Height > 0 &&
						currentBottomFrame.Width > 0 &&
						currentBottomFrame.Height > 0 &&
						currentTopFrame.Width > 0 &&
						currentTopFrame.Height > 0;
				});

				Assert.Equal("Bottom Button", bottomView.CurrentTitle);
				Assert.Equal("Click Me!", topView.CurrentTitle);
				Assert.NotNull(bottomView.Superview);
				Assert.NotNull(topView.Superview);

				var bottomFit = bottomView.SizeThatFits(CGSize.Empty);
				var topFit = topView.SizeThatFits(CGSize.Empty);
				var expectedHeight = Math.Max(bottomFit.Height, topFit.Height);
				var bottomFrame = bottomView.ConvertRectToView(bottomView.Bounds, layoutView);
				var topFrame = topView.ConvertRectToView(topView.Bounds, layoutView);

				Assert.True(bottomFit.Width > 0 && bottomFit.Height > 0);
				Assert.True(topFit.Width > 0 && topFit.Height > 0);
				Assert.True(
					layoutView.Bounds.Height + tolerance >= expectedHeight,
					$"Issue17673 AbsoluteLayout collapsed below its native Button content: " +
					$"layout height={layoutView.Bounds.Height:F2}, expected native fitting height={expectedHeight:F2}, " +
					$"tolerance={tolerance:F2}, bottom frame={bottomFrame}, top frame={topFrame}");
				AssertContained(layoutView.Bounds, bottomFrame, tolerance);
				AssertContained(layoutView.Bounds, topFrame, tolerance);
			});
		}

		static void AssertContained(CGRect containerBounds, CGRect childFrame, double tolerance)
		{
			Assert.True(childFrame.Left >= containerBounds.Left - tolerance);
			Assert.True(childFrame.Top >= containerBounds.Top - tolerance);
			Assert.True(childFrame.Right <= containerBounds.Right + tolerance);
			Assert.True(childFrame.Bottom <= containerBounds.Bottom + tolerance);
		}
	}
}
#endif

