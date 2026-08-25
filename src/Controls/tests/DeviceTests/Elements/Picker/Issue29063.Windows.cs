#if WINDOWS
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WComboBox = Microsoft.UI.Xaml.Controls.ComboBox;
using WContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29063")]
	public class Issue29063 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptySelectedItemRendersEmptyTextInsideViewCell()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
#pragma warning disable CS0618 // Type or member is obsolete
					handlers.AddHandler<TableView, TableViewRenderer>();
					handlers.AddHandler<ViewCell, ViewCellRenderer>();
#pragma warning restore CS0618 // Type or member is obsolete
				});
			});

			await InvokeOnMainThreadAsync(async () =>
			{
				var items = new[]
				{
					string.Empty,
					"Active",
					"Inactive",
					"Pending"
				};
				var viewModel = new PickerViewModel();
				var picker = new Picker
				{
					Title = "Status",
					ItemsSource = items
				};
				picker.SetBinding(Picker.SelectedItemProperty, nameof(PickerViewModel.Status));

				var loadedState = -1;
				picker.Loaded += (_, _) => loadedState = 1;

				var viewCell = new ViewCell { View = picker };
				var section = new TableSection("Picker Issue") { viewCell };
				var root = new TableRoot("MAUI Issue 24276") { section };
				var tableView = new TableView
				{
					Intent = TableIntent.Form,
					Root = root
				};
				var layout = new VerticalStackLayout
				{
					Padding = new Thickness(30, 0),
					Spacing = 25,
					Children = { tableView }
				};
				var page = new ContentPage
				{
					Content = layout,
					BindingContext = viewModel
				};

				Assert.Equal(-1, loadedState);

				await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(page), async _ =>
				{
					Assert.Same(items, picker.ItemsSource);
					Assert.Equal(items, picker.ItemsSource.Cast<string>());
					Assert.Same(viewModel, picker.BindingContext);
					Assert.Equal(string.Empty, viewModel.Status);
					Assert.Equal(string.Empty, picker.SelectedItem);

					var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
					Assert.Same(picker, pickerHandler.VirtualView);
					var platformPicker = Assert.IsType<WComboBox>(pickerHandler.PlatformView);

					WContentPresenter contentPresenter = null;
					WTextBlock renderedTextBlock = null;
					await AssertEventually(
						() =>
						{
							contentPresenter = platformPicker.GetDescendantByName<WContentPresenter>("ContentPresenter");
							renderedTextBlock = contentPresenter?.GetFirstDescendant<WTextBlock>();
							return loadedState == 1 &&
								platformPicker.ActualWidth > 0 &&
								platformPicker.ActualHeight > 0 &&
								renderedTextBlock is not null;
						},
						timeout: 3000,
						message: "The attached Windows Picker did not render its native content.");

					Assert.Equal(1, loadedState);
					Assert.NotNull(contentPresenter);
					Assert.NotNull(renderedTextBlock);
					Assert.True(platformPicker.ActualWidth > 0);
					Assert.True(platformPicker.ActualHeight > 0);

					var renderedText = renderedTextBlock.Text;
					Assert.True(
						renderedText == string.Empty,
						$"Issue 29063: Windows Picker rendered '{renderedText}' for selected empty string; expected empty text.");
				});
			});
		}

		sealed class PickerViewModel
		{
			public string Status { get; set; } = string.Empty;
		}
	}
}
#endif

