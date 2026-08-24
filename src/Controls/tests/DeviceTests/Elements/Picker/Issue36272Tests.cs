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

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Picker)]
	[Category("Issue36272")]
	public class Issue36272 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SharedItemsSourceDoesNotRetainUnloadedPicker()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
				});
			});

			var sharedItems = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
			var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var lifecycleState = -1;
			var picker = new Picker
			{
				Title = "Shared collection Picker",
				ItemsSource = sharedItems,
				SelectedIndex = 0,
			};

			picker.Loaded += (_, _) =>
			{
				lifecycleState = 1;
				loaded.TrySetResult();
			};
			picker.Unloaded += (_, _) =>
			{
				lifecycleState = 2;
				unloaded.TrySetResult();
			};

			var pickerHost = new Grid
			{
				HeightRequest = 80,
			};
			pickerHost.Add(picker);

			var page = new ContentPage
			{
				Title = "Picker ItemsSource retention",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							Text = "A Picker below uses a shared, long-lived ObservableCollection. Remove it and check whether the collection retains it.",
						},
						pickerHost,
					},
				},
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await loaded.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.Equal(1, lifecycleState);
				AssertAttachedPicker(picker, sharedItems);

				var pickerReference = RemovePicker(pickerHost, picker);
				picker = null;

				await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.Equal(2, lifecycleState);
				await AssertionExtensions.WaitForGC(pickerReference);
				GC.KeepAlive(sharedItems);
			});
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static WeakReference RemovePicker(Grid pickerHost, Picker picker)
		{
			var pickerReference = new WeakReference(picker);
			Assert.True(pickerHost.Remove(picker));
			return pickerReference;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void AssertAttachedPicker(Picker picker, ObservableCollection<string> sharedItems)
		{
			var handler = Assert.IsType<PickerHandler>(picker.Handler);
			Assert.NotNull(handler.PlatformView);
			Assert.Same(sharedItems, picker.ItemsSource);
			Assert.Equal(3, picker.Items.Count);
		}
	}
}
#endif

