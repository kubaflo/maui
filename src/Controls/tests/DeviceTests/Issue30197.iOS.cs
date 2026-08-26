#if MACCATALYST
using System;
using System.Globalization;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30197")]
	public class Issue30197 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task NativeHourCycleUpdatesAfterRuntimeCultureChange()
		{
			var originalCulture = CultureInfo.CurrentCulture;
			var originalUICulture = CultureInfo.CurrentUICulture;
			var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
			var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
			var initialCulture = CultureInfo.GetCultureInfo("en-US");
			var updatedCulture = CultureInfo.GetCultureInfo("ja-JP");
			var selectedTime = new TimeSpan(17, 30, 0);

			try
			{
				await InvokeOnMainThreadAsync(() =>
				{
					SetCulture(initialCulture);
					Assert.Equal(initialCulture.Name, CultureInfo.CurrentCulture.Name);
					Assert.Equal(initialCulture.Name, CultureInfo.CurrentUICulture.Name);
					Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
					Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);
				});

				EnsureHandlerCreated(builder =>
				{
					builder.ConfigureMauiHandlers(handlers =>
					{
						handlers.AddHandler<Window, WindowHandler>();
						handlers.AddHandler<Page, PageHandler>();
						handlers.AddHandler<IScrollView, ScrollViewHandler>();
						handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
						handlers.AddHandler<Label, LabelHandler>();
						handlers.AddHandler<Button, ButtonHandler>();
						handlers.AddHandler<TimePicker, TimePickerHandler>();
					});
				});

				var timePicker = new TimePicker
				{
					Time = selectedTime
				};
				Assert.Equal("t", timePicker.Format);
				var cultureStatusLabel = new Label
				{
					Text = "Culture: en-US"
				};
				var formatDescriptionLabel = new Label
				{
					FontAttributes = FontAttributes.Bold,
					Text = "TimePicker format"
				};
				var changeCultureButton = new Button
				{
					Text = "Change culture to ja-JP"
				};
				var recordResultButton = new Button
				{
					IsEnabled = false,
					Text = "Inspect current display"
				};
				var clickSentinel = -1;

				changeCultureButton.Clicked += (_, _) =>
				{
					SetCulture(updatedCulture);
					cultureStatusLabel.Text = "Culture: ja-JP";
					changeCultureButton.IsEnabled = false;
					recordResultButton.IsEnabled = true;
					clickSentinel = 1;
				};

				var layout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label { FontSize = 34, Text = "TimePicker runtime culture change" },
						new Label { Text = "Selected time: 17:30" },
						timePicker,
						cultureStatusLabel,
						formatDescriptionLabel,
						changeCultureButton,
						recordResultButton
					}
				};
				var page = new ContentPage
				{
					Title = "TimePicker runtime culture change",
					Content = new ScrollView { Content = layout }
				};

				await CreateHandlerAndAddToWindow(new Window(page), async () =>
				{
					var timePickerHandler = timePicker.Handler as TimePickerHandler;
					Assert.NotNull(timePickerHandler);
					var nativePicker = timePickerHandler.PlatformView;
					Assert.NotNull(nativePicker);
					Assert.NotNull(nativePicker.Window);
					Assert.Equal(UIDatePickerMode.Time, nativePicker.Mode);
					Assert.Equal(selectedTime.Hours, nativePicker.Date.ToDateTime().Hour);
					Assert.Equal(selectedTime.Minutes, nativePicker.Date.ToDateTime().Minute);

					var initialLocale = nativePicker.Locale;
					Assert.NotNull(initialLocale);
					Assert.Equal(GetExpectedHourCycle(initialCulture), GetNativeHourCycle(initialLocale));

					var originalHandle = nativePicker.Handle;
					var buttonHandler = changeCultureButton.Handler as ButtonHandler;
					Assert.NotNull(buttonHandler);
					buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

					await AssertEventually(
						() => clickSentinel == 1,
						message: "The culture-change button callback did not run");

					Assert.Equal(updatedCulture.Name, CultureInfo.CurrentCulture.Name);
					Assert.Equal(updatedCulture.Name, CultureInfo.CurrentUICulture.Name);
					Assert.Equal(updatedCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
					Assert.Equal(updatedCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);
					Assert.Equal(originalHandle, nativePicker.Handle);
					Assert.Same(nativePicker, timePickerHandler.PlatformView);
					Assert.NotNull(nativePicker.Window);
					Assert.Equal(selectedTime, timePicker.Time);
					Assert.Equal(selectedTime.Hours, nativePicker.Date.ToDateTime().Hour);
					Assert.Equal(selectedTime.Minutes, nativePicker.Date.ToDateTime().Minute);

					var updatedLocale = nativePicker.Locale;
					Assert.NotNull(updatedLocale);
					var expectedHourCycle = GetExpectedHourCycle(updatedCulture);
					var actualHourCycle = GetNativeHourCycle(updatedLocale);
					Assert.True(
						expectedHourCycle == actualHourCycle,
						$"TimePicker native hour cycle did not update after runtime culture change. " +
						$"Native locale: {updatedLocale.LocaleIdentifier}; native template cycle: {actualHourCycle}; " +
						$"expected culture: {updatedCulture.Name}; expected pattern: {updatedCulture.DateTimeFormat.ShortTimePattern}");
				});
			}
			finally
			{
				await InvokeOnMainThreadAsync(() =>
				{
					CultureInfo.CurrentCulture = originalCulture;
					CultureInfo.CurrentUICulture = originalUICulture;
					CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
					CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
				});
			}
		}

		static string GetExpectedHourCycle(CultureInfo culture) =>
			culture.DateTimeFormat.ShortTimePattern.Contains('H', StringComparison.Ordinal) ? "H" : "h";

		static string GetNativeHourCycle(NSLocale locale)
		{
			using var formatter = new NSDateFormatter
			{
				Locale = locale
			};
			formatter.SetLocalizedDateFormatFromTemplate("j");
			var format = formatter.DateFormat;
			Assert.False(string.IsNullOrEmpty(format));

			if (format.Contains('H', StringComparison.Ordinal))
				return "H";

			if (format.Contains('h', StringComparison.Ordinal))
				return "h";

			return format;
		}

		static void SetCulture(CultureInfo culture)
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
		}
	}
}
#endif

