using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Layout)]
	[Category("Issue17673")]
	public class Issue17673 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task AbsoluteLayoutPreservesButtonsIntrinsicMinimumHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var showButton = new Button { Text = "Show reported layout" };
			var checkButton = new Button { Text = "Check layout height" };
			var measurementLabel = new Label { Text = "The layout height is measured after creation." };
			var statusLabel = new Label { Text = "Layout status", FontAttributes = FontAttributes.Bold };
			var scenarioHost = new StackLayout { Spacing = 0 };
			var root = new StackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 18,
						Text = "AbsoluteLayout proportional child sizing"
					},
					new Label
					{
						Text = "Create the reported two-button layout, then check its arranged height."
					},
					showButton,
					checkButton,
					measurementLabel,
					statusLabel,
					scenarioHost
				}
			};
			var page = new ContentPage { Content = root };

			int showClicks = 0;
			int checkClicks = 0;
			bool loaded = false;
			int sizeChangedCount = 0;
			double observedNativeHeight = -1;
			AbsoluteLayout reportedLayout = null;
			Button bottomButton = null;
			Button topButton = null;

			showButton.Clicked += (_, _) =>
			{
				showClicks++;
				bottomButton = new Button { Text = "Bottom Button" };
				topButton = new Button { Text = "Click Me!", InputTransparent = false };
				reportedLayout = new AbsoluteLayout { bottomButton, topButton };

				AbsoluteLayout.SetLayoutFlags(bottomButton, AbsoluteLayoutFlags.All);
				AbsoluteLayout.SetLayoutBounds(bottomButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutBounds(topButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutFlags(topButton, AbsoluteLayoutFlags.All);

				reportedLayout.Loaded += (_, _) => loaded = true;
				reportedLayout.SizeChanged += (_, _) => sizeChangedCount++;
				scenarioHost.Children.Clear();
				scenarioHost.Children.Add(reportedLayout);
			};

			checkButton.Clicked += (_, _) =>
			{
				checkClicks++;
				var nativeLayout = Assert.IsAssignableFrom<UIView>(reportedLayout.Handler.PlatformView);
				observedNativeHeight = nativeLayout.Bounds.Height;
			};

			await AttachAndRun(page, async _ =>
			{
				var calibrationBottom = new Button { Text = "Bottom Button" };
				var calibrationTop = new Button { Text = "Click Me!", InputTransparent = false };
				var calibrationLayout = new AbsoluteLayout { calibrationBottom, calibrationTop };
				var autoSizeBounds = new Rect(0, 0, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize);
				AbsoluteLayout.SetLayoutBounds(calibrationBottom, autoSizeBounds);
				AbsoluteLayout.SetLayoutBounds(calibrationTop, autoSizeBounds);
				scenarioHost.Children.Add(calibrationLayout);

				await AssertionExtensions.AssertEventually(
					() => calibrationLayout.Handler is not null &&
						calibrationBottom.Handler is not null &&
						calibrationTop.Handler is not null &&
						calibrationLayout.Height > 0,
					message: "Calibration AbsoluteLayout did not complete native layout");

				var calibrationNativeLayout = Assert.IsAssignableFrom<UIView>(calibrationLayout.Handler.PlatformView);
				var calibrationNativeBottom = Assert.IsAssignableFrom<UIButton>(calibrationBottom.Handler.PlatformView);
				var calibrationNativeTop = Assert.IsAssignableFrom<UIButton>(calibrationTop.Handler.PlatformView);
				Assert.Equal("Bottom Button", calibrationBottom.Text);
				Assert.Equal("Click Me!", calibrationTop.Text);
				Assert.Same(calibrationBottom, calibrationLayout.Children[0]);
				Assert.Same(calibrationTop, calibrationLayout.Children[1]);
				Assert.NotNull(calibrationNativeBottom.Superview);
				Assert.NotNull(calibrationNativeTop.Superview);

				double calibrationMinimum = Math.Max(
					calibrationNativeBottom.IntrinsicContentSize.Height,
					calibrationNativeTop.IntrinsicContentSize.Height);
				Assert.True(calibrationNativeLayout.Bounds.Height + 0.5 >= calibrationMinimum);

				scenarioHost.Children.Remove(calibrationLayout);
				await AssertionExtensions.AssertEventually(
					() => scenarioHost.Children.Count == 0,
					message: "Calibration layout was not removed");

				var nativeShowButton = Assert.IsAssignableFrom<UIButton>(showButton.Handler.PlatformView);
				nativeShowButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertionExtensions.AssertEventually(() => showClicks == 1, message: "Show callback did not run");
				await AssertionExtensions.AssertEventually(() => scenarioHost.Children.Count == 1, message: "Reported layout was not added");
				await AssertionExtensions.AssertEventually(() => loaded, message: "Reported layout did not load");
				await AssertionExtensions.AssertEventually(() => sizeChangedCount > 0, message: "Reported layout did not raise SizeChanged");
				await AssertionExtensions.AssertEventually(
					() => reportedLayout.Handler is not null &&
						bottomButton.Handler is not null &&
						topButton.Handler is not null,
					message: "Reported controls did not receive native handlers");
				await AssertionExtensions.AssertEventually(
					() => ((UIView)reportedLayout.Handler.PlatformView).Frame.Height > 0,
					message: "Reported AbsoluteLayout did not receive a nonzero native frame");

				Assert.Equal(new Rect(0, 0, 1, 1), AbsoluteLayout.GetLayoutBounds(bottomButton));
				Assert.Equal(new Rect(0, 0, 1, 1), AbsoluteLayout.GetLayoutBounds(topButton));
				Assert.Equal(AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(bottomButton));
				Assert.Equal(AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(topButton));
				Assert.False(topButton.InputTransparent);

				var nativeBottom = Assert.IsAssignableFrom<UIButton>(bottomButton.Handler.PlatformView);
				var nativeTop = Assert.IsAssignableFrom<UIButton>(topButton.Handler.PlatformView);
				Assert.Same(bottomButton, reportedLayout.Children[0]);
				Assert.Same(topButton, reportedLayout.Children[1]);
				Assert.NotNull(nativeBottom.Superview);
				Assert.NotNull(nativeTop.Superview);

				var nativeCheckButton = Assert.IsAssignableFrom<UIButton>(checkButton.Handler.PlatformView);
				nativeCheckButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertionExtensions.AssertEventually(() => checkClicks == 1, message: "Check callback did not run");
				await AssertionExtensions.AssertEventually(
					() => observedNativeHeight >= 0,
					message: "Check callback did not capture the native height");

				double expectedMinimum = Math.Max(
					nativeBottom.IntrinsicContentSize.Height,
					nativeTop.IntrinsicContentSize.Height);
				Assert.True(
					observedNativeHeight + 0.5 >= expectedMinimum,
					$"AbsoluteLayout native height did not preserve its buttons' intrinsic minimum: observed {observedNativeHeight:0.###}, expected at least {expectedMinimum:0.###} with tolerance 0.5.");
			});
		}
	}
#endif
}

