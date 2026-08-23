#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue35889")]
	public class Issue35889 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewInAutoGridRowHasZeroNativeHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				});
			});

			var beforeLabel = new Label { Text = "before collectionview" };
			var itemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
			var itemTemplate = new DataTemplate(() => new Label { Text = "Hello World" });
			var emptyCollectionView = new CollectionView
			{
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Colors.Red,
				ItemsLayout = itemsLayout,
				ItemTemplate = itemTemplate
			};
			var afterLabel = new Label { Text = "after collectionview" };
			var grid = new Grid
			{
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(beforeLabel);
			grid.Add(emptyCollectionView);
			Grid.SetRow(emptyCollectionView, 1);
			grid.Add(afterLabel);
			Grid.SetRow(afterLabel, 2);

			var page = new ContentPage { Content = grid };
			var nativeHeight = -1d;

			await CreateHandlerAndAddToWindow(page, () =>
			{
				Assert.Null(emptyCollectionView.ItemsSource);
				Assert.Same(itemTemplate, emptyCollectionView.ItemTemplate);
				Assert.Same(itemsLayout, emptyCollectionView.ItemsLayout);
				Assert.Equal(1, Grid.GetRow(emptyCollectionView));
				Assert.Equal(0, Grid.GetRow(beforeLabel));
				Assert.Equal(2, Grid.GetRow(afterLabel));

				var collectionViewHandler = Assert.IsType<CollectionViewHandler2>(emptyCollectionView.Handler);
				Assert.Same(emptyCollectionView, collectionViewHandler.VirtualView);
				var nativeCollectionView = collectionViewHandler.Controller.CollectionView;
				Assert.NotNull(nativeCollectionView.Superview);
				Assert.NotNull(nativeCollectionView.Window);
				Assert.NotNull(nativeCollectionView.CollectionViewLayout);

				nativeHeight = nativeCollectionView.Frame.Height;
				Assert.NotEqual(-1d, nativeHeight);

				Assert.True(
					Math.Abs(nativeHeight) <= 1d,
					$"Issue35889: Empty CollectionView native height must be 0 +/- 1; observed native height {nativeHeight}.");
			});
		}
	}
}
#endif

