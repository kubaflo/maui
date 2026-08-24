#if IOS && !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue35889")]
	public class Issue35889 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewInAutoRowHasZeroNativeHeight()
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

			const double tolerance = 0.5;
			bool calibrationCallbackSeen = false;
			double calibrationHeight = -1;

			var calibrationBeforeLabel = new Label { Text = "before collectionview" };
			var calibrationCollectionView = new CollectionView
			{
				HeightRequest = 0,
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Colors.Red,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				ItemTemplate = new DataTemplate(() => new Label { Text = "Hello World" })
			};
			var calibrationAfterLabel = new Label { Text = "after collectionview" };
			var calibrationGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			calibrationGrid.Add(calibrationBeforeLabel);
			calibrationGrid.Add(calibrationCollectionView, 0, 1);
			calibrationGrid.Add(calibrationAfterLabel, 0, 2);
			calibrationCollectionView.SizeChanged += (_, _) => calibrationCallbackSeen = true;

			await CreateHandlerAndAddToWindow<IWindowHandler>(
				new ContentPage { Content = calibrationGrid },
				async _ =>
				{
					var calibrationHandler = Assert.IsType<CollectionViewHandler2>(calibrationCollectionView.Handler);
					var nativeCalibrationView = calibrationHandler.Controller.CollectionView;

					await AssertionExtensions.AssertEventually(
						() => calibrationCallbackSeen &&
							nativeCalibrationView.Window is not null &&
							nativeCalibrationView.Frame.Width > 0);
					calibrationHeight = nativeCalibrationView.Frame.Height;
					Assert.True(calibrationCallbackSeen);
					Assert.NotNull(nativeCalibrationView.Window);
					Assert.True(nativeCalibrationView.Frame.Width > 0);
					Assert.InRange(calibrationHeight, 0, tolerance);
				});

			Assert.InRange(calibrationHeight, 0, tolerance);

			bool layoutCallbackSeen = false;
			double observedHeight = -1;

			var beforeLabel = new Label { Text = "before collectionview" };
			var collectionView = new CollectionView
			{
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Colors.Red,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				ItemTemplate = new DataTemplate(() => new Label { Text = "Hello World" })
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
			grid.Add(collectionView, 0, 1);
			grid.Add(afterLabel, 0, 2);
			collectionView.SizeChanged += (_, _) => layoutCallbackSeen = true;

			await CreateHandlerAndAddToWindow<IWindowHandler>(
				new ContentPage { Content = grid },
				async _ =>
				{
					var collectionHandler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
					var nativeCollectionView = collectionHandler.Controller.CollectionView;
					var nativeBeforeLabel = Assert.IsAssignableFrom<UILabel>(beforeLabel.Handler.PlatformView);
					var nativeAfterLabel = Assert.IsAssignableFrom<UILabel>(afterLabel.Handler.PlatformView);
					var itemsLayout = Assert.IsType<LinearItemsLayout>(collectionView.ItemsLayout);

					await AssertionExtensions.AssertEventually(
						() => layoutCallbackSeen &&
							nativeCollectionView.Window is not null &&
							nativeCollectionView.Frame.Width > 0 &&
							nativeBeforeLabel.Window is not null &&
							nativeAfterLabel.Window is not null);
					observedHeight = nativeCollectionView.Frame.Height;
					Assert.True(layoutCallbackSeen);
					Assert.NotNull(nativeCollectionView.Window);
					Assert.NotNull(nativeBeforeLabel.Window);
					Assert.NotNull(nativeAfterLabel.Window);
					Assert.True(nativeCollectionView.Frame.Width > 0);
					Assert.Equal(0, (int)nativeCollectionView.NumberOfSections());
					Assert.Equal("before collectionview", beforeLabel.Text);
					Assert.Equal("after collectionview", afterLabel.Text);
					Assert.Equal(0, Grid.GetRow(beforeLabel));
					Assert.Equal(1, Grid.GetRow(collectionView));
					Assert.Equal(2, Grid.GetRow(afterLabel));
					Assert.Null(collectionView.ItemsSource);
					Assert.NotNull(collectionView.ItemTemplate);
					Assert.Equal(ItemsLayoutOrientation.Vertical, itemsLayout.Orientation);
					Assert.Equal(LayoutOptions.Start, collectionView.VerticalOptions);
					Assert.Equal(Colors.Red, collectionView.BackgroundColor);
				});

			Assert.True(
				observedHeight >= 0 && observedHeight <= tolerance,
				$"Empty iOS CollectionView native height should be 0; observed {observedHeight}, expected 0 +/- {tolerance}.");
		}
	}
}
#endif

