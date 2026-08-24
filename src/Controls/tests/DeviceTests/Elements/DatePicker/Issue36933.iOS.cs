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
	[Fact]
	public async Task PickerBackgroundReturnsToPlatformDefaultWhenCleared()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var redBrush = new SolidColorBrush(Colors.Red);
		var datePicker = new DatePicker();
		var timePicker = new TimePicker();
		var toggleButton = new Button { Text = "Set picker backgrounds" };
		var transition = -1;

		toggleButton.Clicked += (_, _) =>
		{
			if (transition == -1)
			{
				datePicker.Background = redBrush;
				timePicker.Background = redBrush;
				toggleButton.Text = "Clear picker backgrounds";
				transition = 1;
				return;
			}

			datePicker.Background = null;
			timePicker.Background = null;
			toggleButton.Text = "Backgrounds cleared";
			transition = 2;
		};

		var layout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					FontSize = 20,
					Text = "DatePicker and TimePicker background reset"
				},
				datePicker,
				timePicker,
				toggleButton,
				new Label
				{
					FontAttributes = FontAttributes.Bold,
					Text = "Picker background status"
				}
			}
		};
		var page = new ContentPage { Content = layout };

		await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
		{
			var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			var timePickerHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var nativeDatePicker = Assert.IsType<MauiDatePicker>(datePickerHandler.PlatformView);
			var nativeTimePicker = Assert.IsType<MauiTimePicker>(timePickerHandler.PlatformView);
			var nativeButton = Assert.IsType<UIButton>(buttonHandler.PlatformView);
			var initialDateColor = nativeDatePicker.BackgroundColor;
			var initialTimeColor = nativeTimePicker.BackgroundColor;

			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.True(await Wait(() => transition == 1), "The first picker background callback did not run.");
			Assert.Equal("Clear picker backgrounds", toggleButton.Text);
			Assert.Same(redBrush, datePicker.Background);
			Assert.Same(redBrush, timePicker.Background);
			Assert.True(
				await Wait(() => ColorsMatch(nativeDatePicker.BackgroundColor, UIColor.Red)),
				"DatePicker did not receive the red native background.");
			Assert.True(
				await Wait(() => ColorsMatch(nativeTimePicker.BackgroundColor, UIColor.Red)),
				"TimePicker did not receive the red native background.");

			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.True(await Wait(() => transition == 2), "The second picker background callback did not run.");
			Assert.Null(datePicker.Background);
			Assert.Null(timePicker.Background);
			Assert.Equal("Backgrounds cleared", toggleButton.Text);

			var datePickerReset = await Wait(() => ColorsMatch(nativeDatePicker.BackgroundColor, initialDateColor));
			var timePickerReset = await Wait(() => ColorsMatch(nativeTimePicker.BackgroundColor, initialTimeColor));

			Assert.True(
				datePickerReset &&
				timePickerReset &&
				ColorsMatch(nativeDatePicker.BackgroundColor, initialDateColor) &&
				ColorsMatch(nativeTimePicker.BackgroundColor, initialTimeColor),
				$"Issue36933 picker background reset failed: DatePicker observed {DescribeColor(nativeDatePicker.BackgroundColor)}, expected {DescribeColor(initialDateColor)}; TimePicker observed {DescribeColor(nativeTimePicker.BackgroundColor)}, expected {DescribeColor(initialTimeColor)}.");
		});
	}

	static bool ColorsMatch(UIColor first, UIColor second) =>
		ColorComparison.ARGBEquivalent(first, second, tolerance: 0.01);

	static string DescribeColor(UIColor color)
	{
		if (color is null)
			return "nil";

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return $"rgba({red:F3}, {green:F3}, {blue:F3}, {alpha:F3})";
	}
}
#endif

