#if IOS && !MACCATALYST
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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
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
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				});
			});

			var referenceLabel = new Label
			{
				HeightRequest = 24,
				VerticalOptions = LayoutOptions.Start
			};
			await CreateHandlerAndAddToWindow<LabelHandler>(referenceLabel, async handler =>
			{
				var nativeLabel = handler.PlatformView;
				await AssertEventually(
					() => nativeLabel.Window is not null,
					timeout: 5000,
					message: "Reference Label did not attach to the native window.");

				var referenceHeight = (double)nativeLabel.Frame.Height;
				Assert.True(
					Math.Abs(referenceHeight - 24) <= 0.5,
					$"Native frame oracle should preserve a 24-point HeightRequest; observed native height={referenceHeight:F1}");
			});

			var beforeLabel = new Label { Text = "before collectionview" };
			var collectionView = new CollectionView
			{
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Colors.Red,
				ItemsSource = null,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
				ItemTemplate = new DataTemplate(() => new Label { Text = "Hello World" })
			};
			var afterLabel = new Label { Text = "after collectionview" };
			var grid = new Grid();
			grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
			grid.Add(beforeLabel, 0, 0);
			grid.Add(collectionView, 0, 1);
			grid.Add(afterLabel, 0, 2);

			var page = new ContentPage { Content = grid };
			var layoutObserved = false;
			var nativeHeight = -1d;

			void OnCollectionViewSizeChanged(object sender, EventArgs args)
			{
				if (collectionView.Handler is CollectionViewHandler2 collectionHandler)
				{
					var nativeCollection = collectionHandler.Controller.CollectionView;
					nativeCollection.BeginInvokeOnMainThread(() =>
					{
						if (nativeCollection.Window is not null)
						{
							nativeHeight = (double)nativeCollection.Frame.Height;
							layoutObserved = true;
						}
					});
				}
			}

			collectionView.SizeChanged += OnCollectionViewSizeChanged;
			try
			{
				await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
				{
					var collectionHandler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
					var nativeCollection = collectionHandler.Controller.CollectionView;

					await AssertEventually(
						() => nativeCollection.Window is not null,
						timeout: 5000,
						message: "CollectionView did not attach to the native window.");
					await AssertEventually(
						() => layoutObserved,
						timeout: 5000,
						message: "No post-attachment page layout callback was observed.");

					Assert.Equal("before collectionview", beforeLabel.Text);
					Assert.Equal("after collectionview", afterLabel.Text);
					Assert.Equal(0, Grid.GetRow(beforeLabel));
					Assert.Equal(1, Grid.GetRow(collectionView));
					Assert.Equal(2, Grid.GetRow(afterLabel));
					Assert.Equal(LayoutOptions.Start, collectionView.VerticalOptions);
					Assert.Equal(Colors.Red, collectionView.BackgroundColor);
					Assert.Null(collectionView.ItemsSource);

					var itemsLayout = Assert.IsType<LinearItemsLayout>(collectionView.ItemsLayout);
					Assert.Equal(ItemsLayoutOrientation.Vertical, itemsLayout.Orientation);
					var templateLabel = Assert.IsType<Label>(collectionView.ItemTemplate.CreateContent());
					Assert.Equal("Hello World", templateLabel.Text);

					var nativeItemCount = 0;
					for (nint section = 0; section < nativeCollection.NumberOfSections(); section++)
					{
						nativeItemCount += (int)nativeCollection.NumberOfItemsInSection(section);
					}
					Assert.Equal(0, nativeItemCount);

					var beforeNative = Assert.IsAssignableFrom<UIView>(beforeLabel.Handler.PlatformView);
					var afterNative = Assert.IsAssignableFrom<UIView>(afterLabel.Handler.PlatformView);
					var nativeWindow = nativeCollection.Window;
					Assert.True(nativeWindow.Bounds.Width > 0);
					Assert.True(nativeWindow.Bounds.Height > 0);

					var collectionBounds = nativeCollection.ConvertRectToView(nativeCollection.Bounds, nativeWindow);
					Assert.Same(nativeWindow, beforeNative.Window);
					Assert.Same(nativeWindow, afterNative.Window);
					Assert.True(double.IsFinite((double)collectionBounds.X));
					Assert.True(double.IsFinite((double)collectionBounds.Y));
					Assert.True(collectionBounds.Y >= nativeWindow.Bounds.Y);
					Assert.True(collectionBounds.Y <= nativeWindow.Bounds.Y + nativeWindow.Bounds.Height);

					Assert.True(
						Math.Abs(nativeHeight) <= 0.5,
						$"Empty CollectionView native height should be 0; observed native height={nativeHeight:F1}");
				});
			}
			finally
			{
				collectionView.SizeChanged -= OnCollectionViewSizeChanged;
			}
		}
	}
}
#endif

