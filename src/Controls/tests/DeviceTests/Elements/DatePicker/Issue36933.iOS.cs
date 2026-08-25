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

#if !MACCATALYST
[Category(TestCategory.DatePicker)]
public class Issue36933 : ControlsHandlerTestBase
{
	const double ColorTolerance = 0.01;

	[Fact]
	[Category("Issue36933")]
	public async Task NullBackgroundRestoresNativePickerDefaults()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<ScrollView, ScrollViewHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var datePicker = new DatePicker { Format = "D" };
		var timePicker = new TimePicker { Format = "t" };
		var stateLabel = new Label { Text = "Platform-default backgrounds" };
		var expectedLabel = new Label { Text = "Expected: platform-default backgrounds after clearing" };
		var toggleButton = new Button { Text = "Apply Orange Background" };
		var backgroundApplied = false;
		var dateNullTransition = -1;
		var timeNullTransition = -1;

		datePicker.PropertyChanged += (_, args) =>
		{
			if (dateNullTransition == 0 &&
				args.PropertyName == nameof(VisualElement.Background) &&
				datePicker.Background is null)
			{
				dateNullTransition = 1;
			}
		};
		timePicker.PropertyChanged += (_, args) =>
		{
			if (timeNullTransition == 0 &&
				args.PropertyName == nameof(VisualElement.Background) &&
				timePicker.Background is null)
			{
				timeNullTransition = 1;
			}
		};

		toggleButton.Clicked += (_, _) =>
		{
			if (!backgroundApplied)
			{
				var orangeBrush = new SolidColorBrush(Colors.Orange);
				datePicker.Background = orangeBrush;
				timePicker.Background = orangeBrush;
				backgroundApplied = true;
				toggleButton.Text = "Clear Background";
				stateLabel.Text = "Orange background applied";
				return;
			}

			datePicker.Background = null;
			timePicker.Background = null;
			stateLabel.Text = "Background set to null";
		};

		var stack = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 18,
			Children =
			{
				new Label { Text = "DatePicker and TimePicker background", FontSize = 22, FontAttributes = FontAttributes.Bold },
				new Label { Text = "Apply an orange background, then clear it by setting Background to null.", FontSize = 16 },
				new Label { Text = "DatePicker", FontAttributes = FontAttributes.Bold },
				datePicker,
				new Label { Text = "TimePicker", FontAttributes = FontAttributes.Bold },
				timePicker,
				stateLabel,
				expectedLabel,
				toggleButton
			}
		};
		var page = new ContentPage
		{
			Title = "Picker Background Test",
			Content = new ScrollView { Content = stack }
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			Assert.Equal("D", datePicker.Format);
			Assert.Equal("t", timePicker.Format);

			var dateHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			var timeHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var initialDateBackground = dateHandler.PlatformView.BackgroundColor;
			var initialTimeBackground = timeHandler.PlatformView.BackgroundColor;

			var defaultDateColor = ReadColor(initialDateBackground);
			var defaultTimeColor = ReadColor(initialTimeBackground);
			var orangeColor = ReadColor(Colors.Orange.ToPlatform());
			Assert.False(ColorClose(defaultDateColor, orangeColor), "DatePicker default background unexpectedly matched orange.");
			Assert.False(ColorClose(defaultTimeColor, orangeColor), "TimePicker default background unexpectedly matched orange.");

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);
			await AssertEventually(
				() => ColorClose(ReadColor(dateHandler.PlatformView.BackgroundColor), orangeColor) &&
					ColorClose(ReadColor(timeHandler.PlatformView.BackgroundColor), orangeColor),
				message: "Both native picker backgrounds should become orange after the first button activation.");

			dateNullTransition = 0;
			timeNullTransition = 0;
			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, dateNullTransition);
			Assert.Equal(1, timeNullTransition);
			Assert.Null(datePicker.Background);
			Assert.Null(timePicker.Background);

			await AssertEventually(
				() => ColorClose(ReadColor(dateHandler.PlatformView.BackgroundColor), defaultDateColor) &&
					ColorClose(ReadColor(timeHandler.PlatformView.BackgroundColor), defaultTimeColor),
				message: $"Native picker backgrounds after Background=null: DatePicker {FormatColor(dateHandler.PlatformView.BackgroundColor)} expected {FormatColor(initialDateBackground)}; TimePicker {FormatColor(timeHandler.PlatformView.BackgroundColor)} expected {FormatColor(initialTimeBackground)}.");
		});
	}

	static (bool HasColor, double Red, double Green, double Blue, double Alpha) ReadColor(UIColor color)
	{
		if (color is null)
			return (false, 0, 0, 0, 0);

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return (true, (double)red, (double)green, (double)blue, (double)alpha);
	}

	static bool ColorClose(
		(bool HasColor, double Red, double Green, double Blue, double Alpha) actual,
		(bool HasColor, double Red, double Green, double Blue, double Alpha) expected) =>
		actual.HasColor == expected.HasColor &&
		(!actual.HasColor ||
		(Math.Abs(actual.Red - expected.Red) <= ColorTolerance &&
		Math.Abs(actual.Green - expected.Green) <= ColorTolerance &&
		Math.Abs(actual.Blue - expected.Blue) <= ColorTolerance &&
		Math.Abs(actual.Alpha - expected.Alpha) <= ColorTolerance));

	static string FormatColor(UIColor color)
	{
		var value = ReadColor(color);
		return value.HasColor
			? $"rgba({value.Red:F3}, {value.Green:F3}, {value.Blue:F3}, {value.Alpha:F3})"
			: "null";
	}
}
#endif

