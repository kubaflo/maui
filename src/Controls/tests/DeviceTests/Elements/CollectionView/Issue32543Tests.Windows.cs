#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue32543")]
	public class Issue32543 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task HorizontalItemsHonorVerticalOptions()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			const double itemHeight = 25;
			const double tolerance = 4;

			Label startItem = null;
			Label centerItem = null;
			Label endItem = null;
			int layoutUpdates = -1;

			var startCollection = CreateCollection(
				"Item VerticalOptions=\"Start\"",
				LayoutOptions.Start,
				item => startItem = item);
			var centerCollection = CreateCollection(
				"Item VerticalOptions=\"Center\"",
				LayoutOptions.Center,
				item => centerItem = item);
			var endCollection = CreateCollection(
				"Item VerticalOptions=\"End\"",
				LayoutOptions.End,
				item => endItem = item);

			var collectionsGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
				},
			};
			collectionsGrid.Add(startCollection);
			collectionsGrid.Add(centerCollection);
			collectionsGrid.Add(endCollection);
			Grid.SetColumn(centerCollection, 1);
			Grid.SetColumn(endCollection, 2);

			var rootGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
				},
			};
			var topBox = new BoxView { BackgroundColor = Colors.Black };
			var bottomBox = new BoxView { BackgroundColor = Colors.Black };
			rootGrid.Add(topBox);
			rootGrid.Add(collectionsGrid);
			rootGrid.Add(bottomBox);
			Grid.SetRow(collectionsGrid, 1);
			Grid.SetRow(bottomBox, 2);

			var page = new ContentPage { Content = rootGrid };
			rootGrid.SizeChanged += (_, _) => layoutUpdates++;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
			{
				await AssertEventually(() =>
					layoutUpdates >= 0
					&& IsReady(startItem, startCollection)
					&& IsReady(centerItem, centerCollection)
					&& IsReady(endItem, endCollection));

				Assert.True(layoutUpdates >= 0, "A layout callback must occur after attachment.");
				Assert.Equal("Item VerticalOptions=\"Start\"", startItem.Text);
				Assert.Equal("Item VerticalOptions=\"Center\"", centerItem.Text);
				Assert.Equal("Item VerticalOptions=\"End\"", endItem.Text);

				var startCollectionBounds = startCollection.GetBoundingBox();
				var centerCollectionBounds = centerCollection.GetBoundingBox();
				var endCollectionBounds = endCollection.GetBoundingBox();
				var startItemBounds = startItem.GetBoundingBox();
				var centerItemBounds = centerItem.GetBoundingBox();
				var endItemBounds = endItem.GetBoundingBox();

				Assert.InRange(startItemBounds.Height, itemHeight - tolerance, itemHeight + tolerance);
				Assert.InRange(centerItemBounds.Height, itemHeight - tolerance, itemHeight + tolerance);
				Assert.InRange(endItemBounds.Height, itemHeight - tolerance, itemHeight + tolerance);

				double startActual = startItemBounds.Y - startCollectionBounds.Y;
				double centerActual = centerItemBounds.Y - centerCollectionBounds.Y;
				double endActual = endItemBounds.Y - endCollectionBounds.Y;

				double startExpected = 0;
				double centerExpected = (centerCollectionBounds.Height - centerItemBounds.Height) / 2;
				double endExpected = endCollectionBounds.Height - endItemBounds.Height;

				bool correctlyPlaced =
					Math.Abs(startActual - startExpected) <= tolerance
					&& Math.Abs(centerActual - centerExpected) <= tolerance
					&& Math.Abs(endActual - endExpected) <= tolerance;

				Assert.True(
					correctlyPlaced,
					$"Issue32543 vertical placement mismatch: " +
					$"Start actual={startActual:F1}, expected={startExpected:F1}, tolerance={tolerance:F1}; " +
					$"Center actual={centerActual:F1}, expected={centerExpected:F1}, tolerance={tolerance:F1}; " +
					$"End actual={endActual:F1}, expected={endExpected:F1}, tolerance={tolerance:F1}.");
			});
		}

		static CollectionView CreateCollection(string text, LayoutOptions verticalOptions, Action<Label> capture)
		{
			return new CollectionView
			{
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Center,
				SelectionMode = SelectionMode.Single,
				ItemsSource = new[] { text },
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal)
				{
					ItemSpacing = 10,
				},
				ItemTemplate = new DataTemplate(() =>
				{
					var label = CreateLabel(string.Empty, verticalOptions);
					label.SetBinding(Label.TextProperty, ".");
					capture(label);
					return label;
				}),
			};
		}

		static Label CreateLabel(string text, LayoutOptions verticalOptions)
		{
			return new Label
			{
				Text = text,
				BackgroundColor = Colors.Red,
				TextColor = Colors.White,
				HeightRequest = 25,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				VerticalOptions = verticalOptions,
			};
		}

		static bool IsReady(Label item, CollectionView collection)
		{
			return item?.Handler?.PlatformView is not null
				&& collection.Handler?.PlatformView is not null
				&& item.GetBoundingBox().Width > 0
				&& item.GetBoundingBox().Height > 0
				&& collection.GetBoundingBox().Width > 0
				&& collection.GetBoundingBox().Height > 0;
		}
	}
}
#endif

