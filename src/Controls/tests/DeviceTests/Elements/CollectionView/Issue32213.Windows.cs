#if WINDOWS
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue32213")]
	public class Issue32213 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HeaderAndFooterTemplatesRenderConfiguredContent()
		{
			const string expectedHeaderText = "HeaderTemplate";
			const string expectedFooterText = "FooterTemplate";
			bool nativeListLoaded = false;
			bool headerRootLoaded = false;
			bool footerRootLoaded = false;
			bool headerTemplateLoaded = false;
			bool footerTemplateLoaded = false;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var items = new[] { "1", "2", "3", "4" };
			var itemTemplate = new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, new Binding("."));
				return label;
			});
			var headerTemplate = new DataTemplate(() =>
			{
				var label = new Label { Text = expectedHeaderText };
				label.Loaded += (_, _) => headerTemplateLoaded = true;
				return label;
			});
			var footerTemplate = new DataTemplate(() =>
			{
				var label = new Label { Text = expectedFooterText };
				label.Loaded += (_, _) => footerTemplateLoaded = true;
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
			var resultLabel = new Label
			{
				Text = "Header and footer templates have not been checked."
			};
			var checkButton = new Button
			{
				Text = "Check rendered templates"
			};
			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(resultLabel);
			grid.Add(collectionView);
			grid.Add(checkButton);
			Grid.SetRow(collectionView, 1);
			Grid.SetRow(checkButton, 2);
			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var collectionViewHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				WListViewBase platformList = collectionViewHandler.PlatformView;
				Assert.NotNull(platformList);

				nativeListLoaded = await AssertHelpers.Wait(() => platformList.IsLoaded, timeout: 5000);
				headerRootLoaded = await AssertHelpers.Wait(
					() => headerTemplateLoaded && platformList.GetChildren<WTextBlock>()
						.Any(textBlock => textBlock.IsLoaded && textBlock.Text == expectedHeaderText),
					timeout: 5000);
				footerRootLoaded = await AssertHelpers.Wait(
					() => footerTemplateLoaded && platformList.GetChildren<WTextBlock>()
						.Any(textBlock => textBlock.IsLoaded && textBlock.Text == expectedFooterText),
					timeout: 5000);

				Assert.Equal("Header", collectionView.Header);
				Assert.Equal("Footer", collectionView.Footer);
				Assert.Same(itemTemplate, collectionView.ItemTemplate);
				Assert.Same(headerTemplate, collectionView.HeaderTemplate);
				Assert.Same(footerTemplate, collectionView.FooterTemplate);
				Assert.Equal(items, collectionView.ItemsSource.Cast<string>());
				Assert.Equal(SelectionMode.Single, collectionView.SelectionMode);
				Assert.Null(collectionView.Style);

				var observedTexts = platformList.GetChildren<WTextBlock>()
					.Where(textBlock => textBlock.IsLoaded)
					.Select(textBlock => textBlock.Text)
					.ToArray();
				string observedTextSummary = string.Join(", ", observedTexts);

				Assert.True(nativeListLoaded,
					$"Expected native list loaded: True; actual: {nativeListLoaded}.");
				Assert.True(headerRootLoaded,
					$"Issue32213 header template did not render on Windows. Expected header root loaded: True; actual: {headerRootLoaded}. Observed native texts: {observedTextSummary}; expected text: {expectedHeaderText}.");
				Assert.True(footerRootLoaded,
					$"Expected footer root loaded: True; actual: {footerRootLoaded}. Observed native texts: {observedTextSummary}; expected text: {expectedFooterText}.");
				Assert.Contains(expectedHeaderText, observedTexts);
				Assert.Contains(expectedFooterText, observedTexts);
			});
		}
	}
}
#endif

