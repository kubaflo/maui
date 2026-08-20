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
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue32543")]
	public class Issue32543 : ControlsHandlerTestBase
	{
		const double PlacementTolerance = 2;
		const double ItemHeight = 25;

		[Fact]
		public async Task HorizontalCollectionViewItemsRespectVerticalOptions()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			Label startItem = null;
			Label centerItem = null;
			Label endItem = null;
			int loadedTransitions = -1;

			var startCollection = CreateCollection(
				"Item VerticalOptions=\"Start\"",
				LayoutOptions.Start,
				label =>
				{
					startItem = label;
					loadedTransitions++;
				});
			var centerCollection = CreateCollection(
				"Item VerticalOptions=\"Center\"",
				LayoutOptions.Center,
				label =>
				{
					centerItem = label;
					loadedTransitions++;
				});
			var endCollection = CreateCollection(
				"Item VerticalOptions=\"End\"",
				LayoutOptions.End,
				label =>
				{
					endItem = label;
					loadedTransitions++;
				});

			var collectionRegion = CreateThreeColumnGrid();
			collectionRegion.Add(startCollection, 0);
			collectionRegion.Add(centerCollection, 1);
			collectionRegion.Add(endCollection, 2);

			var rootGrid = CreateThreeRowGrid();
			rootGrid.Add(new BoxView { BackgroundColor = Colors.Black }, 0, 0);
			rootGrid.Add(collectionRegion, 0, 1);
			rootGrid.Add(new BoxView { BackgroundColor = Colors.Black }, 0, 2);

			var page = new ContentPage { Content = rootGrid };
			loadedTransitions = 0;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async handler =>
			{
				await AssertEventually(
					() => loadedTransitions >= 3
						&& startItem is not null
						&& centerItem is not null
						&& endItem is not null
						&& GetPlatformView(startItem).IsLoaded
						&& GetPlatformView(centerItem).IsLoaded
						&& GetPlatformView(endItem).IsLoaded,
					timeout: 5000,
					message: "All three CollectionView item labels should complete their Loaded transition");

				Assert.Equal(3, loadedTransitions);
				Assert.True(GetPlatformView(startItem).IsLoaded);
				Assert.True(GetPlatformView(centerItem).IsLoaded);
				Assert.True(GetPlatformView(endItem).IsLoaded);
				AssertItemIdentity(startCollection, startItem, "Item VerticalOptions=\"Start\"", LayoutOptions.Start);
				AssertItemIdentity(centerCollection, centerItem, "Item VerticalOptions=\"Center\"", LayoutOptions.Center);
				AssertItemIdentity(endCollection, endItem, "Item VerticalOptions=\"End\"", LayoutOptions.End);

				var regionBounds = GetNativeBounds(collectionRegion);
				var startBounds = GetNativeBounds(startItem);
				var centerBounds = GetNativeBounds(centerItem);
				var endBounds = GetNativeBounds(endItem);

				Assert.Equal(ItemHeight, startBounds.Height, PlacementTolerance);
				Assert.Equal(ItemHeight, centerBounds.Height, PlacementTolerance);
				Assert.Equal(ItemHeight, endBounds.Height, PlacementTolerance);

				Assert.InRange(startBounds.CenterX, regionBounds.Left, regionBounds.Left + (regionBounds.Width / 3));
				Assert.InRange(centerBounds.CenterX, regionBounds.Left + (regionBounds.Width / 3), regionBounds.Left + (2 * regionBounds.Width / 3));
				Assert.InRange(endBounds.CenterX, regionBounds.Left + (2 * regionBounds.Width / 3), regionBounds.Right);

				var startTopOffset = startBounds.Top - regionBounds.Top;
				var centerVerticalRatio = (centerBounds.CenterY - regionBounds.Top) / regionBounds.Height;
				var endBottomOffset = endBounds.Bottom - regionBounds.Bottom;

				Assert.True(
					Math.Abs(startTopOffset) <= PlacementTolerance,
					$"Start item native top should match the correct plain-label layout. Expected offset 0.00, actual {startTopOffset:F2}.");
				Assert.True(
					Math.Abs(centerVerticalRatio - 0.5) <= 0.02,
					$"Center item native midpoint should be at the middle of its row. Expected ratio 0.50, actual {centerVerticalRatio:F2}.");
				Assert.True(
					Math.Abs(endBottomOffset) <= PlacementTolerance,
					$"End item native bottom should be at the bottom of its row. Expected offset 0.00, actual {endBottomOffset:F2}.");
			});
		}

		static CollectionView CreateCollection(
			string text,
			LayoutOptions verticalOptions,
			Action<Label> onLoaded)
		{
			return new CollectionView
			{
				BackgroundColor = Colors.Transparent,
				SelectionMode = SelectionMode.Single,
				HorizontalOptions = LayoutOptions.Center,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal)
				{
					ItemSpacing = 10
				},
				ItemsSource = new[] { text },
				ItemTemplate = new DataTemplate(() =>
				{
					var label = CreateLabel(string.Empty, verticalOptions);
					label.SetBinding(Label.TextProperty, new Binding("."));
					label.Loaded += (_, _) => onLoaded(label);
					return label;
				})
			};
		}

		static Label CreateLabel(string text, LayoutOptions verticalOptions) =>
			new Label
			{
				Text = text,
				BackgroundColor = Colors.Red,
				TextColor = Colors.White,
				HeightRequest = ItemHeight,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalOptions = verticalOptions
			};

		static Grid CreateThreeColumnGrid()
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			return grid;
		}

		static Grid CreateThreeRowGrid()
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
			return grid;
		}

		static void AssertItemIdentity(CollectionView collectionView, Label item, string expectedText, LayoutOptions expectedOptions)
		{
			Assert.Single(collectionView.LogicalChildrenInternal);
			Assert.Same(item, collectionView.LogicalChildrenInternal[0]);
			Assert.Equal(expectedText, item.Text);
			Assert.Equal(expectedText, item.BindingContext);
			Assert.Equal(expectedOptions, item.VerticalOptions);
		}

		static WFrameworkElement GetPlatformView(VisualElement element) =>
			(WFrameworkElement)element.ToPlatform();

		static NativeBounds GetNativeBounds(VisualElement element)
		{
			var platformView = GetPlatformView(element);
			var location = platformView.GetLocationOnScreen();
			Assert.True(location.HasValue, $"{element.GetType().Name} should have an in-window native location");
			return new NativeBounds(location.Value.X, location.Value.Y, platformView.ActualWidth, platformView.ActualHeight);
		}

		readonly struct NativeBounds
		{
			public NativeBounds(double left, double top, double width, double height)
			{
				Left = left;
				Top = top;
				Width = width;
				Height = height;
			}

			public double Left { get; }
			public double Top { get; }
			public double Width { get; }
			public double Height { get; }
			public double Right => Left + Width;
			public double Bottom => Top + Height;
			public double CenterX => Left + (Width / 2);
			public double CenterY => Top + (Height / 2);
		}
	}
}
#endif
