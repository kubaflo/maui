#if MACCATALYST
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

[Category(TestCategory.Memory)]
[Category("Issue36272")]
public class Issue36272 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RemovedPickerDoesNotLeakThroughSharedItemsSource()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<IScrollView, ScrollViewHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<IContentView, ContentViewHandler>();
				handlers.AddHandler<Picker, PickerHandler>();
			});
		});

		var sharedItems = new ObservableCollection<string> { "a", "b", "c" };
		var pickerReference = await CreateAndRemovePicker(sharedItems);

		await AssertionExtensions.WaitForGC(pickerReference);
		GC.KeepAlive(sharedItems);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<WeakReference> CreateAndRemovePicker(ObservableCollection<string> sharedItems)
	{
		bool loaded = false;
		bool unloaded = false;
		Picker picker = null;
		ContentView host = null;
		Window window = null;

		await InvokeOnMainThreadAsync(() =>
		{
			picker = new Picker
			{
				ItemsSource = sharedItems,
				Title = "Shared items picker"
			};
			picker.Loaded += (_, _) => loaded = true;
			picker.Unloaded += (_, _) => unloaded = true;

			host = new ContentView
			{
				Content = picker,
				MinimumHeightRequest = 60
			};
			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children = { host }
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = stack
				}
			};
			window = new Window(page);
		});

		await CreateHandlerAndAddToWindow(window, async () =>
		{
			var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
			Assert.NotNull(pickerHandler.PlatformView);
			Assert.Same(sharedItems, picker.ItemsSource);
			Assert.Equal(new[] { "a", "b", "c" }, sharedItems);
			Assert.Equal("Shared items picker", picker.Title);
			Assert.Null(picker.Style);
			Assert.Same(host, picker.Parent);
			Assert.True(loaded);

			host.Content = null;
			await OnUnloadedAsync(picker);

			Assert.True(unloaded);
			Assert.Null(picker.Parent);
		});

		return new WeakReference(picker);
	}
}
#endif

