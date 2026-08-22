using System;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue36422")]
	public class Issue36422 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ChangingItemSpacingKeepsFirstItemVisible()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				});
			});

			var items = new[]
			{
				new { Name = "Baboon", Location = "Africa & Asia" },
				new { Name = "Capuchin Monkey", Location = "Central & South America" },
				new { Name = "Blue Monkey", Location = "Central and East Africa" },
				new { Name = "Squirrel Monkey", Location = "Central & South America" },
				new { Name = "Golden Lion Tamarin", Location = "Brazil" },
				new { Name = "Howler Monkey", Location = "South America" },
				new { Name = "Japanese Macaque", Location = "Japan" },
				new { Name = "Mandrill", Location = "Central Africa" },
				new { Name = "Proboscis Monkey", Location = "Borneo" },
				new { Name = "Red-shanked Douc", Location = "Vietnam, Laos" },
				new { Name = "Gray-shanked Douc", Location = "Vietnam" },
				new { Name = "Golden Snub-nosed Monkey", Location = "China" }
			};

			var collectionView = new CollectionView
			{
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemGrid = new Grid
					{
						Padding = 10,
						RowDefinitions =
						[
							new RowDefinition(GridLength.Auto),
							new RowDefinition(GridLength.Auto)
						],
						ColumnDefinitions =
						[
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Star)
						]
					};

					var image = new Image
					{
						Aspect = Aspect.AspectFill,
						HeightRequest = 60,
						WidthRequest = 60
					};
					Grid.SetRowSpan(image, 2);

					var nameLabel = new Label { FontAttributes = FontAttributes.Bold };
					nameLabel.SetBinding(Label.TextProperty, "Name");
					Grid.SetColumn(nameLabel, 1);

					var locationLabel = new Label
					{
						FontAttributes = FontAttributes.Italic,
						VerticalOptions = LayoutOptions.End
					};
					locationLabel.SetBinding(Label.TextProperty, "Location");
					Grid.SetRow(locationLabel, 1);
					Grid.SetColumn(locationLabel, 1);

					itemGrid.Children.Add(image);
					itemGrid.Children.Add(nameLabel);
					itemGrid.Children.Add(locationLabel);
					return itemGrid;
				})
			};

			var instructions = new StackLayout
			{
				Children =
				{
					new Label { Text = "1. The Monkeys are displayed in a single column list." },
					new Label { Text = "2. Change the vertical spacing and verify that the first item remains visible." }
				}
			};

			var controls = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				HorizontalOptions = LayoutOptions.Center,
				Children =
				{
					new Label { Text = "Spacing:", VerticalTextAlignment = TextAlignment.Center },
					new Entry { Text = "0", WidthRequest = 100 },
					new Button { Text = "Update" },
				}
			};

			var controlsContainer = new ContentView { Content = controls };
			var rootGrid = new Grid
			{
				Margin = 20,
				RowDefinitions =
				[
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				]
			};
			rootGrid.Add(instructions, 0, 0);
			rootGrid.Add(controlsContainer, 0, 1);
			rootGrid.Add(collectionView, 0, 2);

			var page = new ContentPage
			{
				Title = "Vertical list (spacing)",
				Content = rootGrid
			};

			var itemsLayout = Assert.IsType<LinearItemsLayout>(collectionView.ItemsLayout);
			Assert.Equal(0, itemsLayout.ItemSpacing);
			Assert.Equal("Baboon", items[0].Name);

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var handler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
				UICollectionView nativeCollectionView = handler.Controller.CollectionView;
				NSIndexPath firstIndexPath = NSIndexPath.FromItemSection(0, 0);
				NSIndexPath secondIndexPath = NSIndexPath.FromItemSection(1, 0);

				await AssertEventually(
					() =>
					{
						return TryGetRenderedNativeItemLabel(
								nativeCollectionView,
								firstIndexPath,
								out _,
								out _) &&
							items[0].Name == "Baboon";
					},
					timeout: 5000,
					message: "Item 0 'Baboon' was not visible before ItemSpacing changed.");

				double postTriggerGap = -1;
				bool postTriggerLayoutObserved = false;
				itemsLayout.ItemSpacing = 100;

				await AssertEventually(
					() =>
					{
						UICollectionViewLayoutAttributes firstAttributes =
							nativeCollectionView.GetLayoutAttributesForItem(firstIndexPath);
						UICollectionViewLayoutAttributes secondAttributes =
							nativeCollectionView.GetLayoutAttributesForItem(secondIndexPath);
						if (firstAttributes is null || secondAttributes is null)
							return false;

						double measuredGap = secondAttributes.Frame.Top - firstAttributes.Frame.Bottom;
						if (Math.Abs(measuredGap - 100) > 1)
							return false;

						postTriggerGap = measuredGap;
						postTriggerLayoutObserved = true;
						return true;
					},
					timeout: 5000,
					message: "The native layout did not apply ItemSpacing=100.");

				Assert.True(postTriggerLayoutObserved, "The post-trigger native layout probe did not complete.");
				Assert.InRange(postTriggerGap, 99, 101);
				Assert.Equal("Baboon", items[0].Name);

				string visibleIndices = string.Join(
					",",
					nativeCollectionView.IndexPathsForVisibleItems
						.OrderBy(path => path.Section)
						.ThenBy(path => path.Item)
						.Select(path => $"{path.Section}:{path.Item}"));
				bool firstItemIsRendered = TryGetRenderedNativeItemLabel(
						nativeCollectionView,
						firstIndexPath,
						out CGRect nativeLabelFrame,
						out CGRect nativeViewport) &&
					items[0].Name == "Baboon";

				Assert.True(
					firstItemIsRendered,
					$"After ItemSpacing changed from 0 to 100, item 0 'Baboon' must remain visible; observed visible indices: {visibleIndices}; " +
					$"native Baboon label frame: {nativeLabelFrame}; native viewport: {nativeViewport}; " +
					$"content offset: {nativeCollectionView.ContentOffset}; adjusted inset: {nativeCollectionView.AdjustedContentInset}; " +
					$"content inset: {nativeCollectionView.ContentInset}.");
			});
		}

		static bool TryGetRenderedNativeItemLabel(
			UICollectionView nativeCollectionView,
			NSIndexPath indexPath,
			out CGRect nativeLabelFrame,
			out CGRect nativeViewport)
		{
			UIEdgeInsets inset = nativeCollectionView.AdjustedContentInset;
			nativeViewport = new CGRect(
				nativeCollectionView.ContentOffset.X + inset.Left,
				nativeCollectionView.ContentOffset.Y + inset.Top,
				nativeCollectionView.Frame.Width - inset.Left - inset.Right,
				nativeCollectionView.Frame.Height - inset.Top - inset.Bottom);

			var nativeCell = nativeCollectionView.VisibleCells
				.FirstOrDefault(cell => nativeCollectionView.IndexPathForCell(cell)?.Item == indexPath.Item &&
					nativeCollectionView.IndexPathForCell(cell)?.Section == indexPath.Section);
			var nativeLabel = FindNativeLabel(nativeCell, "Baboon");
			if (nativeLabel?.Superview is null)
			{
				nativeLabelFrame = CGRect.Empty;
				return false;
			}

			nativeLabelFrame = nativeLabel.Superview.ConvertRectToView(nativeLabel.Frame, nativeCollectionView);
			bool belongsToCollectionView = false;
			for (UIView current = nativeLabel; current is not null; current = current.Superview)
			{
				if (current.Hidden || current.Alpha <= 0)
					return false;

				if (current == nativeCollectionView)
				{
					belongsToCollectionView = true;
					break;
				}
			}

			return belongsToCollectionView &&
				nativeLabel.Window is not null &&
				nativeLabel.Text == "Baboon" &&
				nativeLabelFrame.Width > 0 &&
				nativeLabelFrame.Height > 0 &&
				nativeLabelFrame.IntersectsWith(nativeViewport);
		}

		static UILabel FindNativeLabel(UIView view, string text)
		{
			if (view is UILabel label && label.Text == text)
				return label;

			if (view is null)
				return null;

			foreach (UIView child in view.Subviews)
			{
				UILabel descendant = FindNativeLabel(child, text);
				if (descendant is not null)
					return descendant;
			}

			return null;
		}

	}
#endif
}

