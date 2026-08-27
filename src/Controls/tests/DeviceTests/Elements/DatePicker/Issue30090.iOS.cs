#if IOS && !MACCATALYST
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category("Issue30090")]
public class Issue30090 : ControlsHandlerTestBase
{
	[Fact]
	public async Task NativeTextUpdatesAfterRuntimeCultureChange()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
		var originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		try
		{
			var initialCulture = new CultureInfo("en-US");
			var targetCulture = new CultureInfo("de-DE");
			CultureInfo.CurrentCulture = initialCulture;
			CultureInfo.CurrentUICulture = initialCulture;
			CultureInfo.DefaultThreadCurrentCulture = initialCulture;
			CultureInfo.DefaultThreadCurrentUICulture = initialCulture;

			Assert.Equal(initialCulture.Name, CultureInfo.CurrentCulture.Name);
			Assert.Equal(initialCulture.Name, CultureInfo.CurrentUICulture.Name);
			Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
			Assert.Equal(initialCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<DatePicker, DatePickerHandler>();
				});
			});

			var reproductionDate = new DateTime(2025, 6, 24);
			var expectedInitialText = reproductionDate.ToString("d", initialCulture);
			var expectedTargetText = reproductionDate.ToString("d", targetCulture);
			Assert.NotEqual(expectedInitialText, expectedTargetText);

			var datePicker = new DatePicker
			{
				Date = reproductionDate
			};
			var cultureSummaryLabel = new Label
			{
				Text = $"Current culture: {initialCulture.Name}"
			};
			var expectedRenderedLabel = new Label
			{
				Text = $"Expected after culture change: {expectedTargetText}"
			};
			var initialRenderedLabel = new Label
			{
				Text = "Initial rendered text: preparing"
			};
			var observedRenderedLabel = new Label
			{
				Text = "Observed after culture change: not triggered"
			};
			var changeCultureButton = new Button
			{
				IsEnabled = false,
				Text = "Change culture to de-DE"
			};
			var readyStatusLabel = new Label
			{
				Text = "Waiting for DatePicker to render"
			};
			var completionStatusLabel = new Label
			{
				Text = "Waiting for culture change"
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 14,
					Children =
					{
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							FontSize = 20,
							Text = "DatePicker runtime culture change"
						},
						new Label
						{
							Text = "The DatePicker below uses its default Format value (d). Change the managed culture from en-US to de-DE."
						},
						datePicker,
						cultureSummaryLabel,
						expectedRenderedLabel,
						initialRenderedLabel,
						observedRenderedLabel,
						changeCultureButton,
						readyStatusLabel,
						completionStatusLabel
					}
				}
			};

			var clickCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var clickOccurred = false;
			var postTriggerCultureName = "culture-not-changed";
			var observedText = "native-text-not-read";
			object postTriggerPlatformView = new object();

			changeCultureButton.Clicked += (_, _) =>
			{
				clickOccurred = true;
				CultureInfo.CurrentCulture = targetCulture;
				CultureInfo.CurrentUICulture = targetCulture;
				CultureInfo.DefaultThreadCurrentCulture = targetCulture;
				CultureInfo.DefaultThreadCurrentUICulture = targetCulture;
				cultureSummaryLabel.Text = $"Current culture: {CultureInfo.CurrentCulture.Name}";

				page.Dispatcher.Dispatch(() =>
				{
					var currentHandler = datePicker.Handler as DatePickerHandler;
					if (currentHandler != null)
					{
						postTriggerPlatformView = currentHandler.PlatformView;
						observedText = currentHandler.PlatformView.Text ?? string.Empty;
					}

					postTriggerCultureName = CultureInfo.CurrentCulture.Name;
					observedRenderedLabel.Text = $"Observed after culture change: {observedText}";
					completionStatusLabel.Text = "Culture change checked";
					clickCompleted.TrySetResult(true);
				});
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var datePickerHandler = datePicker.Handler as DatePickerHandler;
				Assert.NotNull(datePickerHandler);
				var nativeDatePicker = datePickerHandler.PlatformView as MauiDatePicker;
				Assert.NotNull(nativeDatePicker);

				var initialText = nativeDatePicker.Text ?? string.Empty;
				Assert.Equal(expectedInitialText, initialText);
				initialRenderedLabel.Text = $"Initial rendered text: {initialText}";
				readyStatusLabel.Text = "Ready: DatePicker rendered";

				var buttonHandler = changeCultureButton.Handler as ButtonHandler;
				Assert.NotNull(buttonHandler);
				var nativeButton = buttonHandler.PlatformView as UIButton;
				Assert.NotNull(nativeButton);

				changeCultureButton.IsEnabled = true;
				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await clickCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.True(clickOccurred);
				Assert.Equal(targetCulture.Name, postTriggerCultureName);
				Assert.Equal(targetCulture.Name, CultureInfo.CurrentCulture.Name);
				Assert.Equal(targetCulture.Name, CultureInfo.CurrentUICulture.Name);
				Assert.Equal(targetCulture.Name, CultureInfo.DefaultThreadCurrentCulture.Name);
				Assert.Equal(targetCulture.Name, CultureInfo.DefaultThreadCurrentUICulture.Name);
				Assert.Same(nativeDatePicker, postTriggerPlatformView);

				Assert.True(
					observedText == expectedTargetText,
					$"DatePicker native text did not update after runtime culture change. Initial: '{initialText}', observed: '{observedText}', expected: '{expectedTargetText}'.");
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

