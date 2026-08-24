using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Layout, "Issue17673")]
	public class Issue17673 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ProportionalChildrenPreserveNaturalButtonHeightWhenAdded()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var referenceButton = new Button { Text = "Click Me!" };
			var layoutHost = new VerticalStackLayout();
			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16
			};
			content.Add(referenceButton);
			content.Add(layoutHost);

			var page = new ContentPage
			{
				Content = new ScrollView { Content = content }
			};

			await AttachAndRun(page, async _ =>
			{
				Assert.NotNull(referenceButton.Handler);
				var nativeReferenceButton = Assert.IsAssignableFrom<UIButton>(referenceButton.Handler.PlatformView);

				await AssertEventually(
					() => nativeReferenceButton.Frame.Height > 0 && referenceButton.Frame.Height > 0,
					timeout: 5000,
					message: "The reference Button did not receive positive native and managed frames.");

				const double tolerance = 1;
				Assert.True(
					Math.Abs(nativeReferenceButton.Frame.Height - referenceButton.Frame.Height) <= tolerance,
					$"Reference Button native height {nativeReferenceButton.Frame.Height:F2} did not match managed height {referenceButton.Frame.Height:F2}.");
				var expectedDefaultButtonHeight = nativeReferenceButton.Frame.Height;

				var postAddSizeChangedCount = -1;
				var reportedNativeHeight = -1d;
				var bottomButton = new Button { Text = "Bottom Button" };
				var topButton = new Button
				{
					Text = "Click Me!",
					InputTransparent = false
				};
				var reportedLayout = new AbsoluteLayout
				{
					bottomButton,
					topButton
				};

				AbsoluteLayout.SetLayoutFlags(bottomButton, AbsoluteLayoutFlags.All);
				AbsoluteLayout.SetLayoutBounds(bottomButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutBounds(topButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutFlags(topButton, AbsoluteLayoutFlags.All);
				reportedLayout.SizeChanged += (_, _) =>
					postAddSizeChangedCount = postAddSizeChangedCount < 0 ? 1 : postAddSizeChangedCount + 1;

				layoutHost.Add(reportedLayout);

				await AssertEventually(
					() => postAddSizeChangedCount > 0,
					timeout: 5000,
					message: "The dynamically added AbsoluteLayout did not raise SizeChanged.");

				Assert.Same(bottomButton, reportedLayout.Children[0]);
				Assert.Same(topButton, reportedLayout.Children[1]);
				Assert.NotNull(reportedLayout.Handler);
				Assert.NotNull(bottomButton.Handler);
				Assert.NotNull(topButton.Handler);

				var nativeReportedLayout = Assert.IsAssignableFrom<UIView>(reportedLayout.Handler.PlatformView);
				var nativeBottomButton = Assert.IsAssignableFrom<UIButton>(bottomButton.Handler.PlatformView);
				var nativeTopButton = Assert.IsAssignableFrom<UIButton>(topButton.Handler.PlatformView);

				Assert.True(nativeBottomButton.IsDescendantOfView(nativeReportedLayout));
				Assert.True(nativeTopButton.IsDescendantOfView(nativeReportedLayout));

				await AssertEventually(
					() =>
					{
						reportedNativeHeight = nativeReportedLayout.Frame.Height;
						return reportedNativeHeight > 0 &&
							nativeBottomButton.Frame.Height > 0 &&
							nativeTopButton.Frame.Height > 0;
					},
					timeout: 5000,
					message: "The AbsoluteLayout and its Buttons did not receive positive native frames.");

				Assert.True(
					Math.Abs(reportedNativeHeight - expectedDefaultButtonHeight) <= tolerance,
					$"AbsoluteLayout native height did not preserve the default Button height: expected={expectedDefaultButtonHeight:F2}, actual={reportedNativeHeight:F2}, tolerance={tolerance:F2}.");
			});
		}
	}
#endif
}

