#if MACCATALYST
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Collection(RunInNewWindowCollection)]
[Category("Issue36272")]
public class Issue36272 : ControlsHandlerTestBase
{
	void SetupBuilder()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<Grid, LayoutHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Picker, PickerHandler>();
			});
		});
	}

	[Fact]
	public async Task RemovedPickerIsNotRetainedBySharedItemsSource()
	{
		SetupBuilder();

		var sharedItems = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
		var pickerHost = new VerticalStackLayout
		{
			Spacing = 8
		};
		var picker = new Picker
		{
			Title = "Choose an item",
			ItemsSource = sharedItems,
			SelectedIndex = 0
		};
		int loadedState = -1;
		int unloadedState = -1;
		var unloadedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		picker.Loaded += (_, _) => loadedState = 1;
		picker.Unloaded += (_, _) =>
		{
			unloadedState = 1;
			unloadedSource.TrySetResult();
		};
		pickerHost.Add(picker);

		var resultArea = new VerticalStackLayout
		{
			Spacing = 8
		};
		resultArea.Add(new Label
		{
			Text = "Picker is attached and ready.",
			FontSize = 16
		});
		resultArea.Add(new Label
		{
			Text = "The removed Picker should be released.",
			FontSize = 20,
			FontAttributes = FontAttributes.Bold
		});

		var grid = new Grid
		{
			Padding = 24,
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};

		grid.Add(new Label
		{
			Text = "Picker shared collection retention",
			FontSize = 24,
			FontAttributes = FontAttributes.Bold
		});

		var description = new Label
		{
			Text = "The Picker below uses a long-lived shared collection. Remove it and check whether it can be collected.",
			FontSize = 16
		};
		Grid.SetRow(description, 1);
		grid.Add(description);

		Grid.SetRow(pickerHost, 2);
		grid.Add(pickerHost);

		var actionButton = new Button
		{
			Text = "Remove Picker and Check Memory"
		};
		Grid.SetRow(actionButton, 3);
		grid.Add(actionButton);

		Grid.SetRow(resultArea, 4);
		grid.Add(resultArea);

		var page = new ContentPage
		{
			Title = "Picker ItemsSource leak",
			Content = grid
		};

		await CreateHandlerAndAddToWindow(new Window(page), async () =>
		{
			await OnLoadedAsync(picker);
			AssertPickerIsReady(pickerHost, picker, sharedItems, loadedState);

			WeakReference pickerReference = RemovePicker(pickerHost, ref picker);

			await unloadedSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
			Assert.Equal(1, unloadedState);
			Assert.Empty(pickerHost.Children);

			await AssertionExtensions.WaitForGC(pickerReference);
			GC.KeepAlive(sharedItems);
			GC.KeepAlive(page);
		});
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void AssertPickerIsReady(
		VerticalStackLayout pickerHost,
		Picker picker,
		ObservableCollection<string> sharedItems,
		int loadedState)
	{
		Assert.Single(pickerHost.Children);
		Assert.Same(picker, pickerHost.Children[0]);
		Assert.Same(sharedItems, picker.ItemsSource);
		Assert.Equal("Alpha", picker.SelectedItem);
		Assert.Equal(1, loadedState);

		var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
		Assert.IsType<MauiPicker>(pickerHandler.PlatformView);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference RemovePicker(VerticalStackLayout pickerHost, ref Picker picker)
	{
		var pickerReference = new WeakReference(picker);
		Assert.True(pickerHost.Remove(picker));
		Assert.DoesNotContain(picker, pickerHost.Children);
		picker = null;
		return pickerReference;
	}
}
#endif

