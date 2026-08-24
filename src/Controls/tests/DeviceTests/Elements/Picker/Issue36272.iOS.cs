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
		public async Task RemovedPickersDoNotRemainSubscribedToSharedItemsSource()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
				});
			});

			var sharedItems = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
			var pickerHost = new VerticalStackLayout { Spacing = 8 };
			var pickers = new[]
			{
				CreatePicker(sharedItems, 0),
				CreatePicker(sharedItems, 1),
				CreatePicker(sharedItems, 2),
			};

			foreach (var picker in pickers)
				pickerHost.Children.Add(picker);

			var removalCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var clickCount = -1;
			var removeButton = new Button { Text = "Remove Pickers and Check" };
			removeButton.Clicked += (_, _) =>
			{
				while (pickerHost.Children.Count > 0)
					pickerHost.Children.RemoveAt(pickerHost.Children.Count - 1);

				clickCount = 1;
				removalCompleted.SetResult(true);
			};

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Picker shared ItemsSource retention",
						FontSize = 22,
						FontAttributes = FontAttributes.Bold,
					},
					new Label
					{
						Text = "Each default-styled Picker below uses the same long-lived ObservableCollection.",
					},
					pickerHost,
					removeButton,
				},
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = content },
			};
			var references = Array.ConvertAll(pickers, picker => new WeakReference(picker));

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.Equal(3, pickerHost.Children.Count);
				for (var index = 0; index < pickers.Length; index++)
				{
					Assert.Same(pickers[index], pickerHost.Children[index]);
					Assert.Equal($"SharedItemsPicker{index + 1}", pickers[index].AutomationId);
					var pickerHandler = Assert.IsType<PickerHandler>(pickers[index].Handler);
					Assert.NotNull(pickerHandler.PlatformView);
				}

				Assert.Equal(-1, clickCount);
				var buttonHandler = Assert.IsType<ButtonHandler>(removeButton.Handler);
				Assert.NotNull(buttonHandler.PlatformView);
				buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				Assert.True(await removalCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1)));
				Assert.Equal(1, clickCount);
				Assert.Empty(pickerHost.Children);

				for (var index = 0; index < pickers.Length; index++)
					Assert.Same(pickers[index], references[index].Target);
			});

			page = null;
			content = null;
			pickerHost = null;
			removeButton = null;
			for (var index = 0; index < pickers.Length; index++)
				pickers[index] = null;
			pickers = null;

			try
			{
				await AssertionExtensions.WaitForGC(references);
			}
			finally
			{
				GC.KeepAlive(sharedItems);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static Picker CreatePicker(ObservableCollection<string> sharedItems, int index)
		{
			return new Picker
			{
				AutomationId = $"SharedItemsPicker{index + 1}",
				ItemsSource = sharedItems,
				SelectedIndex = index,
				Title = $"Shared Picker {index + 1}",
			};
		}
	}
#endif
}

