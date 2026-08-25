#if MACCATALYST
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Category(TestCategory.Picker)]
[Category("Issue36272")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue36272 : ControlsHandlerTestBase
{
	[Fact]
	public async Task SharedItemsSourceDoesNotRetainUnloadedPicker()
	{
		var sharedItems = new ObservableCollection<string>
		{
			"Alpha",
			"Beta",
			"Gamma"
		};

		WeakReference pickerReference = await CreateAttachAndRemovePicker();

		try
		{
			await AssertionExtensions.WaitForGC(pickerReference);
			Assert.False(pickerReference.IsAlive, "The unloaded Picker should not be retained by its ItemsSource.");
		}
		finally
		{
			GC.KeepAlive(sharedItems);
		}

		async Task<WeakReference> CreateAttachAndRemovePicker()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
				});
			});

			var picker = new Picker
			{
				AutomationId = "LeakPicker",
				ItemsSource = sharedItems,
				SelectedIndex = 0,
				Title = "Shared Picker"
			};
			var pickerHost = new VerticalStackLayout
			{
				AutomationId = "PickerHost",
				Children =
				{
					picker
				}
			};
			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "Shared Picker collection leak"
					},
					new Label
					{
						Text = "The Picker below uses a long-lived ObservableCollection. Remove it, then check whether the collection still retains it."
					},
					pickerHost,
					new Button
					{
						AutomationId = "RemovePickerButton",
						Text = "Remove Picker"
					},
					new Button
					{
						AutomationId = "CheckRetentionButton",
						IsVisible = false,
						Text = "Check Collection Retention"
					},
					new Label
					{
						AutomationId = "ResultLabel",
						FontAttributes = FontAttributes.Bold,
						Text = "Collection retention status"
					}
				}
			};
			var page = new ContentPage
			{
				Title = "Picker ItemsSource retention",
				Content = new ScrollView
				{
					Content = content
				}
			};
			var unloadedCompletion = new TaskCompletionSource<bool>();
			bool unloaded = false;

			picker.Unloaded += (_, _) =>
			{
				unloaded = true;
				unloadedCompletion.TrySetResult(true);
			};

			await AttachAndRun<PageHandler>(page, async _ =>
			{
				var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
				Assert.NotNull(pickerHandler.PlatformView);
				Assert.Contains(picker, pickerHost.Children);
				Assert.False(unloaded);

				await InvokeOnMainThreadAsync(pickerHost.Clear);
				await unloadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));

				Assert.True(unloaded);
				Assert.DoesNotContain(picker, pickerHost.Children);
			});

			return new WeakReference(picker);
		}
	}
}
#endif

