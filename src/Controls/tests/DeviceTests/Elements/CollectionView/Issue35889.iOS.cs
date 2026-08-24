using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category(TestCategory.CollectionView)]
	[Category("Issue35889")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35889 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewInAutoRowHasZeroNativeHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				});
			});

			var itemTemplate = new DataTemplate(() => new Label { Text = "Hello World" });
			var itemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
			var beforeLabel = new Label { Text = "before collectionview" };
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
				RowDefinitions =
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
			var loadedCount = 0;
			page.Loaded += (_, _) => loadedCount++;
			double observedHeight = -1;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(
					() => loadedCount == 1,
					message: "The reported page did not complete its first window attachment.");
				await AssertEventually(
					() => grid.Handler?.PlatformView is UIView nativeGrid &&
						nativeGrid.Window is not null &&
						nativeGrid.Bounds.Width > 0 &&
						nativeGrid.Bounds.Height > 0,
					message: "The reported Grid was not attached and laid out.");

				Assert.Equal(3, grid.RowDefinitions.Count);
				Assert.All(grid.RowDefinitions, row => Assert.Equal(GridLength.Auto, row.Height));
				Assert.Equal(3, grid.Children.Count);
				Assert.Same(beforeLabel, grid.Children[0]);
				Assert.Same(emptyCollectionView, grid.Children[1]);
				Assert.Same(afterLabel, grid.Children[2]);
				Assert.Equal(0, Grid.GetRow(beforeLabel));
				Assert.Equal(1, Grid.GetRow(emptyCollectionView));
				Assert.Equal(2, Grid.GetRow(afterLabel));
				Assert.Equal("before collectionview", beforeLabel.Text);
				Assert.Equal("after collectionview", afterLabel.Text);
				Assert.Equal(LayoutOptions.Start, emptyCollectionView.VerticalOptions);
				Assert.Equal(Colors.Red, emptyCollectionView.BackgroundColor);
				Assert.Null(emptyCollectionView.ItemsSource);
				Assert.Same(itemsLayout, emptyCollectionView.ItemsLayout);
				Assert.Equal(ItemsLayoutOrientation.Vertical, itemsLayout.Orientation);
				Assert.Same(itemTemplate, emptyCollectionView.ItemTemplate);
				Assert.IsType<PageHandler>(page.Handler);
				Assert.IsType<LayoutHandler>(grid.Handler);
				Assert.IsType<LabelHandler>(beforeLabel.Handler);
				Assert.IsType<LabelHandler>(afterLabel.Handler);
				var collectionViewHandler = Assert.IsType<CollectionViewHandler2>(emptyCollectionView.Handler);
				var nativeCollectionView = collectionViewHandler.Controller.CollectionView;
				Assert.Same(emptyCollectionView, collectionViewHandler.VirtualView);
				Assert.Same(collectionViewHandler.Controller.View, collectionViewHandler.PlatformView);
				Assert.NotNull(nativeCollectionView.Window);
				Assert.NotNull(collectionViewHandler.PlatformView.Window);

				observedHeight = collectionViewHandler.PlatformView.Frame.Height;
			});

			Assert.NotEqual(-1, observedHeight);
			Assert.True(
				Math.Abs(observedHeight) <= 0.5,
				$"Empty CollectionView native height must be 0 after initial iOS layout; observed {observedHeight} points, expected 0 points.");
		}
	}
#endif
}

