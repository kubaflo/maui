#if IOS && !MACCATALYST
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

public class Issue30090 : ControlsHandlerTestBase
{
	[Fact]
	[Category("Issue30090")]
	public async Task DefaultFormatUpdatesAfterRuntimeCultureChange()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
		var enUsCulture = CultureInfo.GetCultureInfo("en-US");
		var frFrCulture = CultureInfo.GetCultureInfo("fr-FR");

		try
		{
			SetCulture(enUsCulture);
			Assert.Equal(enUsCulture, CultureInfo.CurrentCulture);
			Assert.Equal(enUsCulture, CultureInfo.CurrentUICulture);
			Assert.Equal(enUsCulture, CultureInfo.DefaultThreadCurrentCulture);
			Assert.Equal(enUsCulture, CultureInfo.DefaultThreadCurrentUICulture);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<DatePicker, DatePickerHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var arrangedDate = new DateTime(2026, 12, 24);
			var datePicker = new DatePicker
			{
				Date = arrangedDate
			};
			var expectedLabel = new Label
			{
				Text = $"Expected fr-FR display: {arrangedDate.ToString("d", frFrCulture)}"
			};
			var observedLabel = new Label
			{
				Text = $"Displayed before change: {arrangedDate.ToString("d", enUsCulture)}"
			};
			var cultureDescriptionLabel = new Label
			{
				Text = "The culture changes from en-US to fr-FR."
			};
			var changeCultureButton = new Button
			{
				Text = "Change culture to fr-FR"
			};
			var expectedBehaviorLabel = new Label
			{
				Text = "The DatePicker display should refresh automatically.",
				FontAttributes = FontAttributes.Bold
			};
			var callbackToken = "not-fired";

			changeCultureButton.Clicked += (_, _) =>
			{
				SetCulture(frFrCulture);
				callbackToken = "culture-changed";
			};

			var innerLayout = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 12,
				Children =
				{
					new Label
					{
						Text = "The DatePicker starts under en-US. Change the culture to fr-FR without selecting another date."
					},
					datePicker,
					expectedLabel,
					observedLabel,
					cultureDescriptionLabel,
					changeCultureButton,
					expectedBehaviorLabel
				}
			};
			var outerLayout = new VerticalStackLayout
			{
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "DatePicker runtime culture change",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					innerLayout
				}
			};
			var page = new ContentPage
			{
				Content = outerLayout
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
				var platformDatePicker = datePickerHandler.PlatformView;
				Assert.NotNull(platformDatePicker);

				var buttonHandler = Assert.IsType<ButtonHandler>(changeCultureButton.Handler);
				var platformButton = Assert.IsAssignableFrom<UIButton>(buttonHandler.PlatformView);
				var enUsText = arrangedDate.ToString("d", enUsCulture);
				var frFrText = arrangedDate.ToString("d", frFrCulture);

				Assert.NotEqual(enUsText, frFrText);
				Assert.Equal(enUsText, platformDatePicker.Text);

				platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertEventually(
					() => callbackToken == "culture-changed",
					message: "The attached Button did not invoke its Clicked callback.");
				Assert.Equal(frFrCulture, CultureInfo.CurrentCulture);
				Assert.Equal(frFrCulture, CultureInfo.CurrentUICulture);
				Assert.Equal(frFrCulture, CultureInfo.DefaultThreadCurrentCulture);
				Assert.Equal(frFrCulture, CultureInfo.DefaultThreadCurrentUICulture);
				Assert.Same(datePickerHandler, datePicker.Handler);
				Assert.Same(platformDatePicker, datePickerHandler.PlatformView);
				Assert.Equal(arrangedDate, datePicker.Date);

				await AssertEventually(
					() => platformDatePicker.Text == frFrText,
					message: $"DatePicker native text did not update after runtime culture change: expected '{frFrText}', observed '{platformDatePicker.Text}'.");
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

