#if IOS && !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Category(TestCategory.DatePicker)]
[Category("Issue36933")]
public class Issue36933 : ControlsHandlerTestBase
{
	const double ColorTolerance = 0.01;

	[Fact]
	public async Task ClearingBackgroundRestoresNativePickerBackgrounds()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<IScrollView, ScrollViewHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var datePicker = new DatePicker();
		var timePicker = new TimePicker();
		var instructionLabel = new Label
		{
			Text = "Use the button to apply and then clear the picker backgrounds.",
			FontSize = 18,
			FontAttributes = FontAttributes.Bold
		};
		var toggleButton = new Button { Text = "Apply red backgrounds" };
		var observedStage = -1;

		toggleButton.Clicked += (_, _) =>
		{
			if (observedStage < 1)
			{
				datePicker.Background = Colors.Red;
				timePicker.Background = Colors.Red;
				observedStage = 1;
				toggleButton.Text = "Clear backgrounds";
				return;
			}

			datePicker.Background = null;
			timePicker.Background = null;
			observedStage = 2;
		};

		var content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "DatePicker and TimePicker runtime Background clearing",
					FontSize = 22,
					FontAttributes = FontAttributes.Bold
				},
				new Label { Text = "DatePicker" },
				datePicker,
				new Label { Text = "TimePicker" },
				timePicker,
				toggleButton,
				instructionLabel
			}
		};
		var page = new ContentPage
		{
			Title = "Picker Background",
			Content = new ScrollView { Content = content }
		};

		await CreateHandlerAndAddToWindow(page, () =>
		{
			var datePickerHandler = Assert.IsAssignableFrom<DatePickerHandler>(datePicker.Handler);
			var timePickerHandler = Assert.IsAssignableFrom<TimePickerHandler>(timePicker.Handler);
			var buttonHandler = Assert.IsAssignableFrom<ButtonHandler>(toggleButton.Handler);
			Assert.NotNull(datePickerHandler.PlatformView);
			Assert.NotNull(timePickerHandler.PlatformView);
			Assert.NotNull(buttonHandler.PlatformView);

			var initialDatePickerColor = datePickerHandler.PlatformView.BackgroundColor;
			var initialTimePickerColor = timePickerHandler.PlatformView.BackgroundColor;
			Assert.False(
				ColorComparison.ARGBEquivalent(UIColor.Red, initialDatePickerColor, ColorTolerance),
				"DatePicker must begin with its platform-default background rather than red.");
			Assert.False(
				ColorComparison.ARGBEquivalent(UIColor.Red, initialTimePickerColor, ColorTolerance),
				"TimePicker must begin with its platform-default background rather than red.");

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, observedStage);
			Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(datePicker.Background).Color);
			Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(timePicker.Background).Color);
			Assert.True(
				ColorComparison.ARGBEquivalent(UIColor.Red, datePickerHandler.PlatformView.BackgroundColor, ColorTolerance),
				$"DatePicker native background was not red: {DescribeColor(datePickerHandler.PlatformView.BackgroundColor)}");
			Assert.True(
				ColorComparison.ARGBEquivalent(UIColor.Red, timePickerHandler.PlatformView.BackgroundColor, ColorTolerance),
				$"TimePicker native background was not red: {DescribeColor(timePickerHandler.PlatformView.BackgroundColor)}");

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(2, observedStage);
			Assert.Null(datePicker.Background);
			Assert.Null(timePicker.Background);

			var finalDatePickerColor = datePickerHandler.PlatformView.BackgroundColor;
			var finalTimePickerColor = timePickerHandler.PlatformView.BackgroundColor;
			var datePickerRestored = ColorComparison.ARGBEquivalent(initialDatePickerColor, finalDatePickerColor, ColorTolerance);
			var timePickerRestored = ColorComparison.ARGBEquivalent(initialTimePickerColor, finalTimePickerColor, ColorTolerance);

			Assert.True(
				datePickerRestored && timePickerRestored,
				$"Issue36933 native backgrounds were not restored after Background=null. " +
				$"DatePicker expected {DescribeColor(initialDatePickerColor)}, observed {DescribeColor(finalDatePickerColor)}; " +
				$"TimePicker expected {DescribeColor(initialTimePickerColor)}, observed {DescribeColor(finalTimePickerColor)}.");
		});
	}

	static string DescribeColor(UIColor color)
	{
		if (color is null)
			return "null";

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return $"rgba({red:F3},{green:F3},{blue:F3},{alpha:F3})";
	}
}
#endif

