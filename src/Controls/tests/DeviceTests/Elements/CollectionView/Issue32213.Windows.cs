using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
#if WINDOWS
	[Category("Issue32213")]
	public class Issue32213 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HeaderAndFooterValuesUseTheirTemplates()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				ItemsSource = new[] { "1", "2", "3", "4" },
				Header = "Header",
				Footer = "Footer",
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, new Binding("."));
					return label;
				}),
				HeaderTemplate = new DataTemplate(() => new Label { Text = "HeaderTemplate" }),
				FooterTemplate = new DataTemplate(() => new Label { Text = "FooterTemplate" }),
			};

			var descriptionLabel = new Label { Text = "CollectionView header and footer" };
			var checkButton = new Button { Text = "Check header and footer" };
			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto },
				},
			};

			grid.Add(descriptionLabel);
			grid.Add(collectionView);
			grid.Add(checkButton);
			Grid.SetRow(collectionView, 1);
			Grid.SetRow(checkButton, 2);

			var page = new ContentPage { Content = grid };
			var window = new Window(page);
			var loadedObservation = "not observed";

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(window, async _ =>
			{
				var handler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var nativeList = Assert.IsAssignableFrom<WListViewBase>(handler.PlatformView);

				await AssertEventually(() =>
				{
					if (!nativeList.IsLoaded)
						return false;

					loadedObservation = "loaded";
					return true;
				}, timeout: 10000, message: "The native CollectionView did not load.");

				Assert.Equal("loaded", loadedObservation);

				string[] renderedTexts = [];
				await AssertEventually(() =>
				{
					renderedTexts = nativeList.GetChildren<WTextBlock>()
						.Select(textBlock => textBlock.Text)
						.ToArray();

					return new[] { "1", "2", "3", "4" }.All(renderedTexts.Contains);
				}, timeout: 10000, message: "The native CollectionView did not realize items 1 through 4.");

				bool headerTemplatePresent = false;
				bool footerTemplatePresent = false;
				bool headerTemplateTextRendered = false;
				bool footerTemplateTextRendered = false;
				bool rawHeaderTextRendered = false;
				bool rawFooterTextRendered = false;

				await AssertEventually(() =>
				{
					renderedTexts = nativeList.GetChildren<WTextBlock>()
						.Select(textBlock => textBlock.Text)
						.ToArray();
					headerTemplatePresent = nativeList.HeaderTemplate is not null;
					footerTemplatePresent = nativeList.FooterTemplate is not null;
					headerTemplateTextRendered = renderedTexts.Contains("HeaderTemplate");
					footerTemplateTextRendered = renderedTexts.Contains("FooterTemplate");
					rawHeaderTextRendered = renderedTexts.Contains("Header");
					rawFooterTextRendered = renderedTexts.Contains("Footer");

					return (headerTemplateTextRendered && footerTemplateTextRendered) ||
						(rawHeaderTextRendered && rawFooterTextRendered);
				}, timeout: 10000, message: "The native CollectionView did not render its header and footer.");

				Assert.True(
					headerTemplatePresent && headerTemplateTextRendered,
					$"Issue 32213 template rendering mismatch: header template present={headerTemplatePresent}, " +
					$"template text rendered={headerTemplateTextRendered}, raw text rendered={rawHeaderTextRendered}; " +
					$"footer template present={footerTemplatePresent}, template text rendered={footerTemplateTextRendered}, " +
					$"raw text rendered={rawFooterTextRendered}. Expected HeaderTemplate and FooterTemplate.");

				Assert.True(
					footerTemplatePresent && footerTemplateTextRendered,
					$"Issue 32213 template rendering mismatch: footer template present={footerTemplatePresent}, " +
					$"template text rendered={footerTemplateTextRendered}, raw text rendered={rawFooterTextRendered}. " +
					"Expected FooterTemplate.");
			});
		}
	}
#endif
}

