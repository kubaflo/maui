#if WINDOWS
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
using MWindow = Microsoft.Maui.Controls.Window;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(RunInNewWindowCollection)]
	public class Issue32213 : ControlsHandlerTestBase
	{
		[Fact]
		[Category("Issue32213")]
		public async Task HeaderAndFooterTemplatesRenderWhenValuesAreSet()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<MWindow, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			bool collectionViewLoaded = false;
			bool headerTemplateLoaded = false;
			bool footerTemplateLoaded = false;
			Label headerTemplateLabel = null;
			Label footerTemplateLabel = null;
			var items = new[] { "1", "2", "3", "4" };

			var headerTemplate = new DataTemplate(() =>
			{
				headerTemplateLabel = new Label { Text = "HeaderTemplate" };
				headerTemplateLabel.Loaded += (_, _) => headerTemplateLoaded = true;
				return headerTemplateLabel;
			});
			var footerTemplate = new DataTemplate(() =>
			{
				footerTemplateLabel = new Label { Text = "FooterTemplate" };
				footerTemplateLabel.Loaded += (_, _) => footerTemplateLoaded = true;
				return footerTemplateLabel;
			});
			var itemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, ".");
				return label;
			});

			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				Header = "Header",
				Footer = "Footer",
				ItemsSource = items,
				ItemTemplate = itemTemplate,
				HeaderTemplate = headerTemplate,
				FooterTemplate = footerTemplate
			};
			collectionView.Loaded += (_, _) => collectionViewLoaded = true;

			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 12,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(new Label
			{
				Text = "CollectionView header and footer templates",
				LineBreakMode = LineBreakMode.WordWrap
			});
			grid.Add(collectionView);
			Grid.SetRow(collectionView, 1);
			var checkButton = new Button { Text = "Check rendered templates", IsEnabled = false };
			grid.Add(checkButton);
			Grid.SetRow(checkButton, 2);
			var statusLabel = new Label { Text = "Template status", FontAttributes = FontAttributes.Bold };
			grid.Add(statusLabel);
			Grid.SetRow(statusLabel, 3);

			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.NotNull(collectionView.Handler);
				var collectionViewHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var platformView = collectionViewHandler.PlatformView;
				Assert.NotNull(platformView);

				string[] NativeTexts() => platformView
					.GetChildren<WTextBlock>()
					.Select(textBlock => textBlock.Text)
					.Where(text => text != null)
					.ToArray();

				await AssertEventually(() =>
					collectionViewLoaded &&
					platformView.ActualWidth > 0 &&
					platformView.ActualHeight > 0 &&
					items.All(item => NativeTexts().Contains(item)));

				Assert.True(collectionViewLoaded);
				Assert.True(platformView.ActualWidth > 0);
				Assert.True(platformView.ActualHeight > 0);
				Assert.Same(items, collectionView.ItemsSource);
				Assert.Same(itemTemplate, collectionView.ItemTemplate);
				Assert.Same(headerTemplate, collectionView.HeaderTemplate);
				Assert.Same(footerTemplate, collectionView.FooterTemplate);

				await AssertEventually(() => headerTemplateLoaded || NativeTexts().Contains("Header"));
				await AssertEventually(() => footerTemplateLoaded || NativeTexts().Contains("Footer"));

				var renderedTexts = string.Join(", ", NativeTexts());
				Assert.True(
					headerTemplateLabel != null && headerTemplateLoaded && renderedTexts.Contains("HeaderTemplate", System.StringComparison.Ordinal),
					$"Issue32213 header template was not rendered. Native text: [{renderedTexts}]");
				Assert.True(
					footerTemplateLabel != null && footerTemplateLoaded && renderedTexts.Contains("FooterTemplate", System.StringComparison.Ordinal),
					$"Issue32213 footer template was not rendered. Native text: [{renderedTexts}]");
			});
		}
	}
}
#endif

