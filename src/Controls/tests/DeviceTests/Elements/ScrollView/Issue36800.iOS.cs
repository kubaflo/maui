using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category(TestCategory.ScrollView)]
	[Category("Issue36800")]
	public class Issue36800 : ControlsHandlerTestBase
	{
		const double RangeTolerance = 1;

		[Fact]
		public async Task ShortContentDoesNotHavePhantomVerticalScrollRange()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<ArrangeTrackingStackLayout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var target = CreateScene(SafeAreaEdges.Container);
			await CreateHandlerAndAddToWindow(target.RootPage, async () =>
			{
				var nativeScrollView = await AssertSceneReady(target, SafeAreaEdges.Container, requireAdjustedInset: true);
				var managedContentHeight = target.ScrollView.ContentSize.Height;
				var nativeContentInflation = nativeScrollView.ContentSize.Height - managedContentHeight;
				var verticalRange = GetVerticalRange(nativeScrollView);

				Assert.True(
					managedContentHeight +
						nativeScrollView.AdjustedContentInset.Top +
						nativeScrollView.AdjustedContentInset.Bottom <=
						nativeScrollView.Bounds.Height + RangeTolerance,
					$"Issue36800 managed content did not fit the inset-aware viewport; managed content size {managedContentHeight:F2}, " +
					$"adjusted top {nativeScrollView.AdjustedContentInset.Top:F2}, adjusted bottom {nativeScrollView.AdjustedContentInset.Bottom:F2}, " +
					$"bounds {nativeScrollView.Bounds.Height:F2}.");

				Assert.True(
					nativeContentInflation <= RangeTolerance,
					$"Issue36800 target native content size exceeded managed content size by {nativeContentInflation:F2}; " +
					$"native content size {nativeScrollView.ContentSize.Height:F2}, managed content size {managedContentHeight:F2}, " +
					$"native vertical range {verticalRange:F2}, adjusted top {nativeScrollView.AdjustedContentInset.Top:F2}, " +
					$"adjusted bottom {nativeScrollView.AdjustedContentInset.Bottom:F2}, bounds {nativeScrollView.Bounds.Height:F2}, " +
					$"expected maximum {RangeTolerance:F2}.");
			});
		}

		static (
			ContentPage RootPage,
			ScrollView ScrollView,
			ArrangeTrackingStackLayout ContentLayout,
			Label ContentLabel,
			Button DumpButton,
			Label DiagnosticLabel) CreateScene(SafeAreaEdges scrollSafeAreaEdges)
		{
			var contentLabel = new Label
			{
				Text = "Small content",
				FontSize = 22
			};
			var dumpButton = new Button
			{
				Text = "Dump native state"
			};
			var diagnosticLabel = new Label
			{
				FontSize = 10
			};
			var contentLayout = new ArrangeTrackingStackLayout
			{
				Padding = 16,
				Spacing = 12,
				Children =
				{
					contentLabel,
					dumpButton,
					diagnosticLabel
				}
			};
			var scrollView = new ScrollView
			{
				SafeAreaEdges = scrollSafeAreaEdges,
				Content = contentLayout
			};
			var rootPage = new ContentPage
			{
				SafeAreaEdges = SafeAreaEdges.None,
				Content = scrollView
			};

			return (
				rootPage,
				scrollView,
				contentLayout,
				contentLabel,
				dumpButton,
				diagnosticLabel);
		}

		static async Task<MauiScrollView> AssertSceneReady(
			(
				ContentPage RootPage,
				ScrollView ScrollView,
				ArrangeTrackingStackLayout ContentLayout,
				Label ContentLabel,
				Button DumpButton,
				Label DiagnosticLabel) scene,
			SafeAreaEdges expectedScrollSafeAreaEdges,
			bool requireAdjustedInset)
		{
			Assert.Same(scene.ScrollView, scene.RootPage.Content);
			Assert.Same(scene.ContentLayout, scene.ScrollView.Content);
			Assert.Equal(SafeAreaEdges.None, scene.RootPage.SafeAreaEdges);
			Assert.Equal(expectedScrollSafeAreaEdges, scene.ScrollView.SafeAreaEdges);
			Assert.Equal(new Thickness(16), scene.ContentLayout.Padding);
			Assert.Equal(12, scene.ContentLayout.Spacing);
			Assert.Equal(3, scene.ContentLayout.Children.Count);
			Assert.Same(scene.ContentLabel, scene.ContentLayout.Children[0]);
			Assert.Same(scene.DumpButton, scene.ContentLayout.Children[1]);
			Assert.Same(scene.DiagnosticLabel, scene.ContentLayout.Children[2]);
			Assert.Equal("Small content", scene.ContentLabel.Text);
			Assert.Equal(22, scene.ContentLabel.FontSize);
			Assert.Equal("Dump native state", scene.DumpButton.Text);
			Assert.Equal(10, scene.DiagnosticLabel.FontSize);

			await AssertEventually(
				() => scene.ContentLayout.ArrangedHeight > 0,
				timeout: 5000,
				message: "Issue36800 content did not receive a real arrange callback.");

			await AssertEventually(
				() => scene.ScrollView.ContentSize.Height > 0,
				timeout: 5000,
				message: "Issue36800 managed scroll view did not report a nonempty content size.");

			var scrollHandler = Assert.IsType<ScrollViewHandler>(scene.ScrollView.Handler);
			Assert.Same(scene.ScrollView, scrollHandler.VirtualView);
			var nativeScrollView = Assert.IsType<MauiScrollView>(scrollHandler.PlatformView);

			await AssertEventually(
				() => nativeScrollView.Bounds.Width > 0 &&
					nativeScrollView.Bounds.Height > 0 &&
					nativeScrollView.ContentSize.Height > 0,
				timeout: 5000,
				message: "Issue36800 native scroll view did not reach nonempty layout bounds.");

			if (requireAdjustedInset)
			{
				await AssertEventually(
					() => nativeScrollView.AdjustedContentInset.Top + nativeScrollView.AdjustedContentInset.Bottom > 0,
					timeout: 5000,
					message: "Issue36800 target did not receive a nonzero runtime adjusted vertical inset.");
				Assert.Equal(UIScrollViewContentInsetAdjustmentBehavior.Always, nativeScrollView.ContentInsetAdjustmentBehavior);
			}
			else
			{
				Assert.Equal(UIScrollViewContentInsetAdjustmentBehavior.Never, nativeScrollView.ContentInsetAdjustmentBehavior);
			}

			var nativeContent = Assert.IsAssignableFrom<UIView>(scene.ContentLayout.Handler.PlatformView);
			Assert.Same(nativeScrollView, nativeContent.Superview);

			return nativeScrollView;
		}

		static double GetVerticalRange(UIScrollView nativeScrollView) =>
			nativeScrollView.ContentSize.Height +
			nativeScrollView.AdjustedContentInset.Top +
			nativeScrollView.AdjustedContentInset.Bottom -
			nativeScrollView.Bounds.Height;

		sealed class ArrangeTrackingStackLayout : VerticalStackLayout
		{
			public double ArrangedHeight { get; private set; } = -1;

			protected override Size ArrangeOverride(Rect bounds)
			{
				ArrangedHeight = bounds.Height;
				return base.ArrangeOverride(bounds);
			}
		}

	}
#endif
}

