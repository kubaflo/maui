using System;
using System.Globalization;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Entry)]
	[Category("Issue11740")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue11740 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task BindingDoNothingLeavesNativeEntryTextEmpty()
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

			var converter = new DoNothingConverter();
			var source = new BindingSource();
			var entry = new Entry();
			var expectationLabel = new Label
			{
				Text = "The converter suppresses the Entry text update."
			};
			var pageLoaded = false;
			var page = new ContentPage
			{
				Resources = new ResourceDictionary
				{
					{ "DoNothingConverter", converter }
				}
			};
			var resourceConverter = Assert.IsType<DoNothingConverter>(page.Resources["DoNothingConverter"]);
			var binding = new Binding(nameof(BindingSource.SourceValue), converter: resourceConverter);

			page.Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "Issue 11740: Binding.DoNothing"
					},
					new Label
					{
						Text = "The Entry below should remain empty when the converter returns Binding.DoNothing."
					},
					entry,
					expectationLabel
				}
			};

			entry.SetBinding(Entry.TextProperty, binding);
			page.Loaded += (_, _) => pageLoaded = true;
			page.BindingContext = source;

			Assert.Same(converter, resourceConverter);
			Assert.Same(converter, binding.Converter);
			Assert.Same(source, page.BindingContext);
			Assert.Equal(nameof(BindingSource.SourceValue), binding.Path);

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.True(converter.WasCalled, "The converter was not called after setting the page BindingContext.");
				Assert.True(pageLoaded, "The page did not reach its Loaded transition.");

				var entryHandler = Assert.IsType<EntryHandler>(entry.Handler);
				var platformEntry = Assert.IsAssignableFrom<AppCompatEditText>(entryHandler.PlatformView);
				string nativeText = null;

				await AssertEventually(
					() =>
					{
						nativeText = platformEntry.Text;
						return nativeText == entry.Text;
					},
					message: "The Android Entry text did not synchronize with the MAUI Entry text.");

				Assert.True(
					string.IsNullOrEmpty(nativeText),
					$"Binding.DoNothing should leave the Android Entry empty; observed native text '{nativeText}'.");
			});
		}

		sealed class BindingSource
		{
			public string SourceValue => "This value must not reach the Entry";
		}

		sealed class DoNothingConverter : IValueConverter
		{
			public bool WasCalled { get; private set; }

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				WasCalled = true;
				return Binding.DoNothing;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return Binding.DoNothing;
			}
		}
	}
}

