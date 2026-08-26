#if ANDROID
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
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
	[Category("Issue35643")]
	public class Issue35643 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacingSelectedItemKeepsReplacementCurrent()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CarouselView, CarouselViewHandler>();
				});
			});

			var replacementAction = NotifyCollectionChangedAction.Reset;
			var replacementIndex = -1;
			var postTriggerCurrentItem = "<not-raised>";
			var mutationTriggered = false;

			var scenario = await InvokeOnMainThreadAsync(() =>
			{
				var viewModel = new Issue35643ViewModel();

				viewModel.Items.CollectionChanged += (_, e) =>
				{
					if (mutationTriggered)
					{
						replacementAction = e.Action;
						replacementIndex = e.NewStartingIndex;
					}
				};

				var currentItemLabel = new Label
				{
					Text = "Current item: 2",
					FontSize = 18
				};

				var carouselView = new CarouselView
				{
					Loop = false,
					ItemTemplate = new DataTemplate(() =>
					{
						var itemLabel = new Label
						{
							FontSize = 36,
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center
						};
						itemLabel.SetBinding(Label.TextProperty, ".");

						return new Grid
						{
							BackgroundColor = Colors.LightGray,
							Children = { itemLabel }
						};
					})
				};
				carouselView.SetBinding(ItemsView.ItemsSourceProperty, nameof(Issue35643ViewModel.Items));
				carouselView.SetBinding(
					CarouselView.CurrentItemProperty,
					new Binding(nameof(Issue35643ViewModel.CurrentItem), mode: BindingMode.TwoWay));
				carouselView.CurrentItemChanged += (_, e) =>
				{
					currentItemLabel.Text = $"Current item: {e.CurrentItem}";
					if (mutationTriggered)
						postTriggerCurrentItem = e.CurrentItem as string ?? "<null>";
				};

				var replaceButton = new Button
				{
					Text = "Replace item 2 with 2b"
				};
				replaceButton.Clicked += (_, _) =>
				{
					mutationTriggered = true;
					viewModel.Items[2] = "2b";
					viewModel.CurrentItem = "2b";
				};

				var titleLabel = new Label
				{
					Text = "CarouselView CurrentItem replacement",
					FontSize = 20,
					FontAttributes = FontAttributes.Bold
				};
				var controls = new VerticalStackLayout
				{
					Spacing = 8,
					Children = { currentItemLabel, replaceButton }
				};
				var statusLabel = new Label
				{
					Text = "Replacement pending",
					FontAttributes = FontAttributes.Bold
				};
				var rootGrid = new Grid
				{
					Padding = 24,
					RowSpacing = 16,
					RowDefinitions =
					{
						new RowDefinition(GridLength.Auto),
						new RowDefinition(180),
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Auto)
					},
					Children = { titleLabel, carouselView, controls, statusLabel }
				};
				Grid.SetRow(carouselView, 1);
				Grid.SetRow(controls, 2);
				Grid.SetRow(statusLabel, 3);

				var page = new ContentPage
				{
					Content = rootGrid,
					BindingContext = viewModel
				};

				return (viewModel, carouselView, replaceButton, titleLabel, currentItemLabel, statusLabel, controls, rootGrid, page);
			});

			await CreateHandlerAndAddToWindow<IWindowHandler>(scenario.page, async _ =>
			{
				Assert.NotNull(scenario.page.Handler?.PlatformView);
				Assert.NotNull(scenario.rootGrid.Handler?.PlatformView);
				Assert.NotNull(scenario.titleLabel.Handler?.PlatformView);
				Assert.NotNull(scenario.carouselView.Handler?.PlatformView);
				Assert.NotNull(scenario.controls.Handler?.PlatformView);
				Assert.NotNull(scenario.currentItemLabel.Handler?.PlatformView);
				Assert.NotNull(scenario.replaceButton.Handler?.PlatformView);
				Assert.NotNull(scenario.statusLabel.Handler?.PlatformView);

				var carouselHandler = Assert.IsType<CarouselViewHandler>(scenario.carouselView.Handler);
				var recyclerView = Assert.IsType<MauiCarouselRecyclerView>(carouselHandler.PlatformView);
				await recyclerView.WaitForLayoutOrNonZeroSize();

				Assert.True(recyclerView.IsAttachedToWindow);
				Assert.Equal(new[] { "0", "1", "2" }, scenario.viewModel.Items);
				Assert.Equal("2", scenario.viewModel.CurrentItem);

				var layoutManager = Assert.IsType<LinearLayoutManager>(recyclerView.GetLayoutManager());
				await AssertEventually(
					() => layoutManager.FindFirstCompletelyVisibleItemPosition() == 2,
					message: "CarouselView did not display the initially selected item at native position 2.");
				Assert.Equal(2, layoutManager.FindFirstCompletelyVisibleItemPosition());

				var context = recyclerView.Context;
				Assert.NotNull(context);
				Assert.InRange(context.FromPixels(recyclerView.Height), 178, 182);

				var adapter = recyclerView.GetAdapter();
				Assert.NotNull(adapter);
				using var adapterObserver = new ReplacementObserver();
				adapter.RegisterAdapterDataObserver(adapterObserver);

				try
				{
					var buttonHandler = Assert.IsType<ButtonHandler>(scenario.replaceButton.Handler);
					Assert.NotNull(buttonHandler.PlatformView);
					buttonHandler.PlatformView.PerformClick();

					await adapterObserver.ReplacementProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
					await AssertEventually(
						() => adapter.ItemCount == 3 && !recyclerView.IsComputingLayout && !recyclerView.IsLayoutRequested,
						message: "CarouselView native adapter and layout did not finish processing the replacement.");

					var dispatcherTurn = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					recyclerView.Post(() => dispatcherTurn.TrySetResult(true));
					await dispatcherTurn.Task.WaitAsync(TimeSpan.FromSeconds(5));
				}
				finally
				{
					adapter.UnregisterAdapterDataObserver(adapterObserver);
				}

				Assert.Equal(NotifyCollectionChangedAction.Replace, replacementAction);
				Assert.Equal(2, replacementIndex);
				Assert.Equal(new[] { "0", "1", "2b" }, scenario.viewModel.Items);
				Assert.NotEqual("<not-raised>", postTriggerCurrentItem);
				var actualCurrentItem = scenario.carouselView.CurrentItem as string ?? "<null>";
				Assert.True(
					actualCurrentItem == "2b",
					$"CarouselView CurrentItem after replacing selected item was '{actualCurrentItem}'; expected '2b'.");
			});
		}

		sealed class ReplacementObserver : RecyclerView.AdapterDataObserver
		{
			public TaskCompletionSource<bool> ReplacementProcessed { get; } =
				new(TaskCreationOptions.RunContinuationsAsynchronously);

			public override void OnItemRangeChanged(int positionStart, int itemCount)
			{
				base.OnItemRangeChanged(positionStart, itemCount);
				if (positionStart == 2 && itemCount == 1)
					ReplacementProcessed.TrySetResult(true);
			}
		}

		sealed class Issue35643ViewModel : INotifyPropertyChanged
		{
			string _currentItem = "2";

			public ObservableCollection<string> Items { get; } = new() { "0", "1", "2" };

			public string CurrentItem
			{
				get => _currentItem;
				set
				{
					if (_currentItem == value)
						return;

					_currentItem = value;
					OnPropertyChanged();
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
#endif

