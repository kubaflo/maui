#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue35512")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35512 : ControlsHandlerTestBase
	{
		const double ColorTolerance = 0.001;

		[Fact]
		public async Task ResettingBackgroundColorToNullRestoresImplicitStyle()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
				});
			});

			var defaultColor = Color.FromArgb("#512BD4");
			var implicitButtonStyle = new Style(typeof(Button));
			implicitButtonStyle.Setters.Add(new Setter
			{
				Property = VisualElement.BackgroundColorProperty,
				Value = defaultColor,
			});
			implicitButtonStyle.Setters.Add(new Setter
			{
				Property = Button.TextColorProperty,
				Value = Colors.White,
			});

			var affectedButton = new Button { Text = "Affected Button" };
			var referenceButton = new Button { Text = "Unchanged Reference Button" };
			var applyRedButton = new Button { Text = "Apply Red Background" };
			var resetNullButton = new Button { Text = "Reset Background To Null" };
			var applyRedClicked = 0;
			var resetNullClicked = 0;

			applyRedButton.Clicked += (_, _) =>
			{
				applyRedClicked++;
				affectedButton.BackgroundColor = Colors.Red;
			};
			resetNullButton.Clicked += (_, _) =>
			{
				resetNullClicked++;
				affectedButton.BackgroundColor = null;
			};

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Button BackgroundColor null reset",
						FontSize = 22,
						FontAttributes = FontAttributes.Bold,
					},
					new Label
					{
						Text = "Both buttons begin with the same implicit default style. Only the affected button is changed.",
						FontSize = 16,
					},
					affectedButton,
					referenceButton,
					applyRedButton,
					resetNullButton,
				},
			};
			var page = new ContentPage();
			page.Resources.Add(implicitButtonStyle);
			page.Content = new ScrollView { Content = stack };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var affectedHandler = Assert.IsType<ButtonHandler>(affectedButton.Handler);
				var referenceHandler = Assert.IsType<ButtonHandler>(referenceButton.Handler);
				var applyRedHandler = Assert.IsType<ButtonHandler>(applyRedButton.Handler);
				var resetNullHandler = Assert.IsType<ButtonHandler>(resetNullButton.Handler);
				var affectedNativeButton = Assert.IsType<UIButton>(affectedHandler.PlatformView);
				var referenceNativeButton = Assert.IsType<UIButton>(referenceHandler.PlatformView);
				var applyRedNativeButton = Assert.IsType<UIButton>(applyRedHandler.PlatformView);
				var resetNullNativeButton = Assert.IsType<UIButton>(resetNullHandler.PlatformView);

				var expectedDefault = GetRgba(defaultColor);
				var expectedRed = GetRgba(Colors.Red);
				Assert.False(ColorsEqual(expectedDefault, expectedRed));
				Assert.True(NativeColorEquals(affectedNativeButton.BackgroundColor, expectedDefault),
					"Affected Button did not initially use the arranged implicit background.");
				Assert.True(NativeColorEquals(referenceNativeButton.BackgroundColor, expectedDefault),
					"Reference Button did not initially use the arranged implicit background.");

				applyRedNativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(() => applyRedClicked == 1,
					message: "Apply Red Button Clicked callback did not run.");
				await AssertEventually(
					() => NativeColorEquals(affectedNativeButton.BackgroundColor, expectedRed),
					message: "Affected Button native background did not become red.");

				resetNullNativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(() => resetNullClicked == 1,
					message: "Reset Background To Null Button Clicked callback did not run.");
				Assert.Null(affectedButton.BackgroundColor);
				Assert.Same(affectedHandler, affectedButton.Handler);
				Assert.Same(referenceHandler, referenceButton.Handler);
				Assert.Same(affectedNativeButton, affectedHandler.PlatformView);
				Assert.Same(referenceNativeButton, referenceHandler.PlatformView);
				Assert.True(NativeColorEquals(referenceNativeButton.BackgroundColor, expectedDefault),
					"Unchanged reference Button no longer has the arranged implicit background.");

				var restored = await Wait(
					() => NativeColorEquals(affectedNativeButton.BackgroundColor, expectedDefault));
				Assert.True(restored,
					$"Button native background was not restored after BackgroundColor was reset to null. Expected {Format(expectedDefault)}, observed {Format(affectedNativeButton.BackgroundColor)}.");
			});
		}

		static (double Red, double Green, double Blue, double Alpha) GetRgba(Color color) =>
			(color.Red, color.Green, color.Blue, color.Alpha);

		static bool NativeColorEquals(
			UIColor color,
			(double Red, double Green, double Blue, double Alpha) expected)
		{
			if (color is null)
				return false;

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return ColorsEqual(((double)red, (double)green, (double)blue, (double)alpha), expected);
		}

		static bool ColorsEqual(
			(double Red, double Green, double Blue, double Alpha) left,
			(double Red, double Green, double Blue, double Alpha) right) =>
			Math.Abs(left.Red - right.Red) < ColorTolerance &&
			Math.Abs(left.Green - right.Green) < ColorTolerance &&
			Math.Abs(left.Blue - right.Blue) < ColorTolerance &&
			Math.Abs(left.Alpha - right.Alpha) < ColorTolerance;

		static string Format((double Red, double Green, double Blue, double Alpha) color) =>
			$"RGBA({color.Red:F3}, {color.Green:F3}, {color.Blue:F3}, {color.Alpha:F3})";

		static string Format(UIColor color)
		{
			if (color is null)
				return "<unavailable>";

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return Format(((double)red, (double)green, (double)blue, (double)alpha));
		}
	}
}
#endif

