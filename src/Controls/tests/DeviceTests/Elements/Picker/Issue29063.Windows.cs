#if WINDOWS
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
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
		public async Task EmptySelectedItemRendersAsEmptyTextInsideViewCell()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
#pragma warning disable CS0618 // Type or member is obsolete
					handlers.AddHandler<TableView, TableViewRenderer>();
					handlers.AddHandler<ViewCell, ViewCellRenderer>();
#pragma warning restore CS0618 // Type or member is obsolete
					handlers.AddHandler<Picker, PickerHandler>();
				});
			});

			var items = new List<string>
			{
				string.Empty,
				"Active",
				"Inactive",
				"Pending"
			};
			var issueViewModel = new IssueViewModel { Status = string.Empty };
			var picker = new Picker
			{
				Title = "Status",
				ItemsSource = items
			};
			picker.SetBinding(Picker.SelectedItemProperty, nameof(IssueViewModel.Status));

#pragma warning disable CS0618 // Type or member is obsolete
			var viewCell = new ViewCell { View = picker };
			var tableView = new TableView
			{
				Intent = TableIntent.Form,
				Root = new TableRoot("Picker Issue")
				{
					new TableSection("Picker with empty item")
					{
						viewCell
					}
				}
			};
#pragma warning restore CS0618 // Type or member is obsolete

			var loadedState = -1;
			picker.Loaded += (_, _) => loadedState = 1;

			var page = new ContentPage
			{
				BindingContext = issueViewModel,
				Content = new VerticalStackLayout
				{
					Padding = new Thickness(30, 0),
					Spacing = 20,
					Children = { tableView }
				}
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await AssertEventually(
					() => loadedState == 1,
					message: "Picker did not reach its post-attachment Loaded callback");

				Assert.Equal(1, loadedState);
				Assert.Same(items[0], picker.SelectedItem);
				Assert.Equal(0, picker.SelectedIndex);
				Assert.Equal(string.Empty, issueViewModel.Status);

				var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
				var platformPicker = Assert.IsType<WComboBox>(pickerHandler.PlatformView);
				Assert.True(platformPicker.IsLoaded);

				WContentPresenter contentPresenter = null;
				await AssertEventually(
					() =>
					{
						contentPresenter = platformPicker.GetDescendantByName<WContentPresenter>("ContentPresenter");
						return contentPresenter is not null;
					},
					message: "Picker selected-content presenter was not created");
				Assert.NotNull(contentPresenter);

				WTextBlock selectedTextBlock = null;
				await AssertEventually(
					() =>
					{
						selectedTextBlock = contentPresenter.GetFirstDescendant<WTextBlock>();
						return selectedTextBlock is not null;
					},
					message: "Picker selected-content TextBlock was not created");
				Assert.NotNull(selectedTextBlock);

				var renderedText = "__not_observed__";
				await AssertEventually(
					() =>
					{
						renderedText = selectedTextBlock.Text;
						return renderedText != "__not_observed__";
					},
					message: "Picker selected-content text was not observed");

				Assert.True(
					renderedText == string.Empty,
					$"Picker with empty selected string rendered unexpected native text. Expected: '{string.Empty}'. Observed: '{renderedText}'.");
			});
		}

		sealed class IssueViewModel
		{
			public string Status { get; set; }
		}
	}
}
#endif

