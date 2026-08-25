#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	[Category("Issue36545")]
	public class Issue36545 : ControlsHandlerTestBase
	{
		const double Tolerance = 2;

		[Fact]
		public async Task GroupedGridAppliesVerticalSpacingAfterHeader()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Toolbar, ToolbarHandler>();
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler2>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			double zeroSpacingGap = await MeasureHeaderToFirstRowGap(0);
			Assert.True(
				Math.Abs(zeroSpacingGap) <= Tolerance,
				$"Zero-spacing grouped grid header-to-first-row gap was {zeroSpacingGap:F1}; expected 0.0 +/- {Tolerance:F1}.");

			const double expectedSpacing = 30;
			double configuredSpacingGap = await MeasureHeaderToFirstRowGap(expectedSpacing);
			Assert.True(
				Math.Abs(configuredSpacingGap - expectedSpacing) <= Tolerance,
				$"Grouped grid header-to-first-row gap was {configuredSpacingGap:F1}; expected {expectedSpacing:F1} +/- {Tolerance:F1}.");
		}

		async Task<double> MeasureHeaderToFirstRowGap(double verticalItemSpacing)
		{
			bool sizeChanged = false;
			var collectionView = CreateCollectionView(verticalItemSpacing);
			collectionView.SizeChanged += (_, _) => sizeChanged = true;

			var page = new ContentPage
			{
				Title = "Grouped grid spacing",
				Content = collectionView
			};
			var navigationPage = new NavigationPage(page);

			double measuredGap = double.NaN;
			await CreateHandlerAndAddToWindow(new Window(navigationPage), async () =>
			{
				await AssertEventually(
					() => sizeChanged && collectionView.Width > 0 && collectionView.Height > 0,
					timeout: 5000,
					message: "CollectionView did not complete post-attachment sizing.");

				var handler = collectionView.Handler as CollectionViewHandler2;
				Assert.NotNull(handler);
				var nativeCollectionView = handler.Controller.CollectionView;
				Assert.NotNull(nativeCollectionView);

				UILabel headerLabel = null;
				var itemLabels = new UILabel[5];
				string[] expectedItemTexts = ["100", "200", "300", "400", "500"];

				await AssertEventually(
					() =>
					{
						headerLabel = FindLabel(nativeCollectionView, "100s");
						for (int i = 0; i < expectedItemTexts.Length; i++)
							itemLabels[i] = FindLabel(nativeCollectionView, expectedItemTexts[i]);

						return headerLabel is not null &&
							headerLabel.Window is not null &&
							itemLabels.All(label => label is not null && label.Window is not null);
					},
					timeout: 5000,
					message: "The first group header and five-item row were not realized.");

				Assert.Equal("100s", headerLabel.Text);
				for (int i = 0; i < expectedItemTexts.Length; i++)
					Assert.Equal(expectedItemTexts[i], itemLabels[i].Text);

				var headerView = headerLabel.GetParentOfType<UICollectionReusableView>();
				Assert.NotNull(headerView);

				var itemCells = itemLabels
					.Select(label => label.GetParentOfType<UICollectionViewCell>())
					.ToArray();
				Assert.All(itemCells, Assert.NotNull);
				Assert.Equal(5, itemCells.Distinct().Count());

				CGRect collectionFrame = nativeCollectionView.ConvertRectToView(nativeCollectionView.Bounds, null);
				CGRect headerFrame = headerView.ConvertRectToView(headerView.Bounds, null);
				var itemFrames = itemCells
					.Select(cell => cell.ConvertRectToView(cell.Bounds, null))
					.OrderBy(frame => frame.Left)
					.ToArray();

				AssertFrameWithinCollection(headerFrame, collectionFrame, "header 100s");
				for (int i = 0; i < itemFrames.Length; i++)
				{
					AssertFrameWithinCollection(itemFrames[i], collectionFrame, $"item {expectedItemTexts[i]}");
					Assert.True(
						Math.Abs(itemFrames[i].Top - itemFrames[0].Top) <= Tolerance,
						$"Item {expectedItemTexts[i]} was not realized in the first five-column row.");
					if (i > 0)
						Assert.True(itemFrames[i].Left > itemFrames[i - 1].Left, "The first row did not realize five distinct columns.");
				}

				using var renderedHeader = await headerView.ToBitmap(MauiContext);
				using var renderedFirstItem = await itemCells[0].ToBitmap(MauiContext);
				AssertRenderedGray(renderedHeader, "header 100s");
				AssertRenderedGray(renderedFirstItem, "item 100");

				Assert.True(itemFrames[0].Top >= headerFrame.Bottom, "Item 100 was not positioned after header 100s.");
				measuredGap = itemFrames[0].Top - headerFrame.Bottom;
			});

			Assert.False(double.IsNaN(measuredGap), "The native header-to-row gap was not measured.");
			return measuredGap;
		}

		static CollectionView CreateCollectionView(double verticalItemSpacing)
		{
			var collectionView = new CollectionView
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				Margin = new Thickness(5, 30, 5, 5),
				IsGrouped = true,
				ItemsLayout = new GridItemsLayout(5, ItemsLayoutOrientation.Vertical)
				{
					HorizontalItemSpacing = 10,
					VerticalItemSpacing = verticalItemSpacing
				},
				ItemsSource = new ObservableCollection<NumberGroup>
				{
					CreateGroup("100s", ["100", "200", "300", "400", "500", "600", "700", "800", "900"]),
					CreateGroup("1000s", ["1000", "2000", "3000", "4000", "5000", "6000", "7000", "8000", "9000"])
				}
			};

			collectionView.GroupHeaderTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					HorizontalOptions = LayoutOptions.Fill,
					HorizontalTextAlignment = TextAlignment.Start,
					Padding = new Thickness(10),
					FontSize = 18,
					TextColor = Colors.White,
					FontAttributes = FontAttributes.Bold,
					BackgroundColor = Colors.Gray
				};
				label.SetBinding(Label.TextProperty, nameof(NumberGroup.Name));
				return label;
			});

			collectionView.ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label
				{
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					HorizontalTextAlignment = TextAlignment.Center,
					TextColor = Colors.White
				};
				label.SetBinding(Label.TextProperty, ".");

				return new Border
				{
					StrokeShape = new RoundRectangle { CornerRadius = 10 },
					Padding = new Thickness(5),
					MinimumWidthRequest = 50,
					Stroke = Colors.Transparent,
					BackgroundColor = Colors.Gray,
					StrokeThickness = 1,
					HorizontalOptions = LayoutOptions.Center,
					Content = label
				};
			});

			return collectionView;
		}

		static NumberGroup CreateGroup(string name, IEnumerable<string> numbers)
		{
			var group = new NumberGroup { Name = name };
			foreach (string number in numbers)
				group.Add(number);

			return group;
		}

		static UILabel FindLabel(UIView root, string text)
		{
			if (root is UILabel label && label.Text == text)
				return label;

			foreach (UIView subview in root.Subviews)
			{
				var match = FindLabel(subview, text);
				if (match is not null)
					return match;
			}

			return null;
		}

		static void AssertRenderedGray(UIImage bitmap, string identity)
		{
			Assert.True(bitmap.Size.Width > 0 && bitmap.Size.Height > 0, $"The native view for {identity} did not render a bitmap.");
			int x = (int)Math.Floor((bitmap.Size.Width * 0.5) + Math.Min(20, bitmap.Size.Width * 0.2));
			int y = (int)Math.Floor(bitmap.Size.Height * 0.5);
			Assert.True(
				x >= 0 && x < bitmap.Size.Width && y >= 0 && y < bitmap.Size.Height,
				$"The rendered sample point for {identity} was outside its native bitmap.");
			bitmap.AssertColorAtPoint(Colors.Gray.ToPlatform(), x, y, tolerance: 0.1);
		}

		static void AssertFrameWithinCollection(CGRect frame, CGRect collectionFrame, string identity)
		{
			Assert.True(frame.Width > 0 && frame.Height > 0, $"The native frame for {identity} was empty.");
			Assert.True(
				frame.Left >= collectionFrame.Left - Tolerance &&
				frame.Right <= collectionFrame.Right + Tolerance &&
				frame.Top >= collectionFrame.Top - Tolerance &&
				frame.Bottom <= collectionFrame.Bottom + Tolerance,
				$"The native frame for {identity} was outside the CollectionView surface.");
		}

		sealed class NumberGroup : ObservableCollection<string>
		{
			public string Name { get; set; }
		}
	}
}
#endif

