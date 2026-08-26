#if WINDOWS
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WComboBox = Microsoft.UI.Xaml.Controls.ComboBox;
using WContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29063")]
	public class Issue29063 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptySelectedItemRendersEmptyTextInsideViewCell()
		{
			EnsureHandlerCreated(builder =>
				builder.ConfigureMauiHandlers(handlers => handlers.AddMauiControlsHandlers()));

			var items = new[] { string.Empty, "Active", "Inactive", "Pending" };
			var bindingContext = new PickerViewModel();
			var picker = new Picker
			{
				Title = "Status",
				ItemsSource = items
			};
			picker.SetBinding(Picker.SelectedItemProperty, nameof(PickerViewModel.Status));

#pragma warning disable CS0618 // TableView is required to reproduce the reported ViewCell rendering path.
			var tableView = new TableView
			{
				Intent = TableIntent.Form,
				Root = new TableRoot("MAUI Issue 29063")
				{
					new TableSection("Picker Issue")
					{
						new ViewCell { View = picker }
					}
				}
			};
#pragma warning restore CS0618

			var page = new ContentPage
			{
				BindingContext = bindingContext,
				Content = new VerticalStackLayout
				{
					Padding = new Thickness(30, 0),
					Spacing = 25,
					Children = { tableView }
				}
			};

			var pickerLoadedCompletion = new TaskCompletionSource<bool>();
			picker.Loaded += (_, _) => pickerLoadedCompletion.TrySetResult(true);
			Assert.False(pickerLoadedCompletion.Task.IsCompleted);

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(
					() => pickerLoadedCompletion.Task.IsCompleted,
					message: "Picker did not complete its initial Loaded lifecycle.");
				Assert.True(await pickerLoadedCompletion.Task);

				Assert.Same(items, picker.ItemsSource);
				Assert.Equal(string.Empty, picker.ItemsSource[0]);
				Assert.Equal(string.Empty, picker.SelectedItem);
				Assert.Equal(0, picker.SelectedIndex);

				var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
				WComboBox platformPicker = pickerHandler.PlatformView;
				Assert.NotNull(platformPicker);
				Assert.Equal(0, platformPicker.SelectedIndex);

				WContentPresenter contentPresenter = null;
				await AssertEventually(
					() =>
					{
						contentPresenter = platformPicker.GetDescendantByName<WContentPresenter>("ContentPresenter");
						return contentPresenter is not null;
					},
					message: "Picker native ContentPresenter was not created.");
				Assert.NotNull(contentPresenter);

				WTextBlock textBlock = null;
				await AssertEventually(
					() =>
					{
						textBlock = contentPresenter.GetFirstDescendant<WTextBlock>();
						return textBlock is not null;
					},
					message: "Picker native TextBlock was not created.");
				Assert.NotNull(textBlock);

				var renderedText = textBlock.Text;
				Assert.True(
					renderedText == string.Empty,
					$"Picker rendered unexpected text for the selected empty string. Expected '', actual '{renderedText}'.");
			});
		}

		sealed class PickerViewModel
		{
			public string Status { get; set; } = string.Empty;
		}
	}
}
#endif

