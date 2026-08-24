#if IOS && !MACCATALYST
using System;
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

namespace Microsoft.Maui.DeviceTests;

[Category("Issue36933")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue36933 : ControlsHandlerTestBase
{
	const double ColorTolerance = 0.01;

	[Fact]
	public async Task PickerBackgroundsReturnToPlatformDefaultWhenCleared()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
				handlers.AddHandler<ScrollView, ScrollViewHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var datePicker = new DatePicker();
		var timePicker = new TimePicker();
		var button = new Button { Text = "Set backgrounds" };
		var clicksObserved = -1;
		button.Clicked += (sender, args) =>
		{
			clicksObserved++;
			Brush background = clicksObserved == 0 ? new SolidColorBrush(Colors.Orange) : null;
			datePicker.Background = background;
			timePicker.Background = background;
		};
		var page = new ContentPage
		{
			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 18,
					Children =
					{
						new Label { Text = "DatePicker and TimePicker background clearing", FontSize = 22, FontAttributes = FontAttributes.Bold },
						new Label { Text = "DatePicker" },
						datePicker,
						new Label { Text = "TimePicker" },
						timePicker,
						button,
					}
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			Assert.NotNull(datePicker.Handler);
			Assert.NotNull(timePicker.Handler);
			Assert.NotNull(button.Handler);

			var datePlatformView = Assert.IsAssignableFrom<UIView>(datePicker.Handler.PlatformView);
			var timePlatformView = Assert.IsAssignableFrom<UIView>(timePicker.Handler.PlatformView);
			var nativeButton = Assert.IsAssignableFrom<UIButton>(button.Handler.PlatformView);
			var initialDateBackground = datePlatformView.BackgroundColor;
			var initialTimeBackground = timePlatformView.BackgroundColor;
			var orange = Colors.Orange.ToPlatform();

			Assert.False(ColorComparison.ARGBEquivalent(initialDateBackground, orange, ColorTolerance));
			Assert.False(ColorComparison.ARGBEquivalent(initialTimeBackground, orange, ColorTolerance));

			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
			Assert.Equal(0, clicksObserved);

			await AssertEventually(
				() => ColorComparison.ARGBEquivalent(datePlatformView.BackgroundColor, orange, ColorTolerance)
					&& ColorComparison.ARGBEquivalent(timePlatformView.BackgroundColor, orange, ColorTolerance),
				message: "The native picker backgrounds did not update to orange.");

			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
			Assert.Equal(1, clicksObserved);

			Assert.True(
				ColorComparison.ARGBEquivalent(datePlatformView.BackgroundColor, initialDateBackground, ColorTolerance)
					&& ColorComparison.ARGBEquivalent(timePlatformView.BackgroundColor, initialTimeBackground, ColorTolerance),
				$"Picker native background did not restore its initial platform default after Background was set to null. " +
				$"DatePicker measured: {Describe(datePlatformView.BackgroundColor)}; expected: {Describe(initialDateBackground)}. " +
				$"TimePicker measured: {Describe(timePlatformView.BackgroundColor)}; expected: {Describe(initialTimeBackground)}.");
		});
	}

	static string Describe(UIColor color)
	{
		if (color is null)
			return "null";

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return $"RGBA({red:F4}, {green:F4}, {blue:F4}, {alpha:F4})";
	}
}
#endif

