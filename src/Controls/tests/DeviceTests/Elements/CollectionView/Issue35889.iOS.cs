using System;
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
#if IOS && !MACCATALYST
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue35889")]
	public class Issue35889 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyCollectionViewInAutoRowHasZeroNativeHeight()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(26))
				return;

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

			var baselineBeforeLabel = new Label { Text = "before collectionview" };
			var baselineView = new Grid { BackgroundColor = Colors.Red };
			var baselineAfterLabel = new Label { Text = "after collectionview" };
			var baselineGrid = new Grid();
			baselineGrid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			baselineGrid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			baselineGrid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			baselineGrid.Add(baselineBeforeLabel);
			baselineGrid.Add(baselineView);
			baselineGrid.Add(baselineAfterLabel);
			Grid.SetRow(baselineView, 1);
			Grid.SetRow(baselineAfterLabel, 2);

			double baselineHeight = -1;
			await CreateHandlerAndAddToWindow(new ContentPage { Content = baselineGrid }, () =>
			{
				var baselinePlatformView = baselineView.ToPlatform();
				Assert.NotNull(baselinePlatformView.Superview);
				Assert.NotNull(baselinePlatformView.Window);
				Assert.Equal(1, Grid.GetRow(baselineView));
				baselineHeight = baselinePlatformView.Frame.Height;
				Assert.True(baselineHeight <= 0.5,
					$"Empty Grid calibration height should be zero; observed={baselineHeight:F2}");
			});

			var beforeLabel = new Label { Text = "before collectionview" };
			var collectionView = new CollectionView
			{
				BackgroundColor = Colors.Red,
				VerticalOptions = LayoutOptions.Start,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				ItemTemplate = new DataTemplate(() => new Label { Text = "Hello World" })
			};
			var afterLabel = new Label { Text = "after collectionview" };
			var rootGrid = new Grid();
			rootGrid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			rootGrid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			rootGrid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			rootGrid.Add(beforeLabel);
			rootGrid.Add(collectionView);
			rootGrid.Add(afterLabel);
			Grid.SetRow(collectionView, 1);
			Grid.SetRow(afterLabel, 2);

			var page = new ContentPage { Content = rootGrid };
			double layoutWidthAfterSizeChanged = -1;
			rootGrid.SizeChanged += (_, _) => layoutWidthAfterSizeChanged = rootGrid.Width;

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(
					() => layoutWidthAfterSizeChanged > 0,
					message: "The attached Grid did not report its initial layout.");
				Assert.True(layoutWidthAfterSizeChanged > 0);

				Assert.Same(rootGrid, page.Content);
				Assert.Equal(0, Grid.GetRow(beforeLabel));
				Assert.Equal(1, Grid.GetRow(collectionView));
				Assert.Equal(2, Grid.GetRow(afterLabel));
				Assert.Equal("before collectionview", beforeLabel.Text);
				Assert.Equal("after collectionview", afterLabel.Text);
				Assert.Null(collectionView.ItemsSource);
				Assert.Equal(Colors.Red, collectionView.BackgroundColor);
				Assert.Equal(LayoutOptions.Start, collectionView.VerticalOptions);
				var linearLayout = Assert.IsType<LinearItemsLayout>(collectionView.ItemsLayout);
				Assert.Equal(ItemsLayoutOrientation.Vertical, linearLayout.Orientation);
				Assert.NotNull(collectionView.ItemTemplate);
				var templateLabel = Assert.IsType<Label>(collectionView.ItemTemplate.CreateContent());
				Assert.Equal("Hello World", templateLabel.Text);
				Assert.True(page.Width > 0);

				var handler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
				UICollectionView nativeCollectionView = handler.Controller.CollectionView;
				Assert.NotNull(nativeCollectionView);
				Assert.NotNull(nativeCollectionView.Superview);
				Assert.NotNull(nativeCollectionView.Window);
				Assert.False(nativeCollectionView.Hidden);

				double nativeHeight = nativeCollectionView.Frame.Height;
				Assert.True(nativeHeight <= 0.5,
					$"Empty CollectionView native height should be zero after initial iOS layout; observed={nativeHeight:F2}, expected=0.00 +/- 0.50, baseline={baselineHeight:F2}");
			});
		}
	}
#endif
}

