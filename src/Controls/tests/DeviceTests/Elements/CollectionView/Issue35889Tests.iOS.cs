using System;
using System.Threading.Tasks;
using CoreGraphics;
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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue35889")]
	public class Issue35889 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewHasZeroHeightInAutoGridRow()
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
			var collectionView = new CollectionView
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
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				}
			};

			grid.Add(beforeLabel);
			grid.Add(collectionView, 0, 1);
			grid.Add(afterLabel, 0, 2);

			var page = new ContentPage { Content = grid };
			var window = new Window(page);
			var rootLayoutCallbacks = -1;
			var collectionViewLayoutCallbacks = -1;
			var observedCollectionViewHeight = -1d;
			grid.SizeChanged += (_, _) => rootLayoutCallbacks++;
			collectionView.SizeChanged += (_, _) =>
			{
				collectionViewLayoutCallbacks++;
				observedCollectionViewHeight = collectionView.Height;
			};

			Assert.Null(collectionView.ItemsSource);
			Assert.Same(itemsLayout, collectionView.ItemsLayout);
			Assert.Equal(ItemsLayoutOrientation.Vertical, itemsLayout.Orientation);
			Assert.Same(itemTemplate, collectionView.ItemTemplate);
			Assert.Equal(Colors.Red, collectionView.BackgroundColor);
			Assert.Equal(LayoutOptions.Start, collectionView.VerticalOptions);
			Assert.Equal("before collectionview", beforeLabel.Text);
			Assert.Equal("after collectionview", afterLabel.Text);
			Assert.Equal(0, Grid.GetRow(beforeLabel));
			Assert.Equal(1, Grid.GetRow(collectionView));
			Assert.Equal(2, Grid.GetRow(afterLabel));

			await CreateHandlerAndAddToWindow(window, async () =>
			{
				await AssertEventually(
					() => rootLayoutCallbacks >= 0 && collectionViewLayoutCallbacks >= 0,
					message: "The Grid and CollectionView did not both receive an attached layout callback.");

				var nativeGrid = grid.Handler?.PlatformView as UIView;
				var nativeBeforeLabel = beforeLabel.Handler?.PlatformView as UILabel;
				var collectionHandler = collectionView.Handler as CollectionViewHandler2;
				var nativeCollectionView = collectionHandler?.PlatformView;
				var nativeAfterLabel = afterLabel.Handler?.PlatformView as UILabel;

				Assert.NotNull(nativeGrid);
				Assert.NotNull(nativeBeforeLabel);
				Assert.NotNull(collectionHandler);
				Assert.NotNull(nativeCollectionView);
				Assert.NotNull(nativeAfterLabel);
				Assert.Same(collectionView, collectionHandler.VirtualView);
				Assert.Equal("before collectionview", nativeBeforeLabel.Text);
				Assert.Equal("after collectionview", nativeAfterLabel.Text);
				Assert.NotNull(nativeGrid.Window);
				Assert.Same(nativeGrid.Window, nativeBeforeLabel.Window);
				Assert.Same(nativeGrid.Window, nativeCollectionView.Window);
				Assert.Same(nativeGrid.Window, nativeAfterLabel.Window);

				var nativeGridFrame = nativeGrid.ConvertRectToView(nativeGrid.Bounds, nativeGrid.Window);
				var beforeLabelFrame = nativeBeforeLabel.ConvertRectToView(nativeBeforeLabel.Bounds, nativeGrid.Window);
				var collectionViewFrame = nativeCollectionView.ConvertRectToView(nativeCollectionView.Bounds, nativeGrid.Window);
				var afterLabelFrame = nativeAfterLabel.ConvertRectToView(nativeAfterLabel.Bounds, nativeGrid.Window);

				Assert.True(nativeGridFrame.Width > 0 && nativeGridFrame.Height > 0,
					$"The attached Grid must have a positive native frame, but was {nativeGridFrame}.");

				const double tolerance = 0.5;
				var fittingSize = nativeBeforeLabel.SizeThatFits(
					new CGSize(nativeBeforeLabel.Bounds.Width, nfloat.MaxValue));
				Assert.True(
					Math.Abs(beforeLabelFrame.Height - fittingSize.Height) <= tolerance,
					$"The unaffected first Label height {beforeLabelFrame.Height:F2} did not match its UIKit fitting height {fittingSize.Height:F2}.");
				Assert.True(
					collectionViewFrame.X >= nativeGridFrame.X - tolerance &&
					collectionViewFrame.X <= nativeGridFrame.GetMaxX() + tolerance &&
					collectionViewFrame.Y >= beforeLabelFrame.GetMaxY() - tolerance &&
					collectionViewFrame.Y <= nativeGridFrame.GetMaxY() + tolerance,
					$"The empty CollectionView was not in its expected native Grid location: {collectionViewFrame}.");
				Assert.True(
					afterLabelFrame.Y >= collectionViewFrame.GetMaxY() - tolerance,
					$"The label after the CollectionView was not laid out after it: {afterLabelFrame}.");
				var occupiedAutoRowHeight = (double)(afterLabelFrame.Y - beforeLabelFrame.GetMaxY());
				Assert.True(
					Math.Abs(occupiedAutoRowHeight - observedCollectionViewHeight) <= tolerance,
					$"The native Auto Grid row height {occupiedAutoRowHeight:F2} did not match the CollectionView layout height {observedCollectionViewHeight:F2}.");
				const double expectedAutoRowHeight = 0;
				Assert.True(
					Math.Abs(occupiedAutoRowHeight - expectedAutoRowHeight) <= tolerance,
					$"Empty iOS CollectionView Auto Grid row should equal the expected height of {expectedAutoRowHeight:F2} points, but was {occupiedAutoRowHeight:F2}.");
			});
		}
	}
#endif
}

