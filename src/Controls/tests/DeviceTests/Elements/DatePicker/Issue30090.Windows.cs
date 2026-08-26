#if WINDOWS
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using WCalendarDatePicker = Microsoft.UI.Xaml.Controls.CalendarDatePicker;
using WDependencyObject = Microsoft.UI.Xaml.DependencyObject;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using WVisualTreeHelper = Microsoft.UI.Xaml.Media.VisualTreeHelper;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue30090")]
public class Issue30090 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RuntimeCultureChangeUpdatesDisplayedDate()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var initialCulture = CultureInfo.GetCultureInfo("en-US");
			var updatedCulture = CultureInfo.GetCultureInfo("fr-FR");
			var arrangedDate = new DateTime(2026, 12, 24);
			ApplyCulture(initialCulture);

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

			var cultureLabel = new Label { Text = "Current culture: en-US" };
			var initialDisplayLabel = new Label { Text = "Initial DatePicker digits: pending" };
			var datePicker = new DatePicker { Date = arrangedDate };
			var expectedDisplayLabel = new Label { Text = "Expected fr-FR digits: 24122026" };
			var changeCultureButton = new Button { Text = "Change culture to fr-FR" };
			var checkDisplayButton = new Button
			{
				Text = "Check displayed format",
				IsEnabled = false
			};
			changeCultureButton.Clicked += (_, _) =>
			{
				ApplyCulture(updatedCulture);
				cultureLabel.Text = "Current culture: fr-FR";
				checkDisplayButton.IsEnabled = true;
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
						Text = "DatePicker runtime culture change"
					},
					cultureLabel,
					initialDisplayLabel,
					datePicker,
					expectedDisplayLabel,
					changeCultureButton,
					checkDisplayButton
				}
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = layout }
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(page), async _ =>
			{
				Assert.Equal(initialCulture, CultureInfo.CurrentCulture);
				Assert.Equal(initialCulture, CultureInfo.CurrentUICulture);
				Assert.Equal(initialCulture, CultureInfo.DefaultThreadCurrentCulture);
				Assert.Equal(initialCulture, CultureInfo.DefaultThreadCurrentUICulture);

				var datePickerHandler = Assert.IsType<DatePickerHandler>(datePicker.Handler);
				var platformDatePicker = Assert.IsType<WCalendarDatePicker>(datePickerHandler.PlatformView);
				WTextBlock dateText = null;

				await AssertEventually(
					() =>
					{
						dateText = FindDateText(platformDatePicker);
						return dateText is { IsLoaded: true };
					},
					message: "The native DatePicker text was not loaded.");

				Assert.NotNull(dateText);
				var initialDigits = DigitsOnly(dateText.Text);
				var expectedInitialDigits = DigitsOnly(arrangedDate.ToString("d", initialCulture));
				Assert.Equal(expectedInitialDigits, initialDigits);
				initialDisplayLabel.Text = $"Initial DatePicker digits: {initialDigits}";

				var buttonHandler = Assert.IsType<ButtonHandler>(changeCultureButton.Handler);
				var automationPeer = new ButtonAutomationPeer(buttonHandler.PlatformView);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(
					automationPeer.GetPattern(PatternInterface.Invoke));
				invokeProvider.Invoke();

				Assert.Equal(updatedCulture, CultureInfo.CurrentCulture);
				Assert.Equal(updatedCulture, CultureInfo.CurrentUICulture);
				Assert.Equal(updatedCulture, CultureInfo.DefaultThreadCurrentCulture);
				Assert.Equal(updatedCulture, CultureInfo.DefaultThreadCurrentUICulture);
				Assert.Equal("Current culture: fr-FR", cultureLabel.Text);
				Assert.True(checkDisplayButton.IsEnabled);

				var postTriggerCallbackOccurred = false;
				Assert.True(
					platformDatePicker.DispatcherQueue.TryEnqueue(() => postTriggerCallbackOccurred = true),
					"Unable to enqueue the post-culture-change callback.");
				await AssertEventually(
					() => postTriggerCallbackOccurred,
					message: "The post-culture-change callback did not run.");

				var expectedUpdatedDigits = DigitsOnly(arrangedDate.ToString("d", updatedCulture));
				await AssertEventually(
					() =>
					{
						dateText = FindDateText(platformDatePicker);
						return dateText is { IsLoaded: true } &&
							DigitsOnly(dateText.Text) == expectedUpdatedDigits;
					},
					message: "DatePicker displayed stale date after runtime culture change.");
			});
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = originalCulture;
			Thread.CurrentThread.CurrentUICulture = originalUICulture;
			CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
			CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
		}
	}

	static void ApplyCulture(CultureInfo culture)
	{
		Thread.CurrentThread.CurrentCulture = culture;
		Thread.CurrentThread.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	static WTextBlock FindDateText(WDependencyObject parent)
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < childCount; i++)
		{
			var child = WVisualTreeHelper.GetChild(parent, i);
			if (child is WTextBlock { Name: "DateText" } dateText)
				return dateText;

			var descendant = FindDateText(child);
			if (descendant is not null)
				return descendant;
		}

		return null;
	}

	static string DigitsOnly(string text) =>
		string.Concat(text.Where(char.IsDigit));
}
#endif

