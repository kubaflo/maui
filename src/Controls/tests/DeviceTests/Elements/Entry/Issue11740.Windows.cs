#if WINDOWS
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WTextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue11740")]
	public class Issue11740 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task BindingDoNothingDoesNotUpdateNativeEntryText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var source = new BindingSource();
			var converter = new DoNothingConverter();
			var binding = new Binding(nameof(BindingSource.Value), converter: converter);
			var affectedEntry = new Entry
			{
				AutomationId = "AffectedEntry",
				Placeholder = "Expected to remain empty"
			};
			affectedEntry.SetBinding(Entry.TextProperty, binding);

			var applyBindingButton = new Button
			{
				AutomationId = "ApplyBindingButton",
				Text = "Apply binding"
			};
			var page = new ContentPage
			{
				Title = "Binding.DoNothing",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label { Text = "The Entry should remain empty when the converter returns Binding.DoNothing." },
						affectedEntry,
						applyBindingButton
					}
				}
			};
			applyBindingButton.Clicked += (_, _) => page.BindingContext = source;

			Assert.Null(page.BindingContext);
			Assert.Same(converter, binding.Converter);

			var window = new Window(page);
			await CreateHandlerAndAddToWindow<IWindowHandler>(window, windowHandler =>
			{
				Assert.Same(window.Handler, windowHandler);
				var entryHandler = Assert.IsType<EntryHandler>(affectedEntry.Handler);
				var platformEntry = Assert.IsAssignableFrom<WTextBox>(entryHandler.PlatformView);
				Assert.Equal(string.Empty, platformEntry.Text);

				applyBindingButton.SendClicked();

				Assert.Same(source, page.BindingContext);
				Assert.NotSame(BindableProperty.UnsetValue, converter.ObservedValue);
				Assert.NotSame(BindableProperty.UnsetValue, converter.ReturnedValue);
				Assert.Equal(source.Value, converter.ObservedValue);
				Assert.Same(Binding.DoNothing, converter.ReturnedValue);

				var nativeText = platformEntry.Text;
				Assert.True(
					string.IsNullOrEmpty(nativeText),
					$"Binding.DoNothing updated the native Entry text. Expected: <empty>; Actual: '{nativeText}'.");
			});
		}

		sealed class DoNothingConverter : IValueConverter
		{
			public object ObservedValue { get; private set; } = BindableProperty.UnsetValue;

			public object ReturnedValue { get; private set; } = BindableProperty.UnsetValue;

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				ObservedValue = value;
				ReturnedValue = Binding.DoNothing;

				return ReturnedValue;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
				Binding.DoNothing;
		}

		sealed class BindingSource
		{
			public string Value { get; } = "Bound source value";
		}
	}
}
#endif

