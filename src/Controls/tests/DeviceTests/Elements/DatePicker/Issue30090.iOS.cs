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

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30090")]
public class Issue30090 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RuntimeCultureChangeRefreshesNativeText()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		var previousUICulture = CultureInfo.CurrentUICulture;
		var previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var previousDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var initialCulture = new CultureInfo("en-US");
			var targetCulture = new CultureInfo("fr-FR");
			SetCulture(initialCulture);

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
				});
			});

			var sampleDate = new DateTime(2026, 12, 24);
			var datePicker = new DatePicker
			{
				Date = sampleDate,
			};
			var clickToken = -1;
			var cultureButton = new Button
			{
				Text = "Change culture to fr-FR",
			};
			cultureButton.Clicked += (_, _) =>
			{
				SetCulture(targetCulture);
				clickToken = 30090;
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 14,
				Children =
				{
					new Label
					{
						Text = "DatePicker runtime culture change",
						FontAttributes = FontAttributes.Bold,
						FontSize = 22,
					},
					new Label { Text = "The DatePicker below uses its default Format and styling." },
					datePicker,
					new Label { Text = "Initial display: not available" },
					new Label { Text = "Active culture: en-US" },
					new Label { Text = $"Expected after change: {sampleDate.ToString("d", targetCulture)}" },
					cultureButton,
					new Button { Text = "Check displayed date" },
					new Label { Text = "Displayed date: not checked" },
					new Label
					{
						Text = "DatePicker display after culture change",
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
					},
				},
			};
			var page = new ContentPage
			{
				Title = "DatePicker culture refresh",
				Content = new ScrollView { Content = layout },
			};

			await AttachAndRun(page, async _ =>
			{
				var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
				var nativeDatePicker = Assert.IsAssignableFrom<UITextField>(datePickerHandler.PlatformView);
				var buttonHandler = Assert.IsType<ButtonHandler>(cultureButton.Handler);
				var nativeButton = Assert.IsAssignableFrom<UIButton>(buttonHandler.PlatformView);

				Assert.True(datePicker.Date.HasValue);
				var initialText = datePicker.Date.Value.ToString(datePicker.Format, initialCulture);
				await AssertHelpers.AssertEventually(
					() => nativeDatePicker.Text == initialText,
					message: $"DatePicker did not show the arranged initial text. Observed: '{nativeDatePicker.Text}', expected: '{initialText}'.");
				Assert.Equal(initialText, nativeDatePicker.Text);

				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertHelpers.AssertEventually(
					() => clickToken != -1,
					message: "The attached culture-change button callback did not run.");
				Assert.Equal(30090, clickToken);
				Assert.Equal(targetCulture.Name, CultureInfo.CurrentCulture.Name);
				Assert.Equal(targetCulture.Name, CultureInfo.CurrentUICulture.Name);
				Assert.Equal(targetCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
				Assert.Equal(targetCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);
				Assert.Same(datePickerHandler, datePicker.Handler);
				Assert.Same(nativeDatePicker, datePickerHandler.PlatformView);

				var targetText = datePicker.Date.Value.ToString(datePicker.Format, targetCulture);
				var refreshed = await AssertHelpers.Wait(() => nativeDatePicker.Text == targetText);

				Assert.True(
					refreshed,
					$"DatePicker native text did not refresh after runtime culture change. Observed: '{nativeDatePicker.Text}', expected: '{targetText}'.");
			});
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
			CultureInfo.CurrentUICulture = previousUICulture;
			CultureInfo.DefaultThreadCurrentCulture = previousDefaultCulture;
			CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUICulture;
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

