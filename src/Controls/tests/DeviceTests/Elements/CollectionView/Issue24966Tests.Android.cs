#if ANDROID
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AButton = Android.Widget.Button;
using ATextView = Android.Widget.TextView;
using AView = Android.Views.View;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue24966")]
	public class Issue24966 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task EmptyViewKeepsIntrinsicHeightAfterClearingItems()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<HorizontalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var items = new ObservableCollection<string> { "One item" };
			var resultLabel = new Label { Text = "PASS:" };
			var readyLabel = new Label { Text = "Preparing" };
			var clearButton = new Button { Text = "Clear items", IsEnabled = false };
			var checkButton = new Button { Text = "Check footer position", IsEnabled = false };
			var instructions = new Label { Text = "The footer should stay directly after the empty view." };
			var headerLabel = new Label { Text = "Collection header" };
			var emptyLabel = new Label { Text = "Empty view" };
			var footerLabel = new Label { Text = "Collection footer" };
			Label itemLabel = null;
			var initialItemSizeChanged = false;
			var emptySizeChanged = false;

			var collectionView = new CollectionView
			{
				Header = headerLabel,
				EmptyView = emptyLabel,
				Footer = footerLabel,
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					itemLabel = new Label();
					itemLabel.SetBinding(Label.TextProperty, ".");
					itemLabel.SizeChanged += (sender, args) =>
					{
						if (initialItemSizeChanged || itemLabel.Height <= 0)
							return;

						initialItemSizeChanged = true;
						readyLabel.Text = "Ready";
						clearButton.IsEnabled = true;
					};
					return itemLabel;
				})
			};

			clearButton.Clicked += (sender, args) =>
			{
				items.Clear();
				clearButton.IsEnabled = false;
				checkButton.IsEnabled = true;
				readyLabel.Text = "Empty state visible";
			};

			var buttonRow = new HorizontalStackLayout
			{
				Children =
				{
					clearButton,
					checkButton
				}
			};
			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			grid.Add(resultLabel);
			grid.Add(readyLabel);
			grid.Add(buttonRow);
			grid.Add(instructions);
			grid.Add(collectionView);
			Grid.SetRow(readyLabel, 1);
			Grid.SetRow(buttonRow, 2);
			Grid.SetRow(instructions, 3);
			Grid.SetRow(collectionView, 4);

			var page = new ContentPage
			{
				Title = "CollectionView footer",
				Content = grid
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(
					() => initialItemSizeChanged &&
						itemLabel != null &&
						itemLabel.Handler != null &&
						headerLabel.Handler != null &&
						footerLabel.Handler != null &&
						clearButton.Handler != null &&
						itemLabel.ToPlatform().IsShown &&
						headerLabel.ToPlatform().IsShown &&
						footerLabel.ToPlatform().IsShown &&
						clearButton.ToPlatform().IsShown &&
						clearButton.ToPlatform().Enabled,
					timeout: 5000,
					message: "The initial CollectionView content was not laid out.");

				var handler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var recyclerView = handler.PlatformView;
				var initialRecyclerView = recyclerView;
				var nativeItem = Assert.IsAssignableFrom<ATextView>(itemLabel.ToPlatform());
				var nativeHeader = Assert.IsAssignableFrom<ATextView>(headerLabel.ToPlatform());
				var nativeFooter = Assert.IsAssignableFrom<ATextView>(footerLabel.ToPlatform());
				var nativeClearButton = Assert.IsAssignableFrom<AButton>(clearButton.ToPlatform());
				var density = recyclerView.Resources.DisplayMetrics.Density;
				var tolerance = Math.Max(1, (int)Math.Ceiling(2 * density));

				Assert.True(density > 0);
				Assert.Equal("Ready", readyLabel.Text);
				Assert.True(clearButton.IsEnabled);
				Assert.Equal("One item", nativeItem.Text);
				Assert.Equal("Collection header", nativeHeader.Text);
				Assert.Equal("Collection footer", nativeFooter.Text);
				Assert.Equal(1, nativeItem.LineCount);
				Assert.True(nativeItem.TextSize > 0);
				Assert.NotNull(nativeItem.Layout);
				Assert.True(nativeItem.Layout.Height > 0);
				var expectedItemHeight = nativeItem.Layout.Height + nativeItem.CompoundPaddingTop + nativeItem.CompoundPaddingBottom;
				Assert.InRange(Math.Abs(nativeItem.Height - expectedItemHeight), 0, tolerance);

				AssertNativeViewInside(recyclerView, page.ToPlatform());
				AssertNativeViewInside(nativeHeader, recyclerView);
				AssertNativeViewInside(nativeItem, recyclerView);
				AssertNativeViewInside(nativeFooter, recyclerView);

				emptyLabel.SizeChanged += (sender, args) => emptySizeChanged = true;
				emptySizeChanged = false;
				nativeClearButton.PerformClick();

				await AssertEventually(
					() => items.Count == 0 &&
						emptySizeChanged &&
						emptyLabel.Handler != null &&
						footerLabel.Handler != null &&
						emptyLabel.ToPlatform().IsShown &&
						footerLabel.ToPlatform().IsShown &&
						emptyLabel.ToPlatform().Height > 0,
					timeout: 5000,
					message: "The empty CollectionView content was not laid out after clearing its items.");

				Assert.Empty(items);
				Assert.True(emptySizeChanged, "The EmptyView did not receive a post-clear size callback.");
				Assert.Same(handler, collectionView.Handler);
				Assert.Same(initialRecyclerView, handler.PlatformView);
				Assert.True(checkButton.IsEnabled);
				Assert.Equal("Empty state visible", readyLabel.Text);

				var nativeEmpty = Assert.IsAssignableFrom<ATextView>(emptyLabel.ToPlatform());
				nativeFooter = Assert.IsAssignableFrom<ATextView>(footerLabel.ToPlatform());
				Assert.Equal("Empty view", nativeEmpty.Text);
				Assert.Equal("Collection footer", nativeFooter.Text);
				Assert.Equal(1, nativeEmpty.LineCount);
				Assert.True(nativeEmpty.TextSize > 0);
				Assert.NotNull(nativeEmpty.Layout);
				Assert.True(nativeEmpty.Layout.Height > 0);
				AssertNativeViewInside(nativeEmpty, recyclerView);
				AssertNativeViewInside(nativeFooter, recyclerView);

				var expectedEmptyHeight = nativeEmpty.Layout.Height + nativeEmpty.CompoundPaddingTop + nativeEmpty.CompoundPaddingBottom;
				var footerLocation = new int[2];
				nativeFooter.GetLocationOnScreen(footerLocation);
				Assert.True(
					Math.Abs(nativeEmpty.Height - expectedEmptyHeight) <= tolerance,
					$"Issue24966 empty view expanded: measured={nativeEmpty.Height}px, expected={expectedEmptyHeight}px, tolerance={tolerance}px, footer screen Y={footerLocation[1]}px.");
			});

			static void AssertNativeViewInside(AView view, AView container)
			{
				var viewLocation = new int[2];
				var containerLocation = new int[2];
				view.GetLocationOnScreen(viewLocation);
				container.GetLocationOnScreen(containerLocation);

				Assert.True(view.Width > 0 && view.Height > 0);
				Assert.True(container.Width > 0 && container.Height > 0);
				Assert.True(viewLocation[0] >= containerLocation[0]);
				Assert.True(viewLocation[1] >= containerLocation[1]);
				Assert.True(viewLocation[0] + view.Width <= containerLocation[0] + container.Width);
				Assert.True(viewLocation[1] + view.Height <= containerLocation[1] + container.Height);
			}
		}
	}
}
#endif

