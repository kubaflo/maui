#if MACCATALYST
using System;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue17673")]
	public class Issue17673 : ControlsHandlerTestBase
	{
		[Fact, Category(TestCategory.Layout)]
		public async Task ProportionalChildrenDoNotShrinkAbsoluteLayoutBelowButtonHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var referenceButton = new Button
			{
				Text = "Click Me!",
				InputTransparent = false
			};
			var showButton = new Button { Text = "Show AbsoluteLayout" };
			var scenarioHost = new VerticalStackLayout();
			var rootLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label { Text = "Issue 17673: AbsoluteLayout proportional child sizing", FontAttributes = FontAttributes.Bold },
					new Label { Text = "AbsoluteLayout measurement:" },
					referenceButton,
					showButton,
					new Label { Text = "The affected layout has not been created." },
					scenarioHost
				}
			};
			var page = new ContentPage { Content = rootLayout };

			await CreateHandlerAndAddToWindow<PageHandler>(page, async pageHandler =>
			{
				Assert.NotNull(pageHandler.PlatformView);
				Assert.IsType<LayoutHandler>(rootLayout.Handler);
				Assert.IsType<ButtonHandler>(referenceButton.Handler);
				Assert.IsType<ButtonHandler>(showButton.Handler);
				Assert.IsType<LayoutHandler>(scenarioHost.Handler);

				var referenceNative = Assert.IsAssignableFrom<UIView>(referenceButton.Handler.PlatformView);
				await AssertEventually(
					() => referenceNative.Frame.Width > 0 && referenceNative.Frame.Height > 0,
					message: "Reference Button did not receive a native frame");

				const double tolerance = 1;
				double intrinsicHeight = referenceNative
					.SizeThatFits(new CGSize(referenceNative.Frame.Width, double.PositiveInfinity))
					.Height;
				double referenceFrameHeight = referenceNative.Frame.Height;
				Assert.True(intrinsicHeight > 1, $"Reference Button intrinsic height was invalid: {intrinsicHeight:F2}");
				Assert.InRange(Math.Abs(referenceFrameHeight - intrinsicHeight), 0, tolerance);

				var bottomButton = new Button { Text = "Bottom Button" };
				var topButton = new Button
				{
					Text = "Click Me!",
					InputTransparent = false
				};
				var affectedLayout = new AbsoluteLayout();

				AbsoluteLayout.SetLayoutFlags(bottomButton, AbsoluteLayoutFlags.All);
				AbsoluteLayout.SetLayoutBounds(bottomButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutBounds(topButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutFlags(topButton, AbsoluteLayoutFlags.All);
				affectedLayout.Children.Add(bottomButton);
				affectedLayout.Children.Add(topButton);

				var sizeChanged = false;
				var sizeChangedCount = 0;
				affectedLayout.SizeChanged += (_, _) =>
				{
					sizeChanged = true;
					sizeChangedCount++;
				};

				scenarioHost.Children.Add(affectedLayout);

				await AssertEventually(
					() => sizeChanged,
					message: "AbsoluteLayout did not raise SizeChanged after runtime insertion");
				Assert.True(sizeChangedCount > 0);

				Assert.IsType<LayoutHandler>(affectedLayout.Handler);
				Assert.IsType<ButtonHandler>(bottomButton.Handler);
				Assert.IsType<ButtonHandler>(topButton.Handler);

				var layoutNative = Assert.IsAssignableFrom<UIView>(affectedLayout.Handler.PlatformView);
				var bottomNative = Assert.IsAssignableFrom<UIView>(bottomButton.Handler.PlatformView);
				var topNative = Assert.IsAssignableFrom<UIView>(topButton.Handler.PlatformView);
				await AssertEventually(
					() => layoutNative.Frame.Width > 0 &&
						layoutNative.Frame.Height > 0 &&
						bottomNative.Frame.Width > 0 &&
						bottomNative.Frame.Height > 0 &&
						topNative.Frame.Width > 0 &&
						topNative.Frame.Height > 0,
					message: "AbsoluteLayout and its Buttons did not complete native layout");

				Assert.Same(bottomButton, affectedLayout.Children[0]);
				Assert.Same(topButton, affectedLayout.Children[1]);
				Assert.Equal("Bottom Button", bottomButton.Text);
				Assert.Equal("Click Me!", topButton.Text);
				Assert.False(topButton.InputTransparent);
				Assert.Equal(AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(bottomButton));
				Assert.Equal(AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(topButton));
				Assert.Equal(new Rect(0, 0, 1, 1), AbsoluteLayout.GetLayoutBounds(bottomButton));
				Assert.Equal(new Rect(0, 0, 1, 1), AbsoluteLayout.GetLayoutBounds(topButton));
				Assert.Same(bottomButton, bottomButton.Handler.VirtualView);
				Assert.Same(topButton, topButton.Handler.VirtualView);
				Assert.True(IsNativeDescendantOf(bottomNative, layoutNative), "Bottom Button was not in the affected native layout");
				Assert.True(IsNativeDescendantOf(topNative, layoutNative), "Top Button was not in the affected native layout");

				var bottomFrame = bottomNative.ConvertRectToView(bottomNative.Bounds, layoutNative);
				var topFrame = topNative.ConvertRectToView(topNative.Bounds, layoutNative);
				Assert.InRange((double)bottomFrame.GetMinX(), 0, (double)layoutNative.Bounds.Width);
				Assert.InRange((double)bottomFrame.GetMinY(), 0, (double)layoutNative.Bounds.Height);
				Assert.InRange((double)bottomFrame.GetMaxX(), 0, (double)layoutNative.Bounds.Width + tolerance);
				Assert.InRange((double)bottomFrame.GetMaxY(), 0, (double)layoutNative.Bounds.Height + tolerance);
				Assert.InRange((double)topFrame.GetMinX(), 0, (double)layoutNative.Bounds.Width);
				Assert.InRange((double)topFrame.GetMinY(), 0, (double)layoutNative.Bounds.Height);
				Assert.InRange((double)topFrame.GetMaxX(), 0, (double)layoutNative.Bounds.Width + tolerance);
				Assert.InRange((double)topFrame.GetMaxY(), 0, (double)layoutNative.Bounds.Height + tolerance);

				var requiredHeight = intrinsicHeight - tolerance;
				double layoutHeight = layoutNative.Frame.Height;
				Assert.True(
					layoutHeight >= requiredHeight,
					$"AbsoluteLayout native height was below required default Button height: " +
					$"layout={layoutHeight:F2}, required={requiredHeight:F2}, " +
					$"bottom={bottomNative.Frame.Height:F2}, top={topNative.Frame.Height:F2}, tolerance={tolerance:F2}");
			});
		}

		static bool IsNativeDescendantOf(UIView child, UIView ancestor)
		{
			for (var current = child; current is not null; current = current.Superview)
			{
				if (current.Handle == ancestor.Handle)
					return true;
			}

			return false;
		}
	}
}
#endif

