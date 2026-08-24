#if IOS && !MACCATALYST
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
public class Issue36933 : ControlsHandlerTestBase
{
	[Fact]
	public async Task ClearingPickerBackgroundRestoresPlatformDefault()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<ContentPage, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<DatePicker, DatePickerHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var datePicker = new DatePicker { AutomationId = "AffectedDatePicker" };
		var timePicker = new TimePicker { AutomationId = "AffectedTimePicker" };
		var toggleButton = new Button
		{
			AutomationId = "ToggleButton",
			Text = "Set picker backgrounds"
		};
		var resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18,
			Text = "Picker background status"
		};
		var orangeBrush = new SolidColorBrush(Colors.Orange);
		var backgroundIsSet = false;

		toggleButton.Clicked += (sender, args) =>
		{
			if (!backgroundIsSet)
			{
				datePicker.Background = orangeBrush;
				timePicker.Background = orangeBrush;
				toggleButton.Text = "Clear picker backgrounds";
				backgroundIsSet = true;
				return;
			}

			datePicker.Background = null;
			timePicker.Background = null;
			toggleButton.Text = "Set picker backgrounds";
			backgroundIsSet = false;
		};

		var page = new ContentPage
		{
			Title = "Picker background reproduction",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 22,
						Text = "DatePicker and TimePicker background"
					},
					new Label { Text = "The first tap applies orange. The second tap clears both backgrounds." },
					new Label { Text = "DatePicker" },
					datePicker,
					new Label { Text = "TimePicker" },
					timePicker,
					toggleButton,
					resultLabel
				}
			}
		};

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			var dateHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
			var timeHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
			var buttonHandler = Assert.IsType<ButtonHandler>(toggleButton.Handler);
			var datePlatformView = Assert.IsType<MauiDatePicker>(dateHandler.PlatformView);
			var timePlatformView = Assert.IsType<MauiTimePicker>(timeHandler.PlatformView);
			var nativeButton = Assert.IsType<UIButton>(buttonHandler.PlatformView);
			var initialDateBackground = datePlatformView.BackgroundColor;
			var initialTimeBackground = timePlatformView.BackgroundColor;
			var initialDateStyle = datePlatformView.TraitCollection.UserInterfaceStyle;
			var initialTimeStyle = timePlatformView.TraitCollection.UserInterfaceStyle;

			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Same(orangeBrush, datePicker.Background);
			Assert.Same(orangeBrush, timePicker.Background);
			var expectedOrange = orangeBrush.Color.ToPlatform();
			await AssertEventually(() =>
			{
				var dateApplied = ColorsEqual(expectedOrange, datePlatformView.BackgroundColor);
				var timeApplied = ColorsEqual(expectedOrange, timePlatformView.BackgroundColor);
				return dateApplied & timeApplied;
			}, message: "The native picker backgrounds did not transition to the arranged orange brush.");

			var dateNullTransition = -1;
			var timeNullTransition = -1;
			datePicker.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(datePicker.Background) && datePicker.Background is null)
					dateNullTransition = 0;
			};
			timePicker.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(timePicker.Background) && timePicker.Background is null)
					timeNullTransition = 0;
			};

			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Null(datePicker.Background);
			Assert.Null(timePicker.Background);
			Assert.NotEqual(-1, dateNullTransition);
			Assert.NotEqual(-1, timeNullTransition);

			var observedDateBackground = datePlatformView.BackgroundColor;
			var observedTimeBackground = timePlatformView.BackgroundColor;
			await AssertEventually(() =>
			{
				observedDateBackground = datePlatformView.BackgroundColor;
				observedTimeBackground = timePlatformView.BackgroundColor;
				var dateRestored = ColorsEqual(initialDateBackground, observedDateBackground);
				var timeRestored = ColorsEqual(initialTimeBackground, observedTimeBackground);
				return dateRestored & timeRestored;
			}, message:
				$"Picker backgrounds were not restored after Background was set to null. " +
				$"DatePicker observed: {observedDateBackground}, default: {initialDateBackground}; " +
				$"TimePicker observed: {observedTimeBackground}, default: {initialTimeBackground}.");

			Assert.Equal(initialDateStyle, datePlatformView.TraitCollection.UserInterfaceStyle);
			Assert.Equal(initialTimeStyle, timePlatformView.TraitCollection.UserInterfaceStyle);
		});
	}

	static bool ColorsEqual(UIColor first, UIColor second)
	{
		if (object.ReferenceEquals(first, second))
			return true;

		return first is not null && second is not null && first.CGColor.Equals(second.CGColor);
	}
}
#endif

