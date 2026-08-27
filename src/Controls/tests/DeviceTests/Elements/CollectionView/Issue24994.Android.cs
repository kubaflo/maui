using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
#if ANDROID
	[Category("Issue24994")]
	public class Issue24994 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task FirstRenderIsNotAnomalouslySlowerThanRepeatRenders()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var items = new List<CollectionItem>(5000);
			for (int index = 0; index < 5000; index++)
				items.Add(new CollectionItem(index, $"Item {index:0000}", $"Detail {index:0000}"));

			var activeCycle = -1;
			var completedCycle = -1;
			var renderStopwatch = new Stopwatch();
			var renderCompletion = new TaskCompletionSource<RenderResult>(TaskCreationOptions.RunContinuationsAsynchronously);

			var collectionView = new CollectionView
			{
				EmptyView = new Label { Text = "Collection is empty" },
				ItemTemplate = new DataTemplate(() =>
				{
					var numberLabel = new Label();
					numberLabel.SetBinding(Label.TextProperty, nameof(CollectionItem.Number));

					var nameLabel = new Label();
					nameLabel.SetBinding(Label.TextProperty, nameof(CollectionItem.Name));
					Grid.SetColumn(nameLabel, 1);

					var detailLabel = new Label();
					detailLabel.SetBinding(Label.TextProperty, nameof(CollectionItem.Detail));
					Grid.SetColumn(detailLabel, 2);

					var itemGrid = new Grid
					{
						Padding = new Thickness(8),
						ColumnDefinitions =
						{
							new ColumnDefinition { Width = 80 },
							new ColumnDefinition { Width = GridLength.Star },
							new ColumnDefinition { Width = GridLength.Auto },
						},
						Children = { numberLabel, nameLabel, detailLabel },
					};

					itemGrid.Loaded += (_, _) =>
					{
						if (activeCycle < 0 || itemGrid.BindingContext is not CollectionItem { Number: 0 } item)
							return;

						completedCycle = activeCycle;
						activeCycle = -1;
						renderStopwatch.Stop();
						renderCompletion.TrySetResult(new RenderResult(completedCycle, item.Number, renderStopwatch.Elapsed.TotalMilliseconds));
					};

					return itemGrid;
				}),
			};
			Grid.SetRow(collectionView, 4);

			var renderButton = new Button { Text = "Render First" };
			renderButton.Clicked += (_, _) =>
			{
				renderStopwatch = Stopwatch.StartNew();
				collectionView.ItemsSource = items;
			};

			var resetButton = new Button { Text = "Reset CollectionView" };
			resetButton.Clicked += (_, _) => collectionView.ItemsSource = null;

			var buttonLayout = new HorizontalStackLayout
			{
				Spacing = 12,
				Children = { renderButton, resetButton },
			};
			Grid.SetRow(buttonLayout, 1);

			var phaseLabel = new Label { Text = "Ready for first render" };
			Grid.SetRow(phaseLabel, 2);

			var measurementLayout = new VerticalStackLayout
			{
				Children =
				{
					new Label { Text = "No render measurements yet" },
					new Label { Text = "First-render timing", FontAttributes = FontAttributes.Bold },
				},
			};
			Grid.SetRow(measurementLayout, 3);

			var root = new Grid
			{
				Padding = new Thickness(16),
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
				},
				Children =
				{
					new Label
					{
						Text = "CollectionView first-render performance",
						FontSize = 20,
						FontAttributes = FontAttributes.Bold,
					},
					buttonLayout,
					phaseLabel,
					measurementLayout,
					collectionView,
				},
			};

			var page = new ContentPage
			{
				Title = "CollectionView first render",
				Content = root,
			};

			var durations = new double[4];
			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(page), async _ =>
			{
				var collectionHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				int GetAdapterItemCount()
				{
					var currentAdapter = collectionHandler.PlatformView.GetAdapter();
					Assert.NotNull(currentAdapter);
					return currentAdapter.ItemCount;
				}

				Assert.Null(collectionView.ItemsSource);
				Assert.Equal(1, GetAdapterItemCount());

				var nativeRenderButton = Assert.IsType<ButtonHandler>(renderButton.Handler).PlatformView;
				var nativeResetButton = Assert.IsType<ButtonHandler>(resetButton.Handler).PlatformView;

				for (int cycle = 0; cycle < durations.Length; cycle++)
				{
					completedCycle = -1;
					activeCycle = cycle;
					renderCompletion = new TaskCompletionSource<RenderResult>(TaskCreationOptions.RunContinuationsAsynchronously);

					await InvokeOnMainThreadAsync(nativeRenderButton.PerformClick);
					var result = await renderCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));

					Assert.Equal(cycle, completedCycle);
					Assert.Equal(cycle, result.Cycle);
					Assert.Equal(0, result.ItemNumber);
					await AssertHelpers.AssertEventually(
						() => InvokeOnMainThreadAsync(() => GetAdapterItemCount() == 5000),
						timeout: 2000);
					Assert.Equal(5000, await InvokeOnMainThreadAsync(GetAdapterItemCount));
					durations[cycle] = result.ElapsedMilliseconds;

					await InvokeOnMainThreadAsync(nativeResetButton.PerformClick);
					await AssertHelpers.AssertEventually(
						() => InvokeOnMainThreadAsync(() => GetAdapterItemCount() == 1),
						timeout: 2000);
					Assert.Null(collectionView.ItemsSource);
					Assert.Equal(1, await InvokeOnMainThreadAsync(GetAdapterItemCount));
				}
			});

			var fastestRepeatMilliseconds = Math.Min(durations[1], Math.Min(durations[2], durations[3]));
			Assert.True(
				durations[0] <= fastestRepeatMilliseconds,
				"Expected first render to be no slower than the fastest repeat render");
		}

		sealed record CollectionItem(int Number, string Name, string Detail);

		readonly record struct RenderResult(int Cycle, int ItemNumber, double ElapsedMilliseconds);
	}
#endif
}

