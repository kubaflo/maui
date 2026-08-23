#if IOS && !MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Media;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Picker)]
	[Category("Issue34530")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34530 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task LithuanianLocaleIsAvailableAfterPageLoads()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
				});
			});

			var readyLabel = new Label
			{
				AutomationId = "ReadyLabel",
				Text = "Loading locales"
			};
			var localePicker = new Picker
			{
				AutomationId = "LocalePicker",
				Title = "Available text-to-speech locales"
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16
			};
			layout.Add(new Label
			{
				Text = "Issue 34530: iOS text-to-speech locales",
				FontSize = 20
			});
			layout.Add(new Label
			{
				Text = "Tap the platform-default picker to inspect the locales returned by TextToSpeech.Default.GetLocalesAsync()."
			});
			layout.Add(readyLabel);
			layout.Add(localePicker);
			layout.Add(new Label
			{
				AutomationId = "InteractionLabel",
				Text = "Picker not opened"
			});
			layout.Add(new Label
			{
				Text = "Lithuanian locale availability",
				FontAttributes = FontAttributes.Bold
			});

			var page = new ContentPage
			{
				Content = layout
			};
			Locale[] locales = null;
			int callbackCount = 0;

			page.Loaded += OnPageLoaded;

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await AssertEventually(() => callbackCount == 1);

				Assert.NotNull(locales);
				Assert.IsType<PickerHandler>(localePicker.Handler);
				var pickerLanguages = Assert.IsType<string[]>(localePicker.ItemsSource);
				Assert.Equal(locales.Length, pickerLanguages.Length);
				Assert.Equal(locales.Select(locale => locale.Language), pickerLanguages);

				int matchingCount = locales.Count(locale =>
					string.Equals(locale.Language, "lt", StringComparison.OrdinalIgnoreCase) ||
					locale.Language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase));

				Assert.True(
					matchingCount > 0,
					$"Lithuanian locale missing after locale load; expected at least 1 language beginning 'lt', observed {matchingCount}. Returned language codes: {string.Join(", ", locales.Select(locale => locale.Language))}");
			});

			async void OnPageLoaded(object sender, EventArgs e)
			{
				page.Loaded -= OnPageLoaded;
				locales = (await TextToSpeech.Default.GetLocalesAsync())
					.OrderBy(locale => locale.Language, StringComparer.OrdinalIgnoreCase)
					.ToArray();
				localePicker.ItemsSource = locales
					.Select(locale => locale.Language)
					.ToArray();
				readyLabel.Text = "Locales loaded";
				callbackCount++;
			}
		}
	}
}
#endif

