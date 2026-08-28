#if MACCATALYST
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

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36422")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36422Tests : ControlsHandlerTestBase
	{
		[Fact]
		public async Task FirstItemRemainsAtLeadingEdgeAfterItemSpacingChanges()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				});
			});

			var items = new[]
			{
				new { Name = "Baboon", Location = "Africa" },
				new { Name = "Capuchin Monkey", Location = "Central and South America" },
				new { Name = "Blue Monkey", Location = "Central and East Africa" },
				new { Name = "Squirrel Monkey", Location = "Central and South America" },
				new { Name = "Golden Lion Tamarin", Location = "Brazil" },
				new { Name = "Howler Monkey", Location = "South America" },
				new { Name = "Japanese Macaque", Location = "Japan" },
				new { Name = "Mandrill", Location = "Western Africa" },
				new { Name = "Proboscis Monkey", Location = "Borneo" },
				new { Name = "Red-shanked Douc", Location = "Vietnam" }
			};

			var itemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
			{
				ItemSpacing = 0
			};

			var collectionView = new CollectionView
			{
				ItemsSource = items,
				ItemsLayout = itemsLayout,
				ItemTemplate = new DataTemplate(() =>
				{
					var image = new Image
					{
						Source = "dotnet_bot.png",
						Aspect = Aspect.AspectFill,
						HeightRequest = 60,
						WidthRequest = 60
					};
					Grid.SetRowSpan(image, 2);

					var nameLabel = new Label
					{
						FontAttributes = FontAttributes.Bold
					};
					nameLabel.SetBinding(Label.TextProperty, "Name");

					var locationLabel = new Label
					{
						FontAttributes = FontAttributes.Italic,
						VerticalOptions = LayoutOptions.End
					};
					locationLabel.SetBinding(Label.TextProperty, "Location");

					var itemGrid = new Grid
					{
						Padding = 10,
						RowDefinitions =
						{
							new RowDefinition(GridLength.Auto),
							new RowDefinition(GridLength.Auto)
						},
						ColumnDefinitions =
						{
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Star)
						}
					};
					itemGrid.Add(image, 0, 0);
					itemGrid.Add(nameLabel, 1, 0);
					itemGrid.Add(locationLabel, 1, 1);
					return itemGrid;
				})
			};
			Grid.SetRow(collectionView, 2);

			var instructionLayout = new StackLayout();
			instructionLayout.Add(new Label { Text = "1. The items are displayed in a single column list." });
			instructionLayout.Add(new Label { Text = "2. The first item should remain visible when the vertical spacing changes." });
			instructionLayout.Add(new Label { Text = "3. Change the spacing using the controls below.", FontAttributes = FontAttributes.Bold });

			var spacingEntry = new Entry
			{
				Text = "0",
				WidthRequest = 100
			};
			var updateButton = new Button { Text = "Update" };
			updateButton.Clicked += (sender, args) =>
			{
				if (int.TryParse(spacingEntry.Text, out int spacing))
					itemsLayout.ItemSpacing = spacing;
			};

			var spacingControls = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				HorizontalOptions = LayoutOptions.Center
			};
			spacingControls.Add(new Label
			{
				Text = "Spacing:",
				VerticalTextAlignment = TextAlignment.Center
			});
			spacingControls.Add(spacingEntry);
			spacingControls.Add(updateButton);
			Grid.SetRow(spacingControls, 1);

			var rootGrid = new Grid
			{
				Margin = 20,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			rootGrid.Add(instructionLayout);
			rootGrid.Add(spacingControls);
			rootGrid.Add(collectionView);

			var page = new ContentPage
			{
				Title = "Vertical list (spacing)",
				Content = rootGrid
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.NotNull(collectionView.Handler);
				var collectionHandler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
				var nativeCollectionView = collectionHandler.Controller.CollectionView;
				Assert.NotNull(nativeCollectionView);

				var firstPath = NSIndexPath.FromItemSection(0, 0);
				var secondPath = NSIndexPath.FromItemSection(1, 0);
				UICollectionViewCell firstCell = null;

				bool baselineRendered = await AssertHelpers.Wait(() =>
				{
					nativeCollectionView.LayoutIfNeeded();
					firstCell = nativeCollectionView.CellForItem(firstPath);
					return firstCell is not null && ContainsText(firstCell, "Baboon");
				}, timeout: 5000);
				Assert.True(baselineRendered, "Issue36422 setup: item 0 with text Baboon was not rendered.");

				var baselineFrame = firstCell.Frame;
				var baselineInset = nativeCollectionView.AdjustedContentInset;
				double baselineLeadingEdge = nativeCollectionView.ContentOffset.Y + baselineInset.Top;
				var baselineViewport = new CGRect(
					nativeCollectionView.ContentOffset.X + baselineInset.Left,
					baselineLeadingEdge,
					nativeCollectionView.Bounds.Width - baselineInset.Left - baselineInset.Right,
					nativeCollectionView.Bounds.Height - baselineInset.Top - baselineInset.Bottom);
				Assert.True(
					baselineFrame.IntersectsWith(baselineViewport) &&
					Math.Abs(baselineFrame.Top) <= 1 &&
					Math.Abs(baselineLeadingEdge) <= 1,
					$"Issue36422 setup: item 0 and the viewport were not at the content origin. frame={baselineFrame}, offset={nativeCollectionView.ContentOffset}, inset={baselineInset}, frameTop={baselineFrame.Top:F2}, logicalOffset={baselineLeadingEdge:F2}");

				spacingEntry.Text = string.Empty;
				spacingEntry.Text = "100";
				var nativeButton = Assert.IsAssignableFrom<UIButton>(updateButton.Handler.PlatformView);
				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				Assert.Equal(100, itemsLayout.ItemSpacing);

				double observedGap = -1;
				bool relayoutCompleted = await AssertHelpers.Wait(() =>
				{
					nativeCollectionView.LayoutIfNeeded();
					var firstAttributes = nativeCollectionView.CollectionViewLayout.LayoutAttributesForItem(firstPath);
					var secondAttributes = nativeCollectionView.CollectionViewLayout.LayoutAttributesForItem(secondPath);
					if (firstAttributes is null || secondAttributes is null)
						return false;

					observedGap = secondAttributes.Frame.Top - firstAttributes.Frame.Bottom;
					return Math.Abs(observedGap - 100) <= 1;
				}, timeout: 5000);
				Assert.True(relayoutCompleted, $"Issue36422 setup: native item gap did not become 100; observed {observedGap:F2}.");

				Assert.Equal("Baboon", items[0].Name);
				var postTriggerAttributes = nativeCollectionView.CollectionViewLayout.LayoutAttributesForItem(firstPath);
				Assert.NotNull(postTriggerAttributes);
				firstCell = nativeCollectionView.CellForItem(firstPath);

				var postTriggerFrame = postTriggerAttributes.Frame;
				var postTriggerInset = nativeCollectionView.AdjustedContentInset;
				double postTriggerLeadingEdge = nativeCollectionView.ContentOffset.Y + postTriggerInset.Top;
				var postTriggerViewport = new CGRect(
					nativeCollectionView.ContentOffset.X + postTriggerInset.Left,
					postTriggerLeadingEdge,
					nativeCollectionView.Bounds.Width - postTriggerInset.Left - postTriggerInset.Right,
					nativeCollectionView.Bounds.Height - postTriggerInset.Top - postTriggerInset.Bottom);
				double postTriggerDelta = postTriggerFrame.Top - postTriggerLeadingEdge;
				bool firstItemRendered = firstCell is not null && ContainsText(firstCell, "Baboon");
				bool firstItemVisible = postTriggerFrame.IntersectsWith(postTriggerViewport);
				bool firstItemAtContentOrigin = Math.Abs(postTriggerFrame.Top) <= 1;
				bool viewportAtContentOrigin = Math.Abs(postTriggerLeadingEdge) <= 1;

				Assert.True(
					firstItemRendered && firstItemVisible && firstItemAtContentOrigin && viewportAtContentOrigin,
					$"Issue36422: first item must remain visible at the native viewport leading edge after ItemSpacing changes. expectedFrameTop=0.00, actualFrameTop={postTriggerFrame.Top:F2}, expectedLogicalOffset=0.00, actualLogicalOffset={postTriggerLeadingEdge:F2}, rendered={firstItemRendered}, visible={firstItemVisible}, frame={postTriggerFrame}, offset={nativeCollectionView.ContentOffset}, inset={postTriggerInset}, delta={postTriggerDelta:F2}");
			});

			static bool ContainsText(UIView view, string expectedText)
			{
				if (view is UILabel label && label.Text == expectedText)
					return true;

				return view.Subviews.Any(child => ContainsText(child, expectedText));
			}
		}
	}
}
#endif

