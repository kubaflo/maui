#if WINDOWS
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
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
	public class Issue32213 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HeaderAndFooterTemplatesRenderWhenValuesAreSet()
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

			bool collectionViewLoaded = false;
			var headerTemplate = CreateLabelTemplate("HeaderTemplate");
			var footerTemplate = CreateLabelTemplate("FooterTemplate");
			var itemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					FontSize = 18,
					Padding = 8
				};
				label.SetBinding(Label.TextProperty, ".");
				return label;
			});

			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				ItemsSource = new[] { "1", "2", "3", "4" },
				Header = "Header",
				Footer = "Footer",
				ItemTemplate = itemTemplate,
				HeaderTemplate = headerTemplate,
				FooterTemplate = footerTemplate
			};
			collectionView.Loaded += (_, _) => collectionViewLoaded = true;

			var grid = new Grid
			{
				Padding = 20,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(new Label { Text = "READY: CollectionView rendered", FontAttributes = FontAttributes.Bold });
			grid.Add(collectionView, 0, 1);
			grid.Add(new Button { Text = "Check rendered templates" }, 0, 2);
			grid.Add(new Label { Text = "Template status", FontAttributes = FontAttributes.Bold }, 0, 3);

			var page = new ContentPage { Content = grid };
			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				await AssertEventually(() => collectionViewLoaded);
				Assert.True(collectionViewLoaded);

				var collectionViewHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var platformListView = Assert.IsAssignableFrom<WListView>(collectionViewHandler.PlatformView);
				Assert.True(platformListView.IsLoaded);

				string[] renderedTexts = Array.Empty<string>();
				await AssertEventually(() =>
				{
					renderedTexts = platformListView
						.GetChildren<WTextBlock>()
						.Select(textBlock => textBlock.Text)
						.Where(text => text is not null)
						.ToArray();

					return new[] { "1", "2", "3", "4" }.All(renderedTexts.Contains)
						&& (renderedTexts.Contains("Header") || renderedTexts.Contains("HeaderTemplate"))
						&& (renderedTexts.Contains("Footer") || renderedTexts.Contains("FooterTemplate"));
				});

				Assert.All(new[] { "1", "2", "3", "4" }, item => Assert.Contains(item, renderedTexts));
				Assert.Equal("Header", collectionView.Header);
				Assert.Equal("Footer", collectionView.Footer);
				Assert.Same(headerTemplate, collectionView.HeaderTemplate);
				Assert.Same(footerTemplate, collectionView.FooterTemplate);
				Assert.True(
					renderedTexts.Contains("HeaderTemplate"),
					$"Issue32213 header template native text was not rendered; observed native texts: {string.Join(", ", renderedTexts)}");
				Assert.True(
					renderedTexts.Contains("FooterTemplate"),
					$"Issue32213 footer template native text was not rendered; observed native texts: {string.Join(", ", renderedTexts)}");
			});
		}

		static DataTemplate CreateLabelTemplate(string text)
		{
			return new DataTemplate(() => new Label
			{
				Text = text,
				FontSize = 18,
				Padding = 8
			});
		}
	}
}
#endif
