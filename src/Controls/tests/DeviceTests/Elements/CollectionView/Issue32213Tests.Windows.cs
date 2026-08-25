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
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue32213")]
	public class Issue32213Tests : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HeaderAndFooterTemplatesRenderForNonNullValues()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var itemLoaded = false;
			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				Header = "Header",
				Footer = "Footer",
				ItemsSource = new[] { "1", "2", "3", "4" },
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label { FontSize = 18 };
					label.SetBinding(Label.TextProperty, new Binding("."));
					label.Loaded += (_, _) => itemLoaded = true;
					return label;
				}),
				HeaderTemplate = new DataTemplate(() => new Label
				{
					Text = "HeaderTemplate",
					FontSize = 24
				}),
				FooterTemplate = new DataTemplate(() => new Label
				{
					Text = "FooterTemplate",
					FontSize = 24
				})
			};

			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				},
				RowSpacing = 12
			};
			grid.Add(collectionView);
			grid.Add(new Button { Text = "Check rendered templates" }, 0, 1);
			grid.Add(new Label { Text = "CollectionView header and footer", FontSize = 18 }, 0, 2);

			var page = new ContentPage { Content = grid };

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await AssertEventually(
					() => itemLoaded,
					message: "Issue32213: CollectionView items were not loaded.");

				var platformView = Assert.IsAssignableFrom<WListView>(collectionView.Handler.PlatformView);
				await AssertEventually(
					() => platformView.ActualWidth > 0 && platformView.ActualHeight > 0,
					message: "Issue32213: native CollectionView did not acquire a nonzero frame.");

				var nativeTexts = platformView
					.GetChildren<WTextBlock>()
					.Select(textBlock => textBlock.Text)
					.ToArray();

				Assert.Contains("1", nativeTexts);
				Assert.Contains("2", nativeTexts);
				Assert.Contains("3", nativeTexts);
				Assert.Contains("4", nativeTexts);

				var observedTexts = string.Join(", ", nativeTexts);
				Assert.True(
					nativeTexts.Contains("HeaderTemplate"),
					$"Issue32213: HeaderTemplate was not rendered; observed native texts={observedTexts}");
				Assert.True(
					nativeTexts.Contains("FooterTemplate"),
					$"Issue32213: FooterTemplate was not rendered; observed native texts={observedTexts}");
			});
		}
	}
}
#endif

