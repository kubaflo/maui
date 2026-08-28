#if !MACCATALYST
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30090")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue30090 : ControlsHandlerTestBase
{
	[Fact]
	public async Task NativeTextRefreshesAfterRuntimeCultureChange()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var initialCulture = CultureInfo.GetCultureInfo("en-US");
			var updatedCulture = CultureInfo.GetCultureInfo("de-DE");
			var testDate = new DateTime(2025, 12, 31);

			SetCulture(initialCulture);
			Assert.Equal(initialCulture.Name, CultureInfo.CurrentCulture.Name);
			Assert.Equal(initialCulture.Name, CultureInfo.CurrentUICulture.Name);
			Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
			Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);

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
				});
			});

			var currentCultureLabel = new Label { Text = "Current culture: en-US" };
			var expectedDateLabel = new Label();
			var datePicker = new DatePicker { Date = testDate };
			var renderedDateLabel = new Label { Text = "Rendered date: pending" };
			var changeCultureButton = new Button { Text = "Change culture to de-DE" };
			var resultLabel = new Label { Text = "Change culture to compare dates." };
			var clickCount = 0;

			expectedDateLabel.Text = $"Expected date: {testDate.ToString(datePicker.Format, initialCulture)}";
			changeCultureButton.Clicked += (_, _) =>
			{
				clickCount++;
				SetCulture(updatedCulture);
				currentCultureLabel.Text = $"Current culture: {CultureInfo.CurrentCulture.Name}";
				expectedDateLabel.Text = $"Expected date: {testDate.ToString(datePicker.Format, CultureInfo.CurrentCulture)}";
				resultLabel.Text = "Culture changed to de-DE.";
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "The DatePicker uses its default short-date format. Change the culture at runtime and compare the rendered date with the expected date."
					},
					currentCultureLabel,
					expectedDateLabel,
					datePicker,
					renderedDateLabel,
					changeCultureButton,
					resultLabel
				}
			};
			var page = new ContentPage { Content = layout };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
				var platformDatePicker = Assert.IsAssignableFrom<MauiDatePicker>(datePickerHandler.PlatformView);
				var initialExpectedText = testDate.ToString(datePicker.Format, initialCulture);
				var initialNativeText = platformDatePicker.Text ?? string.Empty;

				renderedDateLabel.Text = $"Rendered date: {initialNativeText}";
				Assert.Equal(initialExpectedText, initialNativeText);

				var buttonHandler = Assert.IsType<ButtonHandler>(changeCultureButton.Handler);
				buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				Assert.Equal(1, clickCount);
				Assert.Equal(updatedCulture.Name, CultureInfo.CurrentCulture.Name);
				Assert.Equal(updatedCulture.Name, CultureInfo.CurrentUICulture.Name);
				Assert.Equal(updatedCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
				Assert.Equal(updatedCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);

				var updatedExpectedText = testDate.ToString(datePicker.Format, CultureInfo.CurrentCulture);
				Assert.NotEqual(initialExpectedText, updatedExpectedText);

				var nativeText = platformDatePicker.Text ?? string.Empty;
				var refreshed = await Wait(() =>
				{
					nativeText = platformDatePicker.Text ?? string.Empty;
					return string.Equals(nativeText, updatedExpectedText, StringComparison.Ordinal);
				}, timeout: 2000, interval: 100);
				renderedDateLabel.Text = $"Rendered date: {nativeText}";
				Assert.True(
					refreshed,
					$"DatePicker native text did not refresh after runtime culture change: expected '{updatedExpectedText}', actual '{nativeText}'.");
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

	static void SetCulture(CultureInfo culture)
	{
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}
}
#endif

