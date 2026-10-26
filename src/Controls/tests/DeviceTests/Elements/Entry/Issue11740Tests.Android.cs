using System;
using System.Globalization;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Entry)]
	[Category("Issue11740")]
	public class Issue11740 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task BindingDoNothingDoesNotUpdateNativeText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
				});
			});

			var conversionCount = -1;
			object observedInput = new object();

			var scenario = await InvokeOnMainThreadAsync(() =>
			{
				var converter = new DoNothingConverter
				{
					OnConvert = value =>
					{
						conversionCount++;
						observedInput = value;
					}
				};
				var entry = new Entry();
				var initialText = entry.Text ?? string.Empty;
				var source = new Issue11740BindingSource();
				var page = new ContentPage
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children =
						{
							new Label { Text = "Binding.DoNothing reproduction" },
							entry,
							new Label { Text = "Binding result" },
						}
					}
				};

				page.Resources.Add("DoNothingConverter", converter);
				entry.SetBinding(
					Entry.TextProperty,
					new Binding(
						nameof(Issue11740BindingSource.Value),
						converter: (IValueConverter)page.Resources["DoNothingConverter"]));
				page.BindingContext = source;

				return (page, entry, initialText, source, converter);
			});

			Assert.Same(scenario.converter, scenario.page.Resources["DoNothingConverter"]);
			Assert.Equal(string.Empty, scenario.initialText);
			Assert.Equal("Source value", scenario.source.Value);
			Assert.True(conversionCount >= 0, "The converter was not invoked after assigning BindingContext.");
			Assert.Equal(scenario.source.Value, observedInput);

			await CreateHandlerAndAddToWindow(scenario.page, async () =>
			{
				Assert.NotNull(scenario.entry.Handler);
				var entryHandler = Assert.IsType<EntryHandler>(scenario.entry.Handler);
				Assert.NotNull(entryHandler.PlatformView);
				var platformEntry = Assert.IsAssignableFrom<AppCompatEditText>(entryHandler.PlatformView);

				await AssertHelpers.AssertEventually(
					async () => await InvokeOnMainThreadAsync(() =>
					{
						var nativeText = platformEntry.Text ?? string.Empty;
						var managedText = scenario.entry.Text ?? string.Empty;
						return platformEntry.IsAttachedToWindow && nativeText == managedText;
					}),
					message: "The Entry handler did not attach or its native text did not settle.");

				var nativeText = await InvokeOnMainThreadAsync(() => platformEntry.Text ?? string.Empty);
				Assert.True(
					scenario.initialText == nativeText,
					$"Binding.DoNothing should leave the native Entry text empty. Expected: '{scenario.initialText}', Actual: '{nativeText}'.");
			});
		}

		sealed class Issue11740BindingSource
		{
			public string Value { get; } = "Source value";
		}

		sealed class DoNothingConverter : IValueConverter
		{
			public Action<object> OnConvert { get; init; } =
				_ => throw new InvalidOperationException("The conversion callback was not configured.");

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				OnConvert(value);
				return Binding.DoNothing;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return Binding.DoNothing;
			}
		}
	}
}

