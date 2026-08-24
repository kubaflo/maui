#if MACCATALYST
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Category(TestCategory.Picker)]
[Category("Issue36272")]
public class Issue36272 : ControlsHandlerTestBase
{
	void SetupBuilder()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<IContentView, ContentViewHandler>();
				handlers.AddHandler<Grid, LayoutHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Picker, PickerHandler>();
			});
		});
	}

	[Fact]
	public async Task RemovedPickerDoesNotLeakThroughSharedItemsSource()
	{
		SetupBuilder();

		var sharedItems = new ObservableCollection<string> { "a", "b", "c" };

		var pickerReference = await CreateAndReleasePicker(sharedItems);

		await AssertionExtensions.WaitForGC(pickerReference);
		GC.KeepAlive(sharedItems);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<WeakReference> CreateAndReleasePicker(ObservableCollection<string> sharedItems)
	{
		var lifecycleState = -1;
		var unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var picker = new Picker
		{
			ItemsSource = sharedItems
		};
		picker.Loaded += (_, _) => lifecycleState = 0;
		picker.Unloaded += (_, _) =>
		{
			lifecycleState = 1;
			unloaded.TrySetResult();
		};

		var pickerHost = new VerticalStackLayout
		{
			Spacing = 8,
			Children =
			{
				new Label { Text = "Affected default-state Picker:" },
				picker
			}
		};

		var headingLabel = new Label
		{
			Text = "Issue 36272: shared Picker ItemsSource retention",
			FontSize = 22,
			FontAttributes = FontAttributes.Bold
		};
		var releaseButton = new Button
		{
			Text = "Release Picker and check collection"
		};
		var statusLabel = new Label
		{
			Text = "Picker is attached; retention has not been checked.",
			FontAttributes = FontAttributes.Bold
		};
		var grid = new Grid
		{
			Padding = new Thickness(24),
			RowSpacing = 16,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				headingLabel,
				pickerHost,
				releaseButton,
				statusLabel
			}
		};
		Grid.SetRow(pickerHost, 1);
		Grid.SetRow(releaseButton, 2);
		Grid.SetRow(statusLabel, 3);

		var page = new ContentPage { Content = grid };
		var pickerReference = new WeakReference(picker);

		await CreateHandlerAndAddToWindow(page, async () =>
		{
			Assert.Equal(0, lifecycleState);
			Assert.True(picker.IsLoaded);
			Assert.Equal(2, pickerHost.Children.Count);
			Assert.Same(picker, pickerHost.Children[1]);
			Assert.Same(sharedItems, picker.ItemsSource);
			Assert.Equal(3, picker.ItemsSource.Count);
			Assert.Equal("a", picker.ItemsSource[0]);
			Assert.Equal("b", picker.ItemsSource[1]);
			Assert.Equal("c", picker.ItemsSource[2]);
			Assert.Equal(-1, picker.SelectedIndex);

			var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
			var nativePicker = Assert.IsType<MauiPicker>(pickerHandler.PlatformView);
			Assert.NotNull(nativePicker.Superview);

			await InvokeOnMainThreadAsync(() =>
			{
				pickerHost.Children.Clear();
				statusLabel.Text = "Picker removed; checking whether it can be collected.";
			});
			await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(2));

			Assert.Equal(1, lifecycleState);
			Assert.Empty(pickerHost.Children);
		});

		return pickerReference;
	}
}
#endif

