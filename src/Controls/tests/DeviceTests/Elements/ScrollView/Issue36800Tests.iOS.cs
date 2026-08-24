#if IOS && !MACCATALYST
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
	[Category(TestCategory.ScrollView)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36800 : ControlsHandlerTestBase
	{
		const double RangeTolerance = 0.5;

		[Fact]
		[Category("Issue36800")]
		public async Task ShortContentHasNoVerticalScrollRangeWithContainerSafeArea()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var clean = await MeasureRange(SafeAreaEdges.None);
			Assert.True(clean.ReachableRange <= RangeTolerance,
				$"Clean ScrollView calibration had a reachable vertical range of {clean.ReachableRange:F2}.");

			var target = await MeasureRange(SafeAreaEdges.Container);
			Assert.True(target.VerticalInset > 0, "The device did not provide the required nonzero vertical safe-area inset.");
			Assert.Equal(UIScrollViewContentInsetAdjustmentBehavior.Always, target.AdjustmentBehavior);
			Assert.True(target.ReachableRange <= RangeTolerance,
				$"Issue36800: short content must have no vertically reachable phantom scroll range. " +
				$"Range={target.ReachableRange:F2}, ContentSize={target.NativeContentHeight:F2}, " +
				$"Bounds={target.BoundsHeight:F2}, Insets={target.TopInset:F2}+{target.BottomInset:F2}, " +
				$"Tolerance={RangeTolerance:F2}.");
		}

		async Task<RangeMeasurement> MeasureRange(SafeAreaEdges safeAreaEdges)
		{
			var titleLabel = new Label
			{
				Text = "Small content",
				FontSize = 22
			};
			var diagnosticButton = new Button
			{
				Text = "Dump native state"
			};
			var diagnosticLabel = new Label
			{
				FontSize = 10
			};
			var contentLayout = new VerticalStackLayout
			{
				Padding = 16,
				Spacing = 12,
				Children =
				{
					titleLabel,
					diagnosticButton,
					diagnosticLabel
				}
			};
			var scrollView = new ScrollView
			{
				SafeAreaEdges = safeAreaEdges,
				Content = contentLayout
			};
			var page = new ContentPage
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = scrollView
			};
			var layoutCount = -1;
			scrollView.SizeChanged += OnSizeChanged;

			var measurement = new RangeMeasurement(
				double.NaN,
				double.NaN,
				double.NaN,
				double.NaN,
				double.NaN,
				default);
			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(() => layoutCount >= 0,
					message: "The ScrollView did not complete a post-attachment layout.");

				Assert.Same(contentLayout, scrollView.Content);
				Assert.Equal(3, contentLayout.Children.Count);
				Assert.Same(titleLabel, contentLayout.Children[0]);
				Assert.Same(diagnosticButton, contentLayout.Children[1]);
				Assert.Same(diagnosticLabel, contentLayout.Children[2]);
				Assert.Equal("Small content", titleLabel.Text);
				Assert.Equal(22, titleLabel.FontSize);
				Assert.Equal("Dump native state", diagnosticButton.Text);
				Assert.Null(diagnosticLabel.Text);
				Assert.Equal(10, diagnosticLabel.FontSize);
				Assert.Equal(new Thickness(16), contentLayout.Padding);
				Assert.Equal(12, contentLayout.Spacing);
				Assert.Equal(safeAreaEdges, scrollView.SafeAreaEdges);
				Assert.NotNull(scrollView.Window);
				Assert.NotNull(scrollView.Handler);

				var handler = Assert.IsType<ScrollViewHandler>(scrollView.Handler);
				Assert.NotNull(handler.PlatformView);
				var platformView = handler.PlatformView;

				await AssertEventually(
					() => platformView.Bounds.Height > 0 &&
						platformView.ContentSize.Height > 0 &&
						scrollView.Height > 0 &&
						scrollView.ContentSize.Height > 0,
					message: "The managed and native ScrollViews did not complete layout.");

				if (safeAreaEdges == SafeAreaEdges.Container)
				{
					await AssertEventually(
						() => platformView.AdjustedContentInset.Top + platformView.AdjustedContentInset.Bottom > 0,
						message: "The real root window did not propagate a nonzero vertical safe-area inset.");
				}

				Assert.Equal(0, contentLayout.Bounds.X);
				Assert.Equal(0, contentLayout.Bounds.Y);
				Assert.True(scrollView.ContentSize.Height <= scrollView.Height,
					$"Managed content height {scrollView.ContentSize.Height:F2} did not fit viewport {scrollView.Height:F2}.");

				var insets = platformView.AdjustedContentInset;
				var reachableRange = Math.Max(
					0,
					platformView.ContentSize.Height + insets.Top + insets.Bottom - platformView.Bounds.Height);

				measurement = new RangeMeasurement(
					reachableRange,
					platformView.ContentSize.Height,
					platformView.Bounds.Height,
					insets.Top,
					insets.Bottom,
					platformView.ContentInsetAdjustmentBehavior);
			});

			scrollView.SizeChanged -= OnSizeChanged;
			return measurement;

			void OnSizeChanged(object sender, EventArgs args)
			{
				layoutCount++;
			}
		}

		readonly record struct RangeMeasurement(
			double ReachableRange,
			double NativeContentHeight,
			double BoundsHeight,
			double TopInset,
			double BottomInset,
			UIScrollViewContentInsetAdjustmentBehavior AdjustmentBehavior)
		{
			public double VerticalInset => TopInset + BottomInset;
		}
	}
}
#endif

