#if MACCATALYST
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

[Category(TestCategory.Memory)]
[Category("Issue36272")]
public class Issue36272 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RemovedPickerWithSharedItemsSourceIsCollected()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<ContentView, ContentViewHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Picker, PickerHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
			});
		});

		var sharedItems = new ObservableCollection<string>
		{
			"Alpha",
			"Beta",
			"Gamma"
		};
		var pickerHost = new ContentView
		{
			Content = new Picker
			{
				ItemsSource = sharedItems
			}
		};
		var removeButton = new Button
		{
			Text = "Remove Picker and check collection"
		};
		var resultLabel = new Label
		{
			Text = "Picker removal status",
			FontAttributes = FontAttributes.Bold,
			FontSize = 18
		};
		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Picker shared ItemsSource retention",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold
					},
					new Label
					{
						Text = "The default-state Picker below uses a shared, long-lived collection. Remove it to check whether that collection retains it."
					},
					new Label
					{
						Text = "Affected Picker (no selection):"
					},
					pickerHost,
					removeButton,
					resultLabel
				}
			}
		};

		var callbackCount = 0;
		WeakReference pickerReference = null;
		removeButton.Clicked += (sender, args) =>
		{
			callbackCount++;
			var removedPicker = Assert.IsType<Picker>(pickerHost.Content);
			pickerReference = new WeakReference(removedPicker);
			pickerHost.Content = null;

			Assert.Null(removedPicker.Parent);
		};

		await CreateHandlerAndAddToWindow(new Window(page), () =>
		{
			var picker = Assert.IsType<Picker>(pickerHost.Content);
			Assert.Same(sharedItems, picker.ItemsSource);
			Assert.Collection(
				sharedItems,
				item => Assert.Equal("Alpha", item),
				item => Assert.Equal("Beta", item),
				item => Assert.Equal("Gamma", item));
			Assert.Equal(-1, picker.SelectedIndex);
			Assert.Same(picker, pickerHost.Content);
			Assert.NotNull(picker.Handler);
			Assert.NotNull(picker.Handler.PlatformView);

			Assert.NotNull(removeButton.Handler);
			var nativeButton = Assert.IsAssignableFrom<UIButton>(removeButton.Handler.PlatformView);
			nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, callbackCount);
			Assert.NotNull(pickerReference);
			Assert.Null(pickerHost.Content);
		});

		Assert.NotNull(pickerReference);
		await AssertionExtensions.WaitForGC(pickerReference);
		GC.KeepAlive(sharedItems);
	}
}
#endif

