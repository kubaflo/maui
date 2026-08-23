#if ANDROID
using System;
using System.Globalization;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Entry)]
	[Category("Issue11740")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue11740 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task BindingDoNothingLeavesNativeEntryEmpty()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var titleLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
				Text = "Binding.DoNothing converter reproduction"
			};
			var explanationLabel = new Label
			{
				Text = "The affected Entry below should remain empty after the binding is applied."
			};
			var affectedEntry = new Entry();
			var supportingLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				Text = "Entry value"
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					titleLabel,
					explanationLabel,
					affectedEntry,
					supportingLabel
				}
			};
			var page = new ContentPage { Content = layout };

			Assert.Same(layout, page.Content);
			Assert.Equal(new Thickness(24), layout.Padding);
			Assert.Equal(16, layout.Spacing);
			Assert.Collection(
				layout.Children,
				child => Assert.Same(titleLabel, child),
				child => Assert.Same(explanationLabel, child),
				child => Assert.Same(affectedEntry, child),
				child => Assert.Same(supportingLabel, child));
			Assert.Equal(FontAttributes.Bold, titleLabel.FontAttributes);
			Assert.Equal(20, titleLabel.FontSize);
			Assert.True(string.IsNullOrEmpty(affectedEntry.Text));
			Assert.Null(affectedEntry.Style);
			Assert.Equal(-1, affectedEntry.WidthRequest);
			Assert.Equal(-1, affectedEntry.HeightRequest);
			Assert.Equal(FontAttributes.Bold, supportingLabel.FontAttributes);

			var source = new object();
			var converter = new DoNothingConverter();
			var binding = new Binding(
				".",
				mode: BindingMode.OneWay,
				converter: converter,
				source: source);

			Assert.Equal(".", binding.Path);
			Assert.Equal(BindingMode.OneWay, binding.Mode);
			Assert.Same(converter, binding.Converter);
			Assert.Same(source, binding.Source);
			Assert.Equal(0, converter.ConvertCallCount);

			affectedEntry.SetBinding(Entry.TextProperty, binding);

			Assert.Equal(1, converter.ConvertCallCount);
			Assert.Same(source, converter.ConvertedValue);

			string managedText = "<not-sampled>";
			string nativeText = "<not-sampled>";
			await CreateHandlerAndAddToWindow<IWindowHandler>(page, _ =>
			{
				var entryHandler = Assert.IsType<EntryHandler>(affectedEntry.Handler);
				Assert.Same(affectedEntry, entryHandler.VirtualView);
				var nativeEntry = Assert.IsAssignableFrom<AppCompatEditText>(entryHandler.PlatformView);

				managedText = affectedEntry.Text;
				nativeText = nativeEntry.Text;
			});

			Assert.NotEqual("<not-sampled>", managedText);
			Assert.NotEqual("<not-sampled>", nativeText);
			Assert.True(
				string.IsNullOrEmpty(managedText) && string.IsNullOrEmpty(nativeText),
				$"Binding.DoNothing should leave the Entry text empty; observed managed text: '{managedText}', native text: '{nativeText}'");
		}

		sealed class DoNothingConverter : IValueConverter
		{
			public int ConvertCallCount { get; private set; }

			public object ConvertedValue { get; private set; }

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				ConvertCallCount++;
				ConvertedValue = value;
				return Binding.DoNothing;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
				Binding.DoNothing;
		}
	}
}
#endif

