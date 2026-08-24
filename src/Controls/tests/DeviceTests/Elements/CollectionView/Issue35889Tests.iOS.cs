#if IOS && !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue35889")]
	public class Issue35889 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewInAutoGridRowHasZeroHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddControlsHandlers();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var initialPage = new ContentPage();
			var navigationPage = new NavigationPage(initialPage);
			var window = new Window(navigationPage);

			await CreateHandlerAndAddToWindow<IWindowHandler>(window, async _ =>
			{
				var target = CreateScenarioPage();
				int collectionViewSizeChangedCount = 0;
				double heightAfterSizeChanged = -1;
				target.CollectionView.SizeChanged += (_, _) =>
				{
					collectionViewSizeChangedCount++;
					heightAfterSizeChanged = target.CollectionView.Height;
				};

				await navigationPage.PushAsync(target.Page);

				bool layoutCompleted = await Wait(
					() => collectionViewSizeChangedCount > 0 && heightAfterSizeChanged >= 0,
					timeout: 5000);
				bool nativeViewAttached = await Wait(
					() => target.CollectionView.Handler is CollectionViewHandler2 handler &&
						handler.PlatformView.Window is not null,
					timeout: 5000);

				Assert.True(
					layoutCompleted,
					$"Target CollectionView did not complete layout; SizeChanged count was {collectionViewSizeChangedCount} and recorded height was {heightAfterSizeChanged}.");
				Assert.True(nativeViewAttached, "Target CollectionView did not attach to a native window.");

				Assert.Equal("before collectionview", target.BeforeLabel.Text);
				Assert.Equal("after collectionview", target.AfterLabel.Text);
				Assert.Equal(0, Grid.GetRow(target.BeforeLabel));
				Assert.Equal(1, Grid.GetRow(target.CollectionView));
				Assert.Equal(2, Grid.GetRow(target.AfterLabel));
				Assert.Equal(3, target.Grid.RowDefinitions.Count);
				Assert.All(target.Grid.RowDefinitions, row => Assert.True(row.Height.IsAuto));
				Assert.Null(target.CollectionView.ItemsSource);
				Assert.NotNull(target.CollectionView.ItemTemplate);
				var itemsLayout = Assert.IsType<LinearItemsLayout>(target.CollectionView.ItemsLayout);
				Assert.Equal(ItemsLayoutOrientation.Vertical, itemsLayout.Orientation);
				Assert.Equal(LayoutOptions.Start, target.CollectionView.VerticalOptions);
				Assert.Equal(Colors.Red, target.CollectionView.BackgroundColor);
				Assert.Null(target.CollectionView.Style);
				Assert.Equal(-1, target.CollectionView.HeightRequest);

				var targetHandler = Assert.IsType<CollectionViewHandler2>(target.CollectionView.Handler);
				var targetPlatformView = targetHandler.PlatformView;
				Assert.NotNull(targetPlatformView);
				Assert.Same(target.CollectionView, targetHandler.VirtualView);
				Assert.Same(target.CollectionView.ToPlatform(MauiContext), targetPlatformView);
				Assert.NotNull(targetPlatformView.Window);
				Assert.Equal(Colors.Red.ToPlatform(), targetPlatformView.BackgroundColor);

				double actualHeight = targetPlatformView.Frame.Height;
				Assert.True(
					actualHeight >= -0.5d && actualHeight <= 0.5d,
					$"Empty CollectionView native height should be within 0.5 of zero, but was {actualHeight}.");
			});
		}

		static (ContentPage Page, Grid Grid, Label BeforeLabel, CollectionView CollectionView, Label AfterLabel) CreateScenarioPage()
		{
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
			grid.Add(collectionView, row: 1);
			grid.Add(afterLabel, row: 2);

			return (new ContentPage { Content = grid }, grid, beforeLabel, collectionView, afterLabel);
		}
	}
}
#endif

