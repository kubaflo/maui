using System;
using System.Globalization;
using System.Linq;
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

#if WINDOWS
public class Issue30090 : ControlsHandlerTestBase
{
	[Fact]
	[Category("Issue30090")]
	public async Task DefaultFormatRefreshesAfterRuntimeCultureChange()
	{
		var savedCurrentCulture = CultureInfo.CurrentCulture;
		var savedCurrentUICulture = CultureInfo.CurrentUICulture;
		var savedDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var savedDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
		var initialCulture = new CultureInfo("en-US");
		var changedCulture = new CultureInfo("fr-FR");
		var selectedDate = new DateTime(2026, 12, 24);

		try
		{
			SetCulture(initialCulture);
			AssertCulture(initialCulture);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<DatePicker, DatePickerHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var datePicker = new DatePicker
			{
				Date = selectedDate
			};
			var cultureChangeTriggered = false;
			var changeCultureButton = new Button
			{
				Text = "Change culture"
			};
			changeCultureButton.Clicked += (_, _) =>
			{
				SetCulture(changedCulture);
				cultureChangeTriggered = true;
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children = { datePicker, changeCultureButton }
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				AssertCulture(initialCulture);

				var datePickerHandler = datePicker.Handler as DatePickerHandler;
				Assert.NotNull(datePickerHandler);
				WCalendarDatePicker platformDatePicker = datePickerHandler.PlatformView;
				Assert.NotNull(platformDatePicker);
				platformDatePicker.UpdateLayout();

				var dateText = FindDateText(platformDatePicker);
				Assert.NotNull(dateText);

				var initialText = NormalizeDateText(dateText.Text);
				var expectedInitialText = selectedDate.ToString("d", CultureInfo.CurrentCulture);
				Assert.Equal(expectedInitialText, initialText);

				var postTriggerDispatcherRan = false;
				var postTriggerDispatcher = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

				var buttonHandler = changeCultureButton.Handler as ButtonHandler;
				Assert.NotNull(buttonHandler);
				var buttonPeer = new ButtonAutomationPeer(buttonHandler.PlatformView);
				var invokeProvider = buttonPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
				Assert.NotNull(invokeProvider);
				invokeProvider.Invoke();

				Assert.True(cultureChangeTriggered, "The culture-change button click did not run.");
				AssertCulture(changedCulture);

				Assert.True(platformDatePicker.DispatcherQueue.TryEnqueue(() =>
				{
					postTriggerDispatcherRan = true;
					postTriggerDispatcher.TrySetResult(true);
				}));

				await postTriggerDispatcher.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.True(postTriggerDispatcherRan, "The post-culture-change UI callback did not run.");

				var expectedChangedText = selectedDate.ToString("d", CultureInfo.CurrentCulture);
				platformDatePicker.UpdateLayout();
				var currentDateText = FindDateText(platformDatePicker);
				Assert.NotNull(currentDateText);
				var observedChangedText = NormalizeDateText(currentDateText.Text);

				AssertCulture(changedCulture);
				Assert.True(
					observedChangedText == expectedChangedText,
					$"DatePicker default format did not refresh after runtime culture change. Observed '{observedChangedText}', expected '{expectedChangedText}'.");
			});
		}
		finally
		{
			CultureInfo.CurrentCulture = savedCurrentCulture;
			CultureInfo.CurrentUICulture = savedCurrentUICulture;
			CultureInfo.DefaultThreadCurrentCulture = savedDefaultCulture;
			CultureInfo.DefaultThreadCurrentUICulture = savedDefaultUICulture;
		}
	}

	static void SetCulture(CultureInfo culture)
	{
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	static void AssertCulture(CultureInfo expected)
	{
		Assert.Equal(expected.Name, CultureInfo.CurrentCulture.Name);
		Assert.Equal(expected.Name, CultureInfo.CurrentUICulture.Name);
		Assert.Equal(expected.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
		Assert.Equal(expected.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);
	}

	static string NormalizeDateText(string value) =>
		new(value.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.Format).ToArray());

	static WTextBlock FindDateText(WDependencyObject parent)
	{
		var childCount = WVisualTreeHelper.GetChildrenCount(parent);
		for (var index = 0; index < childCount; index++)
		{
			var child = WVisualTreeHelper.GetChild(parent, index);
			if (child is WTextBlock textBlock && textBlock.Name == "DateText")
				return textBlock;

			var descendant = FindDateText(child);
			if (descendant is not null)
				return descendant;
		}

		return null;
	}
}
#endif

