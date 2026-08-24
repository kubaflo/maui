#if IOS && !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.ScrollView)]
	[Category("Issue36800")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36800 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task UndersizedContentHasNoReachableVerticalOverflow()
		{
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var withoutContainerSafeArea = await MeasureNativeRange(SafeAreaEdges.None);
			Assert.True(
				withoutContainerSafeArea.Range <= tolerance,
				$"Control measurement must not introduce vertical overflow; native range was {withoutContainerSafeArea.Range:F2} pt.");

			var withContainerSafeArea = await MeasureNativeRange(SafeAreaEdges.Container);
			Assert.True(
				withContainerSafeArea.Range <= tolerance,
				$"Issue 36800: undersized ScrollView must have no reachable vertical overflow; native range was {withContainerSafeArea.Range:F2} pt, expected <= {tolerance:F2} pt (content={withContainerSafeArea.ContentHeight:F2}, top={withContainerSafeArea.TopInset:F2}, bottom={withContainerSafeArea.BottomInset:F2}, bounds={withContainerSafeArea.BoundsHeight:F2}).");

			async Task<(double Range, double ContentHeight, double TopInset, double BottomInset, double BoundsHeight)> MeasureNativeRange(SafeAreaEdges scrollSafeAreaEdges)
			{
				var smallContentLabel = new Label
				{
					Text = "Small content",
					FontSize = 22
				};
				var diagnosticLabel = new Label
				{
					FontSize = 10
				};
				var contentStack = new VerticalStackLayout
				{
					Padding = 16,
					Spacing = 12,
					Children =
					{
						smallContentLabel,
						new Button { Text = "Dump native state" },
						diagnosticLabel,
						new Label()
					}
				};
				var scrollView = new ScrollView
				{
					SafeAreaEdges = scrollSafeAreaEdges,
					Content = contentStack
				};
				var page = new ContentPage
				{
					SafeAreaEdges = SafeAreaEdges.None,
					Content = scrollView
				};

				var layoutCallbackOccurred = false;
				scrollView.SizeChanged += (_, _) => layoutCallbackOccurred = true;

				var measurement = (Range: double.NaN, ContentHeight: double.NaN, TopInset: double.NaN, BottomInset: double.NaN, BoundsHeight: double.NaN);

				await CreateHandlerAndAddToWindow(page, async () =>
				{
					await AssertEventually(() =>
					{
						if (!layoutCallbackOccurred ||
							scrollView.Handler?.PlatformView is not UIScrollView nativeScrollView ||
							nativeScrollView.Window is null)
						{
							return false;
						}

						var runtimeSafeArea = nativeScrollView.SafeAreaInsets.Top + nativeScrollView.SafeAreaInsets.Bottom;
						var adjustedSafeArea = nativeScrollView.AdjustedContentInset.Top + nativeScrollView.AdjustedContentInset.Bottom;
						return nativeScrollView.Bounds.Width > 0 &&
							nativeScrollView.Bounds.Height > 0 &&
							nativeScrollView.ContentSize.Height > 0 &&
							contentStack.Width > 0 &&
							contentStack.Height > 0 &&
							smallContentLabel.Width > 0 &&
							smallContentLabel.Height > 0 &&
							runtimeSafeArea > 0 &&
							(scrollSafeAreaEdges == SafeAreaEdges.None || adjustedSafeArea > 0);
					});

					Assert.True(layoutCallbackOccurred, "The ScrollView must receive a post-attachment layout callback.");
					var scrollHandler = Assert.IsType<ScrollViewHandler>(scrollView.Handler);
					var nativeScrollView = Assert.IsAssignableFrom<UIScrollView>(scrollHandler.PlatformView);
					Assert.NotNull(nativeScrollView.Window);
					Assert.Same(contentStack, scrollView.Content);
					Assert.Same(contentStack, smallContentLabel.Parent);
					Assert.Contains(smallContentLabel, contentStack.Children);
					Assert.True(
						smallContentLabel.Frame.Left >= -tolerance &&
						smallContentLabel.Frame.Top >= -tolerance &&
						smallContentLabel.Frame.Right <= contentStack.Width + tolerance &&
						smallContentLabel.Frame.Bottom <= contentStack.Height + tolerance,
						"The intended Small content label must be laid out within the ScrollView content.");

					var adjustedInset = nativeScrollView.AdjustedContentInset;
					if (scrollSafeAreaEdges == SafeAreaEdges.Container)
					{
						Assert.Equal(UIScrollViewContentInsetAdjustmentBehavior.Always, nativeScrollView.ContentInsetAdjustmentBehavior);
						Assert.True(adjustedInset.Top + adjustedInset.Bottom > 0, "The real window must supply a nonzero adjusted vertical safe-area inset.");
					}

					var contentHeight = nativeScrollView.ContentSize.Height;
					var boundsHeight = nativeScrollView.Bounds.Height;
					var range = contentHeight + adjustedInset.Top + adjustedInset.Bottom - boundsHeight;
					measurement = (range, contentHeight, adjustedInset.Top, adjustedInset.Bottom, boundsHeight);
				});

				return measurement;
			}
		}
	}
}
#endif

