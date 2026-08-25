using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

#if IOS && !MACCATALYST
[Category("Issue36933")]
[Category(TestCategory.DatePicker)]
public class Issue36933 : ControlsHandlerTestBase
{
	const double ColorTolerance = 0.01;

	[Fact]
	public async Task ClearingPickerBackgroundRestoresNativeDefault()
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

		var datePicker = new DatePicker();
		var timePicker = new TimePicker();
		var toggleButton = new Button { Text = "Toggle picker backgrounds" };
		var backgroundApplied = false;

		toggleButton.Clicked += (_, _) =>
		{
			if (!backgroundApplied)
			{
				datePicker.Background = new SolidColorBrush(Colors.Red);
				timePicker.Background = new SolidColorBrush(Colors.Red);
				backgroundApplied = true;
				return;
			}

			datePicker.Background = null;
			timePicker.Background = null;
			backgroundApplied = false;
		};

		var page = new ContentPage
		{
			Title = "Picker background test",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "DatePicker and TimePicker background reset",
						FontSize = 20,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "Both pickers start with their platform-default background. The first tap makes them red; the second requests the default background again.",
						LineBreakMode = LineBreakMode.WordWrap
					},
					datePicker,
					timePicker,
					toggleButton,
					new Label
					{
						Text = "The picker backgrounds are toggled together.",
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "The native backgrounds should return to their initial appearance.",
						FontAttributes = FontAttributes.Bold
					}
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, () =>
		{
			var dateHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			var timeHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var nativeDatePicker = dateHandler.PlatformView;
			var nativeTimePicker = timeHandler.PlatformView;

			Assert.NotNull(nativeDatePicker);
			Assert.NotNull(nativeTimePicker);
			Assert.NotNull(nativeDatePicker.Window);
			Assert.NotNull(nativeTimePicker.Window);

			var defaultDateBackground = nativeDatePicker.BackgroundColor;
			var defaultTimeBackground = nativeTimePicker.BackgroundColor;
			Assert.False(
				ColorComparison.ARGBEquivalent(defaultDateBackground, UIColor.Red, ColorTolerance),
				"DatePicker platform-default background must differ from red.");
			Assert.False(
				ColorComparison.ARGBEquivalent(defaultTimeBackground, UIColor.Red, ColorTolerance),
				"TimePicker platform-default background must differ from red.");

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			var dateBrush = Assert.IsType<SolidColorBrush>(datePicker.Background);
			var timeBrush = Assert.IsType<SolidColorBrush>(timePicker.Background);
			Assert.Equal(Colors.Red, dateBrush.Color);
			Assert.Equal(Colors.Red, timeBrush.Color);
			Assert.True(ColorComparison.ARGBEquivalent(nativeDatePicker.BackgroundColor, UIColor.Red, ColorTolerance));
			Assert.True(ColorComparison.ARGBEquivalent(nativeTimePicker.BackgroundColor, UIColor.Red, ColorTolerance));

			var dateClearCallback = -1;
			var timeClearCallback = -1;
			PropertyChangedEventHandler dateBackgroundChanged = (_, args) =>
			{
				if (args.PropertyName == nameof(DatePicker.Background) && datePicker.Background is null)
					dateClearCallback = 1;
			};
			PropertyChangedEventHandler timeBackgroundChanged = (_, args) =>
			{
				if (args.PropertyName == nameof(TimePicker.Background) && timePicker.Background is null)
					timeClearCallback = 1;
			};
			datePicker.PropertyChanged += dateBackgroundChanged;
			timePicker.PropertyChanged += timeBackgroundChanged;

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, dateClearCallback);
			Assert.Equal(1, timeClearCallback);
			Assert.Null(datePicker.Background);
			Assert.Null(timePicker.Background);

			var clearedDateBackground = nativeDatePicker.BackgroundColor;
			var clearedTimeBackground = nativeTimePicker.BackgroundColor;
			Assert.True(
				ColorComparison.ARGBEquivalent(clearedDateBackground, defaultDateBackground, ColorTolerance),
				$"DatePicker native background remained red after Background was cleared. Expected: {DescribeColor(defaultDateBackground)}; Actual: {DescribeColor(clearedDateBackground)}");
			Assert.True(
				ColorComparison.ARGBEquivalent(clearedTimeBackground, defaultTimeBackground, ColorTolerance),
				$"TimePicker native background remained red after Background was cleared. Expected: {DescribeColor(defaultTimeBackground)}; Actual: {DescribeColor(clearedTimeBackground)}");

			return Task.CompletedTask;
		});
	}

	static string DescribeColor(UIColor color) => color?.ToString() ?? "<null>";
}
#endif

