#if WINDOWS
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WTimePicker = Microsoft.UI.Xaml.Controls.TimePicker;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30197")]
public class Issue30197 : ControlsHandlerTestBase
{
	[Fact]
	public async Task LoadedTimePickerUpdatesClockFormatWhenCultureChanges()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
		var initialCulture = CultureInfo.GetCultureInfo("en-US");
		var changedCulture = CultureInfo.GetCultureInfo("fr-FR");
		var selectedTime = new TimeSpan(13, 45, 0);

		void ApplyCulture(CultureInfo culture)
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
		}

		string FormatTime(CultureInfo culture) =>
			DateTime.Today.Add(selectedTime).ToString("t", culture);

		try
		{
			ApplyCulture(initialCulture);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<TimePicker, TimePickerHandler>();
				});
			});

			var affectedTimePicker = new TimePicker
			{
				Time = selectedTime,
				HorizontalOptions = LayoutOptions.Start,
			};
			var cultureLabel = new Label
			{
				Text = $"Current culture: {initialCulture.Name}",
				FontSize = 18,
			};
			var expectedDisplayLabel = new Label
			{
				Text = $"Expected display after culture change: {FormatTime(initialCulture)}",
				FontSize = 18,
			};
			var changeCultureButton = new Button
			{
				Text = "Change culture to fr-FR",
			};
			var checkDisplayButton = new Button
			{
				Text = "Check displayed format",
				IsEnabled = false,
			};
			var observationLabel = new Label
			{
				Text = "Observe the TimePicker display after changing the culture.",
				FontSize = 20,
				FontAttributes = FontAttributes.Bold,
			};
			var cultureChanged = false;
			var cultureChangeCompleted = new TaskCompletionSource<bool>();
			WButton nativeChangeCultureButton = null;

			changeCultureButton.Clicked += (_, _) =>
			{
				ApplyCulture(changedCulture);
				cultureChanged = true;
				cultureLabel.Text = $"Current culture: {changedCulture.Name}";
				expectedDisplayLabel.Text = $"Expected display after culture change: {FormatTime(changedCulture)}";
				changeCultureButton.IsEnabled = false;
				checkDisplayButton.IsEnabled = true;

				if (!nativeChangeCultureButton.DispatcherQueue.TryEnqueue(
					() => cultureChangeCompleted.TrySetResult(true)))
				{
					cultureChangeCompleted.TrySetException(
						new InvalidOperationException("Unable to enqueue the post-culture-change UI transition."));
				}
			};

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "TimePicker runtime culture change",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
					},
					new Label
					{
						Text = "The TimePicker below uses its default Format value.",
						FontSize = 16,
					},
					cultureLabel,
					new Label
					{
						Text = $"Display before culture change: {FormatTime(initialCulture)}",
						FontSize = 18,
					},
					affectedTimePicker,
					expectedDisplayLabel,
					changeCultureButton,
					checkDisplayButton,
					observationLabel,
				},
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = content,
				},
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.Equal(initialCulture.Name, CultureInfo.CurrentCulture.Name);
				Assert.Equal(initialCulture.Name, CultureInfo.CurrentUICulture.Name);
				Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
				Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);

				var timePickerHandler = Assert.IsType<TimePickerHandler>(affectedTimePicker.Handler);
				WTimePicker nativeTimePicker = timePickerHandler.PlatformView;
				Assert.NotNull(nativeTimePicker);
				Assert.True(nativeTimePicker.IsLoaded);
				Assert.Equal("t", affectedTimePicker.Format);
				Assert.Equal(selectedTime, affectedTimePicker.Time);
				Assert.Equal(selectedTime, nativeTimePicker.SelectedTime);

				string ReadDisplayedTime()
				{
					var hourText = nativeTimePicker.GetDescendantByName<WTextBlock>("HourTextBlock");
					var minuteText = nativeTimePicker.GetDescendantByName<WTextBlock>("MinuteTextBlock");
					var periodText = nativeTimePicker.GetDescendantByName<WTextBlock>("PeriodTextBlock");

					if (hourText is null || minuteText is null)
						return string.Empty;

					var period = periodText is null ||
						periodText.Visibility != WVisibility.Visible ||
						string.IsNullOrWhiteSpace(periodText.Text)
						? string.Empty
						: $" {periodText.Text}";

					return $"{hourText.Text}:{minuteText.Text}{period}";
				}

				var expectedInitialDisplay = FormatTime(initialCulture);
				var initialDisplay = string.Empty;
				var initialDisplayReady = await AssertHelpers.Wait(() =>
				{
					initialDisplay = ReadDisplayedTime();
					return string.Equals(initialDisplay, expectedInitialDisplay, StringComparison.Ordinal);
				});

				Assert.True(
					initialDisplayReady,
					$"TimePicker did not render the arranged en-US value. Observed '{initialDisplay}', expected '{expectedInitialDisplay}'.");

				var expectedChangedDisplay = FormatTime(changedCulture);
				Assert.NotEqual(expectedInitialDisplay, expectedChangedDisplay);
				Assert.True(initialCulture.DateTimeFormat.ShortTimePattern.Contains("tt", StringComparison.Ordinal));
				Assert.True(changedCulture.DateTimeFormat.ShortTimePattern.Contains("H", StringComparison.Ordinal));

				var buttonHandler = Assert.IsType<ButtonHandler>(changeCultureButton.Handler);
				nativeChangeCultureButton = buttonHandler.PlatformView;
				Assert.NotNull(nativeChangeCultureButton);
				Assert.True(nativeChangeCultureButton.IsLoaded);

				var automationPeer = new ButtonAutomationPeer(nativeChangeCultureButton);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(
					automationPeer.GetPattern(PatternInterface.Invoke));
				invokeProvider.Invoke();
				await cultureChangeCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));

				Assert.True(cultureChanged);
				Assert.False(changeCultureButton.IsEnabled);
				Assert.True(checkDisplayButton.IsEnabled);
				Assert.Equal(changedCulture.Name, CultureInfo.CurrentCulture.Name);
				Assert.Equal(changedCulture.Name, CultureInfo.CurrentUICulture.Name);
				Assert.Equal(changedCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
				Assert.Equal(changedCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);
				Assert.Same(timePickerHandler, affectedTimePicker.Handler);
				Assert.True(nativeTimePicker.IsLoaded);

				var observedDisplay = "<not read>";
				var displayUpdated = await AssertHelpers.Wait(() =>
				{
					observedDisplay = ReadDisplayedTime();
					return string.Equals(observedDisplay, expectedChangedDisplay, StringComparison.Ordinal);
				});

				Assert.True(
					displayUpdated,
					$"TimePicker retained stale displayed time after runtime culture change. Initial '{initialDisplay}', observed '{observedDisplay}', expected '{expectedChangedDisplay}'.");
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
}
#endif

