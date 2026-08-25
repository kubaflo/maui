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

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category(TestCategory.Memory)]
[Category("Issue36272")]
public class Issue36272 : ControlsHandlerTestBase
{
	[Fact]
	public async Task RemovedPickerIsCollectedWhileItemsSourceRemainsAlive()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<ContentPage, PageHandler>();
				handlers.AddHandler<ScrollView, ScrollViewHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Picker, PickerHandler>();
			});
		});

		var sharedItems = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
		var pickerHost = new VerticalStackLayout();
		var picker = new Picker
		{
			Title = "Shared collection picker",
			ItemsSource = sharedItems
		};
		pickerHost.Children.Add(picker);

		var removeButton = new Button
		{
			Text = "Remove Picker and check collection"
		};
		var measurementLabel = new Label
		{
			Text = "WeakReference alive: not checked"
		};
		var resultLabel = new Label
		{
			FontAttributes = FontAttributes.Bold,
			Text = "Ready to remove Picker"
		};
		var content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16
		};
		content.Children.Add(new Label
		{
			FontAttributes = FontAttributes.Bold,
			FontSize = 20,
			Text = "Shared Picker ItemsSource leak"
		});
		content.Children.Add(new Label
		{
			Text = "The Picker below uses a long-lived ObservableCollection. Run the check to remove the Picker and test whether the collection still retains it."
		});
		content.Children.Add(pickerHost);
		content.Children.Add(removeButton);
		content.Children.Add(measurementLabel);
		content.Children.Add(resultLabel);

		var page = new ContentPage
		{
			Title = "Picker ItemsSource Leak",
			Content = new ScrollView { Content = content }
		};

		WeakReference pickerReference = null;
		int callbackState = -1;
		bool removedExactPicker = false;
		removeButton.Clicked += (_, _) =>
		{
			callbackState = 1;
			pickerReference = new WeakReference(picker);
			removedExactPicker = pickerHost.Children.Remove(picker);
			picker = null;
		};

		await CreateHandlerAndAddToWindow(new Window(page), async () =>
		{
			await OnLoadedAsync(page);

			Assert.Same(picker, Assert.Single(pickerHost.Children));
			Assert.Same(sharedItems, picker.ItemsSource);
			Assert.Collection(
				sharedItems,
				item => Assert.Equal("Alpha", item),
				item => Assert.Equal("Beta", item),
				item => Assert.Equal("Gamma", item));

			Assert.NotNull(Assert.IsType<PickerHandler>(picker.Handler).PlatformView);
			var buttonHandler = Assert.IsType<ButtonHandler>(removeButton.Handler);
			Assert.NotNull(buttonHandler.PlatformView);

			buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

			Assert.Equal(1, callbackState);
			Assert.True(removedExactPicker);
			Assert.Empty(pickerHost.Children);
			Assert.NotNull(pickerReference);

			await AssertionExtensions.WaitForGC(pickerReference);
			GC.KeepAlive(sharedItems);
		});
	}
}
#endif

