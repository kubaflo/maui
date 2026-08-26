#if WINDOWS
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WTimePicker = Microsoft.UI.Xaml.Controls.TimePicker;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30197")]
public class Issue30197 : ControlsHandlerTestBase
{
	[Fact]
	public async Task DefaultFormatUpdatesAfterRuntimeCultureChange()
	{
		const string InitialClockIdentifier = "12HourClock";
		const string UpdatedClockIdentifier = "24HourClock";
		var initialCulture = CultureInfo.GetCultureInfo("en-US");
		var updatedCulture = CultureInfo.GetCultureInfo("ja-JP");
		var previousCulture = Thread.CurrentThread.CurrentCulture;
		var previousUICulture = Thread.CurrentThread.CurrentUICulture;
		var previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var previousDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			SetCulture(initialCulture);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<TimePicker, TimePickerHandler>();
				});
			});

			var cultureLabel = new Label { Text = $"Current culture: {initialCulture.Name}" };
			var expectedLabel = new Label { Text = "Expected display after change: 17:30" };
			var timePicker = new TimePicker { Time = new TimeSpan(17, 30, 0) };
			var clickObserved = -1;
			var changeCultureButton = new Button { Text = "Change culture to ja-JP" };

			changeCultureButton.Clicked += (_, _) =>
			{
				SetCulture(updatedCulture);
				clickObserved = 0;
				cultureLabel.Text = $"Current culture: {updatedCulture.Name}";
			};

			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "TimePicker runtime culture change",
							FontSize = 24,
							FontAttributes = FontAttributes.Bold
						},
						new Label { Text = "The TimePicker below uses its default Format value." },
						timePicker,
						cultureLabel,
						expectedLabel,
						changeCultureButton
					}
				}
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.Equal(initialCulture, CultureInfo.CurrentCulture);
				Assert.Equal(initialCulture, CultureInfo.CurrentUICulture);
				Assert.Equal(initialCulture, CultureInfo.DefaultThreadCurrentCulture);
				Assert.Equal(initialCulture, CultureInfo.DefaultThreadCurrentUICulture);
				Assert.Equal("t", timePicker.Format);
				Assert.Equal(new TimeSpan(17, 30, 0), timePicker.Time);

				var timePickerHandler = Assert.IsType<TimePickerHandler>(timePicker.Handler);
				WTimePicker nativeTimePicker = timePickerHandler.PlatformView;
				Assert.NotNull(nativeTimePicker);
				Assert.Equal(new TimeSpan(17, 30, 0), nativeTimePicker.SelectedTime);
				Assert.Equal(InitialClockIdentifier, nativeTimePicker.ClockIdentifier);

				var originalManagedTimePicker = timePicker;
				var originalNativeTimePicker = nativeTimePicker;
				var buttonHandler = Assert.IsType<ButtonHandler>(changeCultureButton.Handler);
				WButton nativeButton = buttonHandler.PlatformView;
				Assert.NotNull(nativeButton);
				var automationPeer = new ButtonAutomationPeer(nativeButton);
				var invokeProvider = automationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
				Assert.NotNull(invokeProvider);

				invokeProvider.Invoke();

				await AssertEventually(
					() => clickObserved == 0,
					message: "The mounted culture-change button did not invoke its Clicked callback.");
				Assert.Equal(0, clickObserved);
				Assert.Equal($"Current culture: {updatedCulture.Name}", cultureLabel.Text);
				Assert.Equal(updatedCulture, CultureInfo.CurrentCulture);
				Assert.Equal(updatedCulture, CultureInfo.CurrentUICulture);
				Assert.Equal(updatedCulture, CultureInfo.DefaultThreadCurrentCulture);
				Assert.Equal(updatedCulture, CultureInfo.DefaultThreadCurrentUICulture);
				Assert.Same(originalManagedTimePicker, timePickerHandler.VirtualView);
				Assert.Same(originalNativeTimePicker, timePickerHandler.PlatformView);
				Assert.Equal(new TimeSpan(17, 30, 0), timePicker.Time);

				await AssertEventually(
					() => nativeTimePicker.ClockIdentifier == UpdatedClockIdentifier,
					message: $"TimePicker retained stale clock format after runtime culture change: measured '{nativeTimePicker.ClockIdentifier}', expected '{UpdatedClockIdentifier}'.");
				Assert.True(
					nativeTimePicker.ClockIdentifier == UpdatedClockIdentifier,
					$"TimePicker retained stale clock format after runtime culture change: measured '{nativeTimePicker.ClockIdentifier}', expected '{UpdatedClockIdentifier}'.");
			});
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = previousCulture;
			Thread.CurrentThread.CurrentUICulture = previousUICulture;
			CultureInfo.DefaultThreadCurrentCulture = previousDefaultCulture;
			CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUICulture;
		}

		static void SetCulture(CultureInfo culture)
		{
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
		}
	}
}
#endif

