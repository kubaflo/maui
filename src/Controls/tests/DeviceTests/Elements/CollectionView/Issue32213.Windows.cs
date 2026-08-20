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
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(RunInNewWindowCollection)]
	[Category("Issue32213")]
	public class Issue32213 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HeaderAndFooterTemplatesRenderWhenContentIsSet()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			Label oracleHeaderLabel = null;
			Label oracleFooterLabel = null;
			var oracleCollectionView = new CollectionView
			{
				ItemsSource = new[] { "1", "2", "3", "4" },
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, ".");
					return label;
				}),
				HeaderTemplate = new DataTemplate(() =>
				{
					oracleHeaderLabel = new Label { Text = "OracleHeaderTemplate" };
					return oracleHeaderLabel;
				}),
				FooterTemplate = new DataTemplate(() =>
				{
					oracleFooterLabel = new Label { Text = "OracleFooterTemplate" };
					return oracleFooterLabel;
				})
			};
			var oracleGrid = new Grid
			{
				oracleCollectionView
			};
			int oracleLayoutCallbackCount = -1;

			await CreateHandlerAndAddToWindow<LayoutHandler>(oracleGrid, async _ =>
			{
				var nativeCollectionView = (WListViewBase)oracleCollectionView.Handler.PlatformView;
				nativeCollectionView.LayoutUpdated += (_, _) => oracleLayoutCallbackCount++;

				await AssertEventually(() => oracleLayoutCallbackCount >= 0);
				Assert.True(oracleLayoutCallbackCount >= 0, "The oracle CollectionView did not receive a post-attachment native layout callback.");

				await AssertEventually(() =>
					IsLoadedTextBlock(oracleHeaderLabel, "OracleHeaderTemplate") &&
					IsLoadedTextBlock(oracleFooterLabel, "OracleFooterTemplate"));
				Assert.True(IsLoadedTextBlock(oracleHeaderLabel, "OracleHeaderTemplate"));
				Assert.True(IsLoadedTextBlock(oracleFooterLabel, "OracleFooterTemplate"));
			});

			Label headerTemplateLabel = null;
			Label footerTemplateLabel = null;
			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				ItemsSource = new[] { "1", "2", "3", "4" },
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label();
					label.SetBinding(Label.TextProperty, ".");
					return label;
				}),
				Header = "Header",
				HeaderTemplate = new DataTemplate(() =>
				{
					headerTemplateLabel = new Label { Text = "HeaderTemplate" };
					return headerTemplateLabel;
				}),
				Footer = "Footer",
				FooterTemplate = new DataTemplate(() =>
				{
					footerTemplateLabel = new Label { Text = "FooterTemplate" };
					return footerTemplateLabel;
				})
			};
			var grid = new Grid
			{
				collectionView
			};
			int layoutCallbackCount = -1;

			await CreateHandlerAndAddToWindow<LayoutHandler>(grid, async _ =>
			{
				var nativeCollectionView = (WListViewBase)collectionView.Handler.PlatformView;
				nativeCollectionView.LayoutUpdated += (_, _) => layoutCallbackCount++;

				await AssertEventually(() => layoutCallbackCount >= 0);
				Assert.True(layoutCallbackCount >= 0, "The reported CollectionView did not receive a post-attachment native layout callback.");

				await AssertEventually(() =>
				{
					var textBlocks = nativeCollectionView.GetChildren<WTextBlock>();
					return textBlocks.Any(textBlock => textBlock.Text == "1") &&
						textBlocks.Any(textBlock => textBlock.Text == "4");
				});

				var renderedTextBlocks = nativeCollectionView.GetChildren<WTextBlock>().ToList();
				Assert.Contains(renderedTextBlocks, textBlock => textBlock.Text == "1");
				Assert.Contains(renderedTextBlocks, textBlock => textBlock.Text == "4");

				bool headerTemplateRendered = IsLoadedTextBlock(headerTemplateLabel, "HeaderTemplate");
				bool footerTemplateRendered = IsLoadedTextBlock(footerTemplateLabel, "FooterTemplate");
				string nativeHeaderText = (nativeCollectionView.Header as WTextBlock)?.Text ??
					(headerTemplateLabel?.Handler?.PlatformView as WTextBlock)?.Text ?? string.Empty;
				string nativeFooterText = (nativeCollectionView.Footer as WTextBlock)?.Text ??
					(footerTemplateLabel?.Handler?.PlatformView as WTextBlock)?.Text ?? string.Empty;

				Assert.True(
					headerTemplateRendered && nativeHeaderText == "HeaderTemplate" &&
					footerTemplateRendered && nativeFooterText == "FooterTemplate",
					$"Header template rendered: {headerTemplateRendered}; native header text: '{nativeHeaderText}'; expected: 'HeaderTemplate'. Footer template rendered: {footerTemplateRendered}; native footer text: '{nativeFooterText}'; expected: 'FooterTemplate'.");
			});
		}

		static bool IsLoadedTextBlock(Label label, string expectedText) =>
			label?.Handler?.PlatformView is WTextBlock textBlock &&
			textBlock.IsLoaded() &&
			textBlock.Text == expectedText;
	}
}
#endif

