#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30860")]
	public class Issue30860 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CurrentItemAssignedDuringLoadedRaisesPositionChanged()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CarouselView, CarouselViewHandler>();
					handlers.AddHandler<IndicatorView, IndicatorViewHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
				});
			});

			await RunScenario();

			async Task RunScenario()
			{
				const int expectedPosition = 20;
				var loadedCompleted = false;
				var currentItemAssignmentStarted = false;
				var positionChangedCount = 0;
				var observedPosition = -1;

				var carouselView = new CarouselView();
				var indicatorView = new IndicatorView
				{
					HorizontalOptions = LayoutOptions.Start
				};
				var indicatorScrollView = new ScrollView
				{
					Orientation = ScrollOrientation.Horizontal,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
					Content = indicatorView
				};
				var currentItemLabel = new Label
				{
					Text = "Current item: pending",
					FontSize = 18
				};
				var introductionLabel = new Label
				{
					Text = "CurrentItem is assigned during Loaded. The indicator should center without dragging.",
					FontSize = 16
				};

				carouselView.ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label
					{
						FontSize = 28,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					};
					itemLabel.SetBinding(Label.TextProperty, "Name");

					return new Grid
					{
						Padding = 16,
						Children = { itemLabel }
					};
				});
				carouselView.IndicatorView = indicatorView;
				carouselView.PositionChanged += (_, args) =>
				{
					if (!currentItemAssignmentStarted)
						return;

					positionChangedCount++;
					observedPosition = args.CurrentPosition;
					var targetX = Math.Max(0, (args.CurrentPosition * 24) - (indicatorScrollView.Width / 2));
					_ = indicatorScrollView.ScrollToAsync(targetX, 0, true);
				};

				var grid = new Grid
				{
					Padding = 20,
					RowSpacing = 12,
					RowDefinitions =
					{
						new RowDefinition { Height = GridLength.Auto },
						new RowDefinition { Height = 180 },
						new RowDefinition { Height = GridLength.Auto },
						new RowDefinition { Height = GridLength.Auto },
						new RowDefinition { Height = GridLength.Auto }
					},
					Children =
					{
						introductionLabel,
						carouselView,
						indicatorScrollView,
						currentItemLabel
					}
				};
				Grid.SetRow(carouselView, 1);
				Grid.SetRow(indicatorScrollView, 2);
				Grid.SetRow(currentItemLabel, 3);

				var page = new ContentPage
				{
					Title = "CarouselView PositionChanged",
					Content = grid
				};

				var items = new List<CarouselItem>();

				page.Loaded += (_, _) =>
				{
					if (loadedCompleted)
						return;

					for (var index = 1; index <= 30; index++)
						items.Add(new CarouselItem($"Carousel item: Item {index}"));

					carouselView.ItemsSource = items;
					currentItemAssignmentStarted = true;
					carouselView.CurrentItem = items[expectedPosition];
					currentItemLabel.Text = "Current item: Item 21";
					loadedCompleted = true;
				};

				await CreateHandlerAndAddToWindow<IWindowHandler>(page, async windowHandler =>
				{
					Assert.True(loadedCompleted);
					Assert.Same(items, carouselView.ItemsSource);
					Assert.Equal(30, items.Count);
					Assert.Same(items[expectedPosition], carouselView.CurrentItem);

					Assert.NotNull(windowHandler.PlatformView);
					Assert.NotNull(page.Handler);
					Assert.NotNull(page.Handler.PlatformView);
					Assert.NotNull(grid.Handler);
					Assert.NotNull(grid.Handler.PlatformView);
					Assert.NotNull(carouselView.Handler);
					Assert.NotNull(carouselView.Handler.PlatformView);
					Assert.NotNull(indicatorView.Handler);
					Assert.NotNull(indicatorView.Handler.PlatformView);
					Assert.NotNull(indicatorScrollView.Handler);
					Assert.NotNull(indicatorScrollView.Handler.PlatformView);
					Assert.NotNull(introductionLabel.Handler);
					Assert.NotNull(introductionLabel.Handler.PlatformView);
					Assert.NotNull(currentItemLabel.Handler);
					Assert.NotNull(currentItemLabel.Handler.PlatformView);

					var carouselHandler = Assert.IsType<CarouselViewHandler>(carouselView.Handler);
					var recyclerView = carouselHandler.PlatformView;
					Assert.NotNull(recyclerView);
					await recyclerView.WaitForLayoutOrNonZeroSize();
					Assert.True(recyclerView.Width > 0);
					Assert.True(recyclerView.Height > 0);

					var callbackRaised = await Wait(() => positionChangedCount > 0, timeout: 2000);

					Assert.True(
						callbackRaised && positionChangedCount > 0 && observedPosition == expectedPosition,
						$"PositionChanged was not raised after Loaded assigned CurrentItem Item 21. " +
						$"Callback count: {positionChangedCount}; observed position: {observedPosition}; " +
						$"expected position: {expectedPosition}.");
				});
			}
		}

		sealed record CarouselItem(string Name);
	}
}
#endif

