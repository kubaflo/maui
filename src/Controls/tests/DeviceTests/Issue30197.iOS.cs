#if MACCATALYST
using System;
using System.Globalization;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30197")]
public class Issue30197 : ControlsHandlerTestBase
{
	[Fact]
	public async Task TimePickerUpdatesNativeHourCycleAfterCultureChanges()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<ContentPage, PageHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<ScrollView, ScrollViewHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var timePicker = new TimePicker
			{
				Time = new TimeSpan(13, 5, 0)
			};
			var stackLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "TimePicker runtime culture change", FontSize = 24, FontAttributes = FontAttributes.Bold },
					new Label { Text = "The TimePicker below uses its platform-default styling and default culture-sensitive format." },
					timePicker,
					new Label { Text = "Initial rendered style: detecting" },
					new Label { Text = "Target culture: not changed" },
					new Label { Text = "Rendered TimePicker style after change: not checked" },
					new Button { Text = "Change culture" },
					new Button { Text = "Check rendered format" },
					new Label { Text = "Rendered format result pending", FontAttributes = FontAttributes.Bold }
				}
			};
			var scrollView = new ScrollView { Content = stackLayout };
			var page = new ContentPage { Content = scrollView };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.True(page.IsLoaded);
				Assert.True(scrollView.IsLoaded);
				Assert.True(stackLayout.IsLoaded);
				Assert.True(timePicker.IsLoaded);
				Assert.Equal("t", timePicker.Format);

				var initialHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
				var initialPicker = Assert.IsType<UIDatePicker>(initialHandler.PlatformView);
				var initialCycle = Uses24HourClock(initialPicker);
				var initialCulture = CultureInfo.GetCultureInfo(initialCycle == 1 ? "de-DE" : "en-US");
				var targetCulture = CultureInfo.GetCultureInfo(initialCycle == 1 ? "en-US" : "de-DE");
				var expectedInitialCycle = Uses24HourClock(initialCulture);
				var expectedTargetCycle = Uses24HourClock(targetCulture);

				SetCurrentCultures(initialCulture);
				AssertCurrentCultures(initialCulture);
				Assert.Equal(expectedInitialCycle, Uses24HourClock(initialPicker));
				Assert.NotEqual(expectedInitialCycle, expectedTargetCycle);

				var originalTime = timePicker.Time;
				var refreshCallbackObserved = false;

				SetCurrentCultures(targetCulture);
				AssertCurrentCultures(targetCulture);
				await InvokeOnMainThreadAsync(() => refreshCallbackObserved = true);
				await AssertEventually(
					() => refreshCallbackObserved,
					message: "The post-culture-change UI callback was not observed.");

				Assert.Same(initialHandler, timePicker.Handler);
				Assert.Same(initialPicker, initialHandler.PlatformView);
				Assert.Equal(originalTime, timePicker.Time);
				Assert.Equal("t", timePicker.Format);

				var observedCycle = await ObserveHourCycleAsync(initialPicker, expectedTargetCycle);

				Assert.NotEqual(-1, observedCycle);
				Assert.True(
					observedCycle == expectedTargetCycle,
					$"TimePicker native hour cycle remained stale after runtime culture change. Initial: {DescribeHourCycle(initialCycle == 1)}; target: {targetCulture.Name}; observed: {DescribeHourCycle(observedCycle == 1)}; expected: {DescribeHourCycle(expectedTargetCycle == 1)}.");
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

	static async Task<int> ObserveHourCycleAsync(UIDatePicker picker, int expectedCycle)
	{
		var observedCycle = -1;

		for (var attempt = 0; attempt < 10 && observedCycle != expectedCycle; attempt++)
		{
			observedCycle = await InvokeOnMainThreadAsync(() => Uses24HourClock(picker));
			await Task.Yield();
		}

		return observedCycle;
	}

	static int Uses24HourClock(UIDatePicker picker)
	{
		using var formatter = new NSDateFormatter
		{
			Locale = picker.Locale ?? NSLocale.CurrentLocale
		};
		formatter.SetLocalizedDateFormatFromTemplate("j");
		var format = formatter.DateFormat ?? string.Empty;
		return format.Contains("H", StringComparison.Ordinal) || format.Contains("k", StringComparison.Ordinal) ? 1 : 0;
	}

	static int Uses24HourClock(CultureInfo culture)
	{
		using var formatter = new NSDateFormatter
		{
			Locale = new NSLocale(culture.Name)
		};
		formatter.SetLocalizedDateFormatFromTemplate("j");
		var format = formatter.DateFormat ?? string.Empty;
		return format.Contains("H", StringComparison.Ordinal) || format.Contains("k", StringComparison.Ordinal) ? 1 : 0;
	}

	static string DescribeHourCycle(bool uses24Hour) => uses24Hour ? "24-hour" : "12-hour";

	static void SetCurrentCultures(CultureInfo culture)
	{
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	static void AssertCurrentCultures(CultureInfo culture)
	{
		Assert.Equal(culture.Name, CultureInfo.CurrentCulture.Name);
		Assert.Equal(culture.Name, CultureInfo.CurrentUICulture.Name);
		Assert.Equal(culture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
		Assert.Equal(culture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);
	}
}
#endif

