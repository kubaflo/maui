#if MACCATALYST
using System;
using System.Globalization;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
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
		public async Task TimePickerUpdatesNativeLocaleWhenCultureChanges()
		{
			var originalCulture = CultureInfo.CurrentCulture;
			var originalUICulture = CultureInfo.CurrentUICulture;
			var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
			var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

			try
			{
				var englishCulture = CultureInfo.GetCultureInfo("en-US");
				var japaneseCulture = CultureInfo.GetCultureInfo("ja-JP");
				SetCultures(englishCulture);

				Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
				Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
				Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentCulture.Name);
				Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentUICulture.Name);

				EnsureHandlerCreated(builder =>
				{
					builder.ConfigureMauiHandlers(handlers =>
					{
						handlers.AddMauiControlsHandlers();
						handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
					});
				});

				var timePicker = new TimePicker
				{
					AutomationId = "AffectedTimePicker",
					Time = new TimeSpan(17, 30, 0),
				};
				var expectedFormatLabel = new Label
				{
					AutomationId = "ExpectedFormatLabel",
					Text = "Expected for en-US: 5:30 PM",
				};
				var changeCultureButton = new Button
				{
					AutomationId = "ChangeCultureButton",
					Text = "Change culture to ja-JP",
				};
				var callbackCount = 0;
				var callbackCurrentCulture = "not-observed";
				var callbackCurrentUICulture = "not-observed";
				var callbackDefaultCulture = "not-observed";
				var callbackDefaultUICulture = "not-observed";
				changeCultureButton.Clicked += (_, _) =>
				{
					SetCultures(japaneseCulture);
					expectedFormatLabel.Text = "Expected for ja-JP: 17:30";
					callbackCurrentCulture = CultureInfo.CurrentCulture.Name;
					callbackCurrentUICulture = CultureInfo.CurrentUICulture.Name;
					callbackDefaultCulture = CultureInfo.DefaultThreadCurrentCulture.Name;
					callbackDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture.Name;
					callbackCount++;
				};

				var layout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							FontSize = 22,
							Text = "TimePicker runtime culture change",
						},
						timePicker,
						expectedFormatLabel,
						changeCultureButton,
						new Button
						{
							AutomationId = "ConfirmButton",
							Text = "Confirm stale TimePicker text",
						},
						new Label
						{
							AutomationId = "ResultLabel",
							FontAttributes = FontAttributes.Bold,
							Text = "Culture status",
						},
					},
				};
				var page = new ContentPage
				{
					Title = "TimePicker culture",
					Content = layout,
				};

				using var expectedEnglishLocale = new NSLocale(englishCulture.TwoLetterISOLanguageName);
				using var expectedJapaneseLocale = new NSLocale(japaneseCulture.TwoLetterISOLanguageName);

				await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
				{
					Assert.NotNull(timePicker.Handler);
					var timePickerHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
					var nativeTimePicker = Assert.IsType<UIDatePicker>(timePickerHandler.PlatformView);
					Assert.NotNull(nativeTimePicker.Locale);
					Assert.True(
						LocaleMatches(nativeTimePicker.Locale, expectedEnglishLocale),
						$"Expected the initial native locale to be derived from en-US, but found '{nativeTimePicker.Locale.Identifier}'.");
					Assert.Equal(timePicker.Time, nativeTimePicker.Date.ToDateTime().TimeOfDay);

					Assert.NotNull(changeCultureButton.Handler);
					var buttonHandler = Assert.IsType<ButtonHandler>(changeCultureButton.Handler);
					var nativeButton = Assert.IsType<UIButton>(buttonHandler.PlatformView);
					var observedLocale = "not-observed";

					await InvokeOnMainThreadAsync(
						() => nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside));

					await AssertEventually(
						() => callbackCount == 1,
						message: "The culture-change button callback did not run exactly once.");
					Assert.Equal("ja-JP", callbackCurrentCulture);
					Assert.Equal("ja-JP", callbackCurrentUICulture);
					Assert.Equal("ja-JP", callbackDefaultCulture);
					Assert.Equal("ja-JP", callbackDefaultUICulture);
					Assert.Equal("Expected for ja-JP: 17:30", expectedFormatLabel.Text);
					Assert.Equal(new TimeSpan(17, 30, 0), timePicker.Time);
					Assert.Same(timePickerHandler, timePicker.Handler);
					Assert.Same(nativeTimePicker, timePickerHandler.PlatformView);

					var localeUpdated = await Wait(() =>
					{
						var locale = nativeTimePicker.Locale;
						observedLocale = locale?.Identifier ?? "<null>";
						return LocaleMatches(locale, expectedJapaneseLocale);
					}, timeout: 2000);

					Assert.True(
						localeUpdated,
						$"TimePicker native locale did not update after runtime culture changed to ja-JP. Actual: '{observedLocale}', expected: '{expectedJapaneseLocale.Identifier}'.");
				});
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;
				CultureInfo.CurrentUICulture = originalUICulture;
				CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
				CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
			}
		}

		static void SetCultures(CultureInfo culture)
		{
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}

		static bool LocaleMatches(NSLocale actual, NSLocale expected)
		{
			var actualIdentifier = actual?.Identifier ?? string.Empty;
			var expectedIdentifier = expected.Identifier;

			return actualIdentifier.Equals(expectedIdentifier, StringComparison.OrdinalIgnoreCase)
				|| actualIdentifier.StartsWith(expectedIdentifier + "_", StringComparison.OrdinalIgnoreCase)
				|| actualIdentifier.StartsWith(expectedIdentifier + "-", StringComparison.OrdinalIgnoreCase);
		}
	}
}
#endif

