#if WINDOWS
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
		public async Task EmptySelectedStringRendersAsEmptyText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Picker, PickerHandler>();
#pragma warning disable CS0618 // Type or member is obsolete
					handlers.AddHandler<TableView, TableViewRenderer>();
					handlers.AddHandler<ViewCell, ViewCellRenderer>();
#pragma warning restore CS0618 // Type or member is obsolete
				});
			});

			var source = new PickerSource
			{
				Items = [string.Empty, "Second value"],
				SelectedValue = string.Empty
			};
			var loadedSentinel = -1;
			var picker = new Picker
			{
				AutomationId = "AffectedPicker"
			};
			picker.Loaded += (_, _) => loadedSentinel = 1;
			picker.SetBinding(Picker.ItemsSourceProperty, nameof(PickerSource.Items));
			picker.SetBinding(Picker.SelectedItemProperty, nameof(PickerSource.SelectedValue));

#pragma warning disable CS0618 // Type or member is obsolete
			var tableView = new TableView
			{
				Root = new TableRoot
				{
					new TableSection
					{
						new ViewCell
						{
							View = picker
						}
					}
				}
			};
#pragma warning restore CS0618 // Type or member is obsolete

			var grid = new Grid
			{
				Padding = 24,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				},
				RowSpacing = 12
			};
			grid.Add(new Label
			{
				Text = "The Picker below should render an empty selected value."
			}, 0, 0);
			grid.Add(tableView, 0, 1);
			grid.Add(new Button
			{
				AutomationId = "CheckButton",
				Text = "Check rendered text"
			}, 0, 2);
			grid.Add(new Label
			{
				AutomationId = "ResultStatus",
				Text = "Status"
			}, 0, 3);

			var page = new ContentPage
			{
				BindingContext = source,
				Content = grid
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(() => loadedSentinel == 1,
					message: "The Picker Loaded callback did not run.");
				Assert.Equal(1, loadedSentinel);
				Assert.Equal(0, picker.SelectedIndex);
				Assert.Same(source.Items[0], picker.SelectedItem);
				Assert.NotNull(picker.Handler);

				var pickerHandler = Assert.IsType<PickerHandler>(picker.Handler);
				var platformPicker = Assert.IsType<WComboBox>(pickerHandler.PlatformView);
				WContentPresenter contentPresenter = null;
				WTextBlock renderedTextBlock = null;

				await AssertEventually(() =>
				{
					contentPresenter = platformPicker.GetDescendantByName<WContentPresenter>("ContentPresenter");
					renderedTextBlock = contentPresenter?.GetFirstDescendant<WTextBlock>();
					return renderedTextBlock is not null;
				}, message: "The Picker's rendered text was not created.");

				var renderedText = renderedTextBlock.Text;
				Assert.True(renderedText == string.Empty,
					$"Issue29063: Picker rendered text was '{renderedText}', expected an empty string.");
			});
		}

		sealed class PickerSource
		{
			public string[] Items { get; set; }

			public string SelectedValue { get; set; }
		}
	}
}
#endif

