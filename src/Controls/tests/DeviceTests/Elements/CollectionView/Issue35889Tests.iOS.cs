using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue35889")]
	public class Issue35889 : ControlsHandlerTestBase
	{
#if IOS && !MACCATALYST
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

			const double tolerance = 0.5;

			var beforeLabel = new Label { Text = "before collectionview" };
			var collectionView = new CollectionView
			{
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Colors.Red,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				ItemTemplate = new DataTemplate(() => new Label { Text = "Hello World" })
			};
			var afterLabel = new Label { Text = "after collectionview" };
			var rootGrid = CreateThreeRowGrid(beforeLabel, collectionView, afterLabel);
			var page = new ContentPage { Content = rootGrid };

			bool layoutCallbackOccurred = false;
			afterLabel.SizeChanged += (_, _) =>
			{
				if (rootGrid.Handler?.PlatformView is UIView nativeGrid &&
					collectionView.Handler?.PlatformView is UIView nativeCollectionView &&
					afterLabel.Handler?.PlatformView is UIView nativeAfterLabel &&
					nativeGrid.Window is not null &&
					nativeGrid.Window == nativeCollectionView.Window &&
					nativeGrid.Window == nativeAfterLabel.Window)
				{
					layoutCallbackOccurred = true;
				}
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async windowHandler =>
			{
				Assert.NotNull(windowHandler.PlatformView);
				await AssertEventually(
					() => layoutCallbackOccurred,
					message: "The trailing label did not receive a post-attachment layout callback");

				var collectionHandler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
				var collectionPlatformView = Assert.IsAssignableFrom<UIView>(collectionHandler.PlatformView);
				var nativeCollectionView = Assert.IsAssignableFrom<UICollectionView>(collectionHandler.Controller.CollectionView);
				var nativeBeforeLabel = Assert.IsAssignableFrom<UIView>(beforeLabel.Handler.PlatformView);
				var nativeAfterLabel = Assert.IsAssignableFrom<UIView>(afterLabel.Handler.PlatformView);

				Assert.Same(collectionView, collectionHandler.VirtualView);
				Assert.Same(collectionHandler.Controller.CollectionView, nativeCollectionView);
				Assert.NotNull(nativeCollectionView.Window);
				Assert.Same(nativeCollectionView.Window, nativeBeforeLabel.Window);
				Assert.Same(nativeCollectionView.Window, nativeAfterLabel.Window);
				Assert.Null(collectionView.ItemsSource);
				Assert.IsType<LinearItemsLayout>(collectionView.ItemsLayout);
				Assert.Equal(ItemsLayoutOrientation.Vertical, ((LinearItemsLayout)collectionView.ItemsLayout).Orientation);
				Assert.NotNull(collectionView.ItemTemplate);
				Assert.Equal(LayoutOptions.Start, collectionView.VerticalOptions);
				Assert.Equal(Colors.Red, collectionView.BackgroundColor);
				Assert.Equal(Colors.Red.ToPlatform(), collectionPlatformView.BackgroundColor);

				var collectionFrame = nativeCollectionView.ConvertRectToView(nativeCollectionView.Bounds, nativeCollectionView.Window);
				var beforeFrame = nativeBeforeLabel.ConvertRectToView(nativeBeforeLabel.Bounds, nativeCollectionView.Window);
				var afterFrame = nativeAfterLabel.ConvertRectToView(nativeAfterLabel.Bounds, nativeCollectionView.Window);
				Assert.True(beforeFrame.Bottom <= collectionFrame.Top + tolerance);
				Assert.True(collectionFrame.Bottom <= afterFrame.Top + tolerance);
				Assert.Equal(0d, collectionFrame.Height);
			});
		}

		static Grid CreateThreeRowGrid(View beforeView, View middleView, View afterView)
		{
			var grid = new Grid();
			grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			grid.Add(beforeView);
			grid.Add(middleView, 0, 1);
			grid.Add(afterView, 0, 2);
			return grid;
		}
#endif
	}
}

