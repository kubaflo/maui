#if MACCATALYST
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

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30532")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue30532 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CharacterSpacingIsAppliedToRenderedTimeText()
		{
			var originalCulture = CultureInfo.CurrentCulture;
			var originalUICulture = CultureInfo.CurrentUICulture;
			var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
			var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
			var enUsCulture = CultureInfo.GetCultureInfo("en-US");

			try
			{
				CultureInfo.CurrentCulture = enUsCulture;
				CultureInfo.CurrentUICulture = enUsCulture;
				CultureInfo.DefaultThreadCurrentCulture = enUsCulture;
				CultureInfo.DefaultThreadCurrentUICulture = enUsCulture;

				Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
				Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentCulture.Name);

				EnsureHandlerCreated(builder =>
				{
					builder.ConfigureMauiHandlers(handlers =>
					{
						handlers.AddHandler<Page, PageHandler>();
						handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
						handlers.AddHandler<ScrollView, ScrollViewHandler>();
						handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
						handlers.AddHandler<Label, LabelHandler>();
						handlers.AddHandler<Button, ButtonHandler>();
						handlers.AddHandler<TimePicker, TimePickerHandler>();
					});
				});

				const double tolerance = 0.01;
				var expectedTime = new TimeSpan(11, 0, 0);
				var defaultTimePicker = new TimePicker
				{
					Format = "hh:mm tt",
					HorizontalOptions = LayoutOptions.Start,
					Time = expectedTime
				};
				var spacedTimePicker = new TimePicker
				{
					CharacterSpacing = 10,
					Format = "hh:mm tt",
					HorizontalOptions = LayoutOptions.Start,
					Time = expectedTime
				};
				var layout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 12,
					Children =
					{
						new Label { Text = "Default character spacing" },
						defaultTimePicker,
						new Label { Text = "CharacterSpacing = 10" },
						spacedTimePicker,
						new Button { Text = "Check character spacing" }
					}
				};
				var page = new ContentPage
				{
					Content = new ScrollView { Content = layout }
				};

				var callbackCompleted = false;
				var defaultNativeWidth = double.NaN;
				var spacedNativeWidth = double.NaN;

				await CreateHandlerAndAddToWindow(page, () =>
				{
					var defaultHandler = Assert.IsType<TimePickerHandler>(defaultTimePicker.Handler);
					var spacedHandler = Assert.IsType<TimePickerHandler>(spacedTimePicker.Handler);
					var defaultNativePicker = Assert.IsType<UIDatePicker>(defaultHandler.PlatformView);
					var spacedNativePicker = Assert.IsType<UIDatePicker>(spacedHandler.PlatformView);

					Assert.NotNull(defaultNativePicker.Window);
					Assert.NotNull(spacedNativePicker.Window);
					Assert.NotNull(defaultNativePicker.TimeZone);
					Assert.NotNull(spacedNativePicker.TimeZone);
					Assert.Equal(defaultNativePicker.TimeZone.Name, spacedNativePicker.TimeZone.Name);

					defaultNativeWidth = defaultNativePicker.Frame.Width;
					spacedNativeWidth = spacedNativePicker.Frame.Width;
					callbackCompleted = true;
				});

				Assert.True(callbackCompleted, "The native TimePicker inspection callback did not complete after attachment.");
				Assert.True(defaultNativeWidth > 0,
					$"The default TimePicker should have a positive rendered native width. Actual: {defaultNativeWidth}.");
				Assert.True(spacedNativeWidth > 0,
					$"The spaced TimePicker should have a positive rendered native width. Actual: {spacedNativeWidth}.");

				Assert.True(spacedNativeWidth > defaultNativeWidth + tolerance,
					$"TimePicker CharacterSpacing was not applied to the rendered native time text. Default width: {defaultNativeWidth}; spaced width: {spacedNativeWidth}; CharacterSpacing: {spacedTimePicker.CharacterSpacing}.");
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;
				CultureInfo.CurrentUICulture = originalUICulture;
				CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
				CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
			}
		}
	}
}
#endif

