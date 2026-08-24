#if MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
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
		public async Task AbsoluteLayoutPreservesNaturalHeightOfProportionalChildren()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var bottomButton = new Button { Text = "Bottom Button" };
			var topButton = new Button { Text = "Click Me!", InputTransparent = false };
			var issueLayout = new AbsoluteLayout
			{
				BackgroundColor = Colors.LightGray,
				Children =
				{
					bottomButton,
					topButton,
				},
			};

			var proportionalBounds = new Rect(0, 0, 1, 1);
			AbsoluteLayout.SetLayoutBounds(bottomButton, proportionalBounds);
			AbsoluteLayout.SetLayoutFlags(bottomButton, AbsoluteLayoutFlags.All);
			AbsoluteLayout.SetLayoutBounds(topButton, proportionalBounds);
			AbsoluteLayout.SetLayoutFlags(topButton, AbsoluteLayoutFlags.All);

			var checkButton = new Button { Text = "Check layout" };
			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = "Issue 17673: AbsoluteLayout proportional child sizing",
						FontSize = 20,
						FontAttributes = FontAttributes.Bold,
					},
					new Label
					{
						Text = "The two default buttons below share Rect(0, 0, 1, 1) with all dimensions proportional.",
					},
					issueLayout,
					new Label { Text = "Layout height: pending" },
					new Label { Text = "Natural button height: pending" },
					new Label { Text = "Status pending", FontAttributes = FontAttributes.Bold },
					checkButton,
				},
			};
			var page = new ContentPage { Content = content };

			var pageLoaded = false;
			var layoutSizeChangedCount = 0;
			page.Loaded += (_, _) => pageLoaded = true;
			issueLayout.SizeChanged += (_, _) => layoutSizeChangedCount++;

			await AttachAndRun<PageHandler>(page, async _ =>
			{
				await AssertEventually(
					() => pageLoaded,
					timeout: 5000,
					message: "The page did not report its post-attachment Loaded transition.");
				Assert.True(pageLoaded, "The page must be loaded before native geometry is evaluated.");

				await AssertEventually(
					() => layoutSizeChangedCount > 0,
					timeout: 5000,
					message: "The AbsoluteLayout did not report a post-attachment size transition.");
				Assert.True(layoutSizeChangedCount > 0, "The AbsoluteLayout must complete a size transition before native geometry is evaluated.");

				Assert.NotNull(page.Handler);
				var pageNativeView = Assert.IsAssignableFrom<UIView>(page.Handler.PlatformView);
				Assert.NotNull(pageNativeView.Window);

				Assert.NotNull(checkButton.Handler);
				var checkNativeView = Assert.IsAssignableFrom<UIView>(checkButton.Handler.PlatformView);
				Assert.Same(checkButton, content.Children[6]);
				Assert.Equal("Check layout", checkButton.Text);
				Assert.NotNull(checkNativeView.Superview);
				Assert.NotNull(checkNativeView.Window);
				Assert.True(checkNativeView.IntrinsicContentSize.Height > 0, "The default check Button must have a positive intrinsic height.");
				Assert.True(checkButton.Height > 0, "The default check Button must have a positive arranged height.");
				Assert.True(checkNativeView.Frame.Height > 0, "The default check Button must have a positive native frame height.");

				const double tolerance = 1;
				Assert.InRange(
					Math.Abs((double)checkNativeView.Frame.Height - checkButton.Height),
					0,
					tolerance);

				Assert.Collection(
					issueLayout.Children,
					child => Assert.Same(bottomButton, child),
					child => Assert.Same(topButton, child));
				Assert.Equal("Bottom Button", bottomButton.Text);
				Assert.Equal("Click Me!", topButton.Text);
				Assert.False(topButton.InputTransparent);
				Assert.Null(bottomButton.Style);
				Assert.Null(topButton.Style);
				Assert.Equal(proportionalBounds, AbsoluteLayout.GetLayoutBounds(bottomButton));
				Assert.Equal(proportionalBounds, AbsoluteLayout.GetLayoutBounds(topButton));
				Assert.Equal(AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(bottomButton));
				Assert.Equal(AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(topButton));

				Assert.NotNull(issueLayout.Handler);
				Assert.NotNull(bottomButton.Handler);
				Assert.NotNull(topButton.Handler);
				var layoutNativeView = Assert.IsAssignableFrom<UIView>(issueLayout.Handler.PlatformView);
				var bottomNativeView = Assert.IsAssignableFrom<UIView>(bottomButton.Handler.PlatformView);
				var topNativeView = Assert.IsAssignableFrom<UIView>(topButton.Handler.PlatformView);
				Assert.NotNull(layoutNativeView.Window);
				Assert.True(bottomNativeView.IsDescendantOfView(layoutNativeView), "The bottom Button native view must be attached below the AbsoluteLayout native view.");
				Assert.True(topNativeView.IsDescendantOfView(layoutNativeView), "The top Button native view must be attached below the AbsoluteLayout native view.");

				var layoutHeight = (double)layoutNativeView.Frame.Height;
				var naturalButtonHeight = (double)checkNativeView.Frame.Height;
				Assert.True(
					layoutHeight + tolerance >= naturalButtonHeight,
					$"AbsoluteLayout native height should preserve proportional children's natural height. Observed layout={layoutHeight:0.##}, natural button={naturalButtonHeight:0.##}.");
			});
		}
	}
}
#endif

