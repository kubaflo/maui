#if IOS && !MACCATALYST
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

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30197")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue30197 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RuntimeCultureChangeRefreshesAttachedTimePicker()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var initialCulture = new CultureInfo("en-US");
			SetCultures(initialCulture);

			Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
			Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
			Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentCulture.Name);
			Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentUICulture.Name);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var arrangedTime = new TimeSpan(5, 30, 0);
			var timePicker = new TimePicker { Time = arrangedTime };
			var changeCultureButton = new Button { Text = "Change culture to ja-JP" };
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label { FontSize = 24, Text = "TimePicker culture" },
					new Label { Text = "Runtime culture change with the default time format." },
					timePicker,
					changeCultureButton,
					new Label { Text = "After changing culture, select another time in the picker." },
					new Label { FontAttributes = FontAttributes.Bold, Text = "Displayed time format" }
				}
			};
			var page = new ContentPage
			{
				Title = "TimePicker culture",
				Content = layout
			};

			var callbackCulture = "<not-called>";
			changeCultureButton.Clicked += (_, _) =>
			{
				var updatedCulture = new CultureInfo("ja-JP");
				SetCultures(updatedCulture);
				callbackCulture = CultureInfo.CurrentCulture.Name;
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var originalHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
				var originalNativeView = Assert.IsType<MauiTimePicker>(originalHandler.PlatformView);
				var originalNativePicker = originalNativeView.Picker;

				Assert.NotNull(originalNativePicker);
				var secondsFromGMT = originalNativePicker.TimeZone.SecondsFromGMT(NSDate.Now);
				Assert.True(
					secondsFromGMT == 0,
					$"Expected the native picker to use a zero GMT offset, but it used {secondsFromGMT} seconds.");

				var initialText = originalNativeView.Text;
				Assert.NotNull(initialText);

				var buttonHandler = Assert.IsType<ButtonHandler>(changeCultureButton.Handler);
				var nativeButton = Assert.IsType<UIButton>(buttonHandler.PlatformView);
				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				Assert.Equal("ja-JP", callbackCulture);
				Assert.Equal("ja-JP", CultureInfo.CurrentCulture.Name);
				Assert.Equal("ja-JP", CultureInfo.CurrentUICulture.Name);
				Assert.Equal("ja-JP", CultureInfo.DefaultThreadCurrentCulture.Name);
				Assert.Equal("ja-JP", CultureInfo.DefaultThreadCurrentUICulture.Name);
				Assert.Same(originalHandler, timePicker.Handler);
				Assert.Same(originalNativeView, originalHandler.PlatformView);
				Assert.Same(originalNativePicker, originalNativeView.Picker);

				var updatedCulture = CultureInfo.CurrentCulture;
				var expectedText = DateTime.Today
					.Add(arrangedTime)
					.ToString(updatedCulture.DateTimeFormat.ShortTimePattern, updatedCulture);
				Assert.NotEqual(initialText, expectedText);

				var observedText = "<not-observed>";
				var refreshed = await Wait(() =>
				{
					observedText = originalNativeView.Text ?? "<null>";
					return observedText == expectedText;
				});

				Assert.True(
					refreshed,
					$"TimePicker did not refresh its native text after the runtime culture change. Initial: '{initialText}', observed: '{observedText}', expected for {updatedCulture.Name}: '{expectedText}'.");
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
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}
}
#endif

