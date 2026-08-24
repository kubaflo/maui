#if MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Layout)]
	[Category("Issue17673")]
	public class Issue17673 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ProportionalChildrenDoNotCollapseLayoutBelowIntrinsicHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var bottom = new Button { Text = "Bottom Button" };
			var top = new Button { Text = "Click Me!", InputTransparent = false };
			var reportedLayout = new AbsoluteLayout { bottom, top };
			var scenarioContainer = new VerticalStackLayout { reportedLayout };
			var page = new ContentPage { Content = scenarioContainer };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var layoutHandler = Assert.IsType<LayoutHandler>(reportedLayout.Handler);
				var bottomHandler = Assert.IsType<ButtonHandler>(bottom.Handler);
				var topHandler = Assert.IsType<ButtonHandler>(top.Handler);
				var nativeLayout = layoutHandler.PlatformView;
				var nativeBottom = bottomHandler.PlatformView;
				var nativeTop = topHandler.PlatformView;

				Assert.True(
					nativeBottom.IsDescendantOfView(nativeLayout),
					"The Bottom Button native view was not attached beneath the AbsoluteLayout native view.");
				Assert.True(
					nativeTop.IsDescendantOfView(nativeLayout),
					"The Click Me! native view was not attached beneath the AbsoluteLayout native view.");

				var bottomIntrinsicHeight = nativeBottom.IntrinsicContentSize.Height;
				var topIntrinsicHeight = nativeTop.IntrinsicContentSize.Height;
				var expectedMinimumHeight = Math.Max(bottomIntrinsicHeight, topIntrinsicHeight);
				const double tolerance = 0.5;

				Assert.True(bottomIntrinsicHeight > 0, $"Bottom Button intrinsic height was {bottomIntrinsicHeight}.");
				Assert.True(topIntrinsicHeight > 0, $"Click Me! intrinsic height was {topIntrinsicHeight}.");
				await AssertEventually(
					() => nativeLayout.Frame.Height + tolerance >= expectedMinimumHeight,
					timeout: 5000,
					message: $"Initial AbsoluteLayout height {nativeLayout.Frame.Height} was below the expected minimum {expectedMinimumHeight}.");

				double observedSizeChangedHeight = -1;
				bool triggerApplied = false;
				reportedLayout.SizeChanged += OnReportedLayoutSizeChanged;

				triggerApplied = true;
				AbsoluteLayout.SetLayoutFlags(bottom, AbsoluteLayoutFlags.All);
				AbsoluteLayout.SetLayoutBounds(bottom, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutBounds(top, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutFlags(top, AbsoluteLayoutFlags.All);

				await AssertEventually(
					() => observedSizeChangedHeight >= 0,
					timeout: 5000,
					message: "AbsoluteLayout did not raise SizeChanged after the reported property changes.");
				await AssertEventually(
					() => Math.Abs(nativeLayout.Frame.Height - observedSizeChangedHeight) <= tolerance,
					timeout: 5000,
					message: $"Native layout did not settle at the SizeChanged height {observedSizeChangedHeight}; native height was {nativeLayout.Frame.Height}.");

				reportedLayout.SizeChanged -= OnReportedLayoutSizeChanged;

				Assert.Same(layoutHandler, reportedLayout.Handler);
				Assert.Same(bottomHandler, bottom.Handler);
				Assert.Same(topHandler, top.Handler);
				Assert.True(
					nativeBottom.IsDescendantOfView(nativeLayout),
					"The Bottom Button native view was detached after the reported property changes.");
				Assert.True(
					nativeTop.IsDescendantOfView(nativeLayout),
					"The Click Me! native view was detached after the reported property changes.");

				var finalHeight = nativeLayout.Frame.Height;
				Assert.True(
					finalHeight + tolerance >= expectedMinimumHeight,
					$"Issue17673 AbsoluteLayout collapsed below its default Button content height: final={finalHeight}, expectedMinimum={expectedMinimumHeight}, bottomIntrinsic={bottomIntrinsicHeight}, topIntrinsic={topIntrinsicHeight}, sizeChanged={observedSizeChangedHeight}, tolerance={tolerance}.");

				void OnReportedLayoutSizeChanged(object sender, EventArgs args)
				{
					if (triggerApplied)
						observedSizeChangedHeight = reportedLayout.Height;
				}
			});
		}
	}
}
#endif

