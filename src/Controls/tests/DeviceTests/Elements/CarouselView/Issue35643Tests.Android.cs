#if ANDROID
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CarouselView)]
	[Category("Issue35643")]
	public class Issue35643 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CurrentItemRemainsReplacementAfterReplacingSelectedLastItem()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CarouselView, CarouselViewHandler>();
				});
			});

			var viewModel = new Issue35643ViewModel
			{
				Items = new ObservableCollection<string> { "0", "1", "2" },
				CurrentItem = "2"
			};
			var carouselView = new CarouselView
			{
				Loop = false,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label
					{
						FontSize = 48,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					};
					itemLabel.SetBinding(Label.TextProperty, ".");
					return new Grid { itemLabel };
				})
			};
			carouselView.SetBinding(ItemsView.ItemsSourceProperty, new Binding(nameof(Issue35643ViewModel.Items)));
			carouselView.SetBinding(CarouselView.CurrentItemProperty, new Binding(nameof(Issue35643ViewModel.CurrentItem), mode: BindingMode.TwoWay));

			var headingLabel = new Label
			{
				Text = "CarouselView CurrentItem replacement",
				FontSize = 20
			};
			var currentItemLabel = new Label();
			currentItemLabel.SetBinding(Label.TextProperty, new Binding(nameof(Issue35643ViewModel.CurrentItem), stringFormat: "Current item: {0}"));

			var replaceButton = new Button { Text = "Replace item 2" };
			var checkButton = new Button { Text = "Check current item" };
			bool clicked = false;
			replaceButton.Clicked += (_, _) =>
			{
				clicked = true;
				viewModel.Items[2] = "2b";
				viewModel.CurrentItem = "2b";
			};

			var buttonLayout = new HorizontalStackLayout
			{
				Spacing = 12,
				Children = { replaceButton, checkButton }
			};
			var expectationLabel = new Label
			{
				Text = "Expected current item: 2b",
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
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			rootGrid.Add(headingLabel);
			rootGrid.Add(carouselView, row: 1);
			rootGrid.Add(currentItemLabel, row: 2);
			rootGrid.Add(buttonLayout, row: 3);
			rootGrid.Add(expectationLabel, row: 4);

			var page = new ContentPage
			{
				Content = rootGrid,
				BindingContext = viewModel
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var carouselHandler = carouselView.Handler as CarouselViewHandler;
				var buttonHandler = replaceButton.Handler as ButtonHandler;
				Assert.NotNull(carouselHandler);
				Assert.NotNull(buttonHandler);

				var recyclerView = carouselHandler.PlatformView;
				var platformButton = buttonHandler.PlatformView as AppCompatButton;
				Assert.NotNull(recyclerView);
				Assert.NotNull(platformButton);
				var adapter = recyclerView.GetAdapter();
				Assert.NotNull(adapter);

				await recyclerView.WaitForLayoutOrNonZeroSize();
				await AssertEventually(() => GetSelectedPosition(recyclerView) == 2);
				Assert.Equal("2", viewModel.CurrentItem);
				Assert.Equal("2", carouselView.CurrentItem);
				Assert.Equal(3, adapter.ItemCount);
				Assert.Equal(2, GetSelectedPosition(recyclerView));

				int changedIndex = -1;
				var replacement = new TaskCompletionSource<int>();
				using var observer = new ReplacementObserver
				{
					OnReplacement = position =>
					{
						changedIndex = position;
						replacement.TrySetResult(position);
					}
				};
				adapter.RegisterAdapterDataObserver(observer);
				platformButton.PerformClick();

				Assert.True(clicked, "The attached native Replace button should invoke its MAUI Clicked callback");
				int replacementIndex = await replacement.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.Equal(2, replacementIndex);
				Assert.Equal(2, changedIndex);
				adapter.UnregisterAdapterDataObserver(observer);

				await WaitForNativeIdle(recyclerView);
				await WaitForNativeIdle(recyclerView);

				Assert.Equal(3, viewModel.Items.Count);
				Assert.Equal("2b", viewModel.Items[2]);

				int selectedPosition = GetSelectedPosition(recyclerView);
				Assert.True(
					viewModel.CurrentItem == "2b" &&
					Equals(carouselView.CurrentItem, "2b") &&
					selectedPosition == 2,
					$"CarouselView CurrentItem should remain the replacement item after replacing the selected last item. " +
					$"Expected item: 2b; model CurrentItem: {viewModel.CurrentItem}; " +
					$"CarouselView CurrentItem: {carouselView.CurrentItem}; expected position: 2; actual position: {selectedPosition}.");
			});
		}

		static int GetSelectedPosition(RecyclerView recyclerView)
		{
			var layoutManager = recyclerView.GetLayoutManager() as LinearLayoutManager;
			Assert.NotNull(layoutManager);
			return layoutManager.FindFirstCompletelyVisibleItemPosition();
		}

		static Task WaitForNativeIdle(RecyclerView recyclerView)
		{
			var completion = new TaskCompletionSource<bool>();
			recyclerView.Post(() => completion.SetResult(true));
			return completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
		}

		sealed class ReplacementObserver : RecyclerView.AdapterDataObserver
		{
			public Action<int> OnReplacement { get; set; }

			public override void OnItemRangeChanged(int positionStart, int itemCount)
			{
				base.OnItemRangeChanged(positionStart, itemCount);
				OnReplacement(positionStart);
			}
		}

		sealed class Issue35643ViewModel : INotifyPropertyChanged
		{
			string _currentItem;

			public ObservableCollection<string> Items { get; set; }

			public string CurrentItem
			{
				get => _currentItem;
				set
				{
					if (_currentItem == value)
						return;

					_currentItem = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentItem)));
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;
		}
	}
}
#endif

