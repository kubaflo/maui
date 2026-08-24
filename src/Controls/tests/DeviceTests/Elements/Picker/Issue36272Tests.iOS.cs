using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Picker)]
	[Category("Issue36272")]
	public class Issue36272 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DetachedPickerDoesNotRemainAliveThroughSharedItemsSource()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
				});
			});

			var sharedItems = new ObservableCollection<string> { "a", "b", "c" };
			var picker = new Picker { ItemsSource = sharedItems };
			var pickerReference = new WeakReference(picker);
			var pickerHost = new ContentView { Content = picker };
			var replacementLabel = new Label { Text = "Picker detached; shared collection remains alive." };
			var collectionLifetimeLabel = new Label
			{
				Text = "Shared collection remains alive during collection check.",
				FontAttributes = FontAttributes.Bold
			};
			var triggerCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var callbackMarker = -1;
			var button = new Button { Text = "Drop Picker and check collection" };

			button.Clicked += (_, _) =>
			{
				pickerHost.Content = replacementLabel;
				picker = null;
				callbackMarker = 1;
				triggerCompleted.SetResult(true);
			};

			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 16,
				VerticalOptions = LayoutOptions.Center,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(new Label { Text = "Affected default Picker backed by a shared collection:" }, 0, 0);
			grid.Add(pickerHost, 0, 1);
			grid.Add(button, 0, 2);
			grid.Add(collectionLifetimeLabel, 0, 3);

			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				AssertPickerAttached(picker);
				Assert.Same(sharedItems, picker.ItemsSource);
				Assert.Equal(3, picker.ItemsSource.Count);
				Assert.Equal("a", picker.ItemsSource[0]);
				Assert.Equal("b", picker.ItemsSource[1]);
				Assert.Equal("c", picker.ItemsSource[2]);

				SendTouchUpInside(button);
				await triggerCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.Equal(1, callbackMarker);
				Assert.Same(replacementLabel, pickerHost.Content);
				AssertPickerDetached(pickerReference);

				await AssertionExtensions.WaitForGC(pickerReference);
				GC.KeepAlive(sharedItems);
			});
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void AssertPickerAttached(Picker picker)
		{
			Assert.NotNull(picker.Handler);
			var platformPicker = picker.Handler.PlatformView as UIView;
			Assert.NotNull(platformPicker);
			Assert.NotNull(platformPicker.Window);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void AssertPickerDetached(WeakReference pickerReference)
		{
			var picker = pickerReference.Target as Picker;
			Assert.NotNull(picker);
			Assert.Null(picker.Parent);
			Assert.NotNull(picker.Handler);

			var platformPicker = picker.Handler.PlatformView as UIView;
			Assert.NotNull(platformPicker);
			Assert.Null(platformPicker.Window);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void SendTouchUpInside(Button button)
		{
			Assert.NotNull(button.Handler);
			var platformButton = button.Handler.PlatformView as UIButton;
			Assert.NotNull(platformButton);
			Assert.NotNull(platformButton.Window);
			platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
		}
	}
#endif
}

