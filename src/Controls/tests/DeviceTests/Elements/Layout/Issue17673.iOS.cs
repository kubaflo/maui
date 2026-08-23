#if MACCATALYST
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Layout)]
	[Category("Issue17673")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue17673 : ControlsHandlerTestBase
	{
		const double HeightTolerance = 0.5;

		[Fact]
		public async Task AutoSizedAbsoluteLayoutPreservesButtonDesiredHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var controlPage = CreatePage(100, out var controlLayout, out var controlTopButton);

			await CreateHandlerAndAddToWindow(controlPage, async () =>
			{
				await AssertEventually(
					() => controlLayout.Handler?.PlatformView is LayoutView layoutView && layoutView.Bounds.Height > 0,
					timeout: 5000,
					message: "Explicit-height control layout did not complete native layout.");

				var nativeLayout = Assert.IsAssignableFrom<LayoutView>(controlLayout.Handler.PlatformView);
				var nativeButton = Assert.IsAssignableFrom<UIButton>(controlTopButton.Handler.PlatformView);
				var desiredHeight = nativeButton.SizeThatFits(CGSize.Empty).Height;

				Assert.True(desiredHeight > 0, $"Control Button native desired height must be nonzero; desired height: {desiredHeight:F2}.");
				Assert.True(
					nativeLayout.Bounds.Height + HeightTolerance >= desiredHeight,
					$"Explicit-height control AbsoluteLayout must contain the Button's native desired height; layout height: {nativeLayout.Bounds.Height:F2}, desired height: {desiredHeight:F2}, tolerance: {HeightTolerance:F2}.");
			});

			var targetPage = CreatePage(-1, out var targetLayout, out var targetTopButton);
			var loadedCallbackCount = -1;
			var sizeChangedCallbackCount = -1;
			double targetNativeHeight = -1;
			double targetDesiredHeight = -1;

			targetLayout.Loaded += (_, _) => loadedCallbackCount++;
			targetLayout.SizeChanged += (_, _) => sizeChangedCallbackCount++;

			await CreateHandlerAndAddToWindow(targetPage, async () =>
			{
				await AssertEventually(
					() => loadedCallbackCount >= 0 &&
						sizeChangedCallbackCount >= 0 &&
						targetLayout.Handler?.PlatformView is LayoutView layoutView &&
						layoutView.Bounds.Height > 0,
					timeout: 5000,
					message: "Auto-sized AbsoluteLayout did not complete Loaded, SizeChanged, and native layout.");

				var nativeLayout = Assert.IsAssignableFrom<LayoutView>(targetLayout.Handler.PlatformView);
				var nativeButton = Assert.IsAssignableFrom<UIButton>(targetTopButton.Handler.PlatformView);

				Assert.True(
					nativeButton.IsDescendantOfView(nativeLayout),
					"The intended top Button must be a native descendant of the intended AbsoluteLayout.");
				Assert.True(nativeButton.Bounds.Height > 0, $"The intended top Button must have nonzero native bounds; bounds height: {nativeButton.Bounds.Height:F2}.");

				targetNativeHeight = nativeLayout.Bounds.Height;
				targetDesiredHeight = nativeButton.SizeThatFits(CGSize.Empty).Height;
			});

			Assert.True(loadedCallbackCount >= 0, "The target AbsoluteLayout Loaded callback must occur.");
			Assert.True(sizeChangedCallbackCount >= 0, "The target AbsoluteLayout SizeChanged callback must occur.");
			Assert.True(targetNativeHeight > 0, $"AbsoluteLayout native height must be captured after layout; captured height: {targetNativeHeight:F2}.");
			Assert.True(targetDesiredHeight > 0, $"Top Button native desired height must be nonzero; desired height: {targetDesiredHeight:F2}.");
			Assert.True(
				targetNativeHeight + HeightTolerance >= targetDesiredHeight,
				$"AbsoluteLayout native height must not shrink below the default top Button's native desired height; layout height: {targetNativeHeight:F2}, desired height: {targetDesiredHeight:F2}, tolerance: {HeightTolerance:F2}.");
		}

		static ContentPage CreatePage(double explicitLayoutHeight, out AbsoluteLayout reportedLayout, out Button topButton)
		{
			var bottomButton = new Button { Text = "Bottom Button" };
			topButton = new Button { Text = "Click Me!", InputTransparent = false };
			reportedLayout = new AbsoluteLayout
			{
				bottomButton,
				topButton
			};

			if (explicitLayoutHeight > 0)
				reportedLayout.HeightRequest = explicitLayoutHeight;

			AbsoluteLayout.SetLayoutBounds(bottomButton, new Rect(0, 0, 1, 1));
			AbsoluteLayout.SetLayoutFlags(bottomButton, AbsoluteLayoutFlags.All);
			AbsoluteLayout.SetLayoutBounds(topButton, new Rect(0, 0, 1, 1));
			AbsoluteLayout.SetLayoutFlags(topButton, AbsoluteLayoutFlags.All);

			return new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							FontSize = 20,
							Text = "Issue 17673: AbsoluteLayout auto-sizing"
						},
						new Label
						{
							Text = "The two default buttons below use proportional bounds. The layout should not shrink below their desired height."
						},
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							Text = "AbsoluteLayout measurement:"
						},
						new Label { Text = "Waiting for layout measurement" },
						reportedLayout,
						new Button { Text = "Check layout height" }
					}
				}
			};
		}
	}
}
#endif

