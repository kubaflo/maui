using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue32543")]
	public class Issue32543 : ControlsHandlerTestBase
	{
		const double ItemHeight = 25;
		const double PositionTolerance = 4;

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

			const string startText = "Item VerticalOptions=\"Start\"";
			const string centerText = "Item VerticalOptions=\"Center\"";
			const string endText = "Item VerticalOptions=\"End\"";

			Label startLabel = null;
			Label centerLabel = null;
			Label endLabel = null;
			bool startLoaded = false;
			bool centerLoaded = false;
			bool endLoaded = false;

			var startCollection = CreateCollection(
				startText,
				LayoutOptions.Start,
				label => startLabel = label,
				() => startLoaded = true);
			var centerCollection = CreateCollection(
				centerText,
				LayoutOptions.Center,
				label => centerLabel = label,
				() => centerLoaded = true);
			var endCollection = CreateCollection(
				endText,
				LayoutOptions.End,
				label => endLabel = label,
				() => endLoaded = true);

			var middleGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				}
			};
			middleGrid.Add(startCollection, 0);
			middleGrid.Add(centerCollection, 1);
			middleGrid.Add(endCollection, 2);

			var rootGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star)
				}
			};
			rootGrid.Add(new BoxView { Color = Colors.Black }, 0, 0);
			rootGrid.Add(middleGrid, 0, 1);
			rootGrid.Add(new BoxView { Color = Colors.Black }, 0, 2);

			var page = new ContentPage { Content = rootGrid };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(() =>
					startLoaded &&
					centerLoaded &&
					endLoaded &&
					startCollection.Width > 0 &&
					startCollection.Height > ItemHeight &&
					centerCollection.Width > 0 &&
					centerCollection.Height > ItemHeight &&
					endCollection.Width > 0 &&
					endCollection.Height > ItemHeight);

				Assert.True(startLoaded);
				Assert.True(centerLoaded);
				Assert.True(endLoaded);
				Assert.Equal(startText, startLabel.Text);
				Assert.Equal(centerText, centerLabel.Text);
				Assert.Equal(endText, endLabel.Text);

				Assert.NotNull(page.Window);
				Window window = page.Window;

				double startX = GetHorizontalCenter(startCollection, page);
				double centerX = GetHorizontalCenter(centerCollection, page);
				double endX = GetHorizontalCenter(endCollection, page);

				Assert.True(TryGetVerticalHitRange(window, startCollection, startX, out double startCollectionTop, out double startCollectionBottom));
				Assert.True(TryGetVerticalHitRange(window, centerCollection, centerX, out double centerCollectionTop, out double centerCollectionBottom));
				Assert.True(TryGetVerticalHitRange(window, endCollection, endX, out double endCollectionTop, out double endCollectionBottom));

				bool hasStartLabel = TryGetVerticalHitRange(window, startLabel, startX, out double startTop, out double startBottom);
				bool hasCenterLabel = TryGetVerticalHitRange(window, centerLabel, centerX, out double centerTop, out double centerBottom);
				bool hasEndLabel = TryGetVerticalHitRange(window, endLabel, endX, out double endTop, out double endBottom);

				double expectedCenter = (centerCollectionTop + centerCollectionBottom) / 2;
				double actualCenter = (centerTop + centerBottom) / 2;

				Assert.True(
					hasStartLabel && Math.Abs(startTop - startCollectionTop) <= PositionTolerance,
					$"Issue32543 Start item vertical placement incorrect: measured {startTop}-{startBottom}, expected top {startCollectionTop}.");
				Assert.InRange(startBottom - startTop, ItemHeight - PositionTolerance, ItemHeight + PositionTolerance);
				Assert.True(
					hasCenterLabel && Math.Abs(actualCenter - expectedCenter) <= PositionTolerance,
					$"Issue32543 Center item vertical placement incorrect: measured {centerTop}-{centerBottom}, expected center {expectedCenter}.");
				Assert.InRange(centerBottom - centerTop, ItemHeight - PositionTolerance, ItemHeight + PositionTolerance);
				Assert.True(
					hasEndLabel && Math.Abs(endBottom - endCollectionBottom) <= PositionTolerance,
					$"Issue32543 End item vertical placement incorrect: measured {endTop}-{endBottom}, expected bottom {endCollectionBottom}.");
				Assert.InRange(endBottom - endTop, ItemHeight - PositionTolerance, ItemHeight + PositionTolerance);
			});

			static CollectionView CreateCollection(
				string text,
				LayoutOptions verticalOptions,
				Action<Label> captureLabel,
				Action markLoaded)
			{
				return new CollectionView
				{
					BackgroundColor = Colors.Transparent,
					SelectionMode = SelectionMode.Single,
					HorizontalOptions = LayoutOptions.Center,
					ItemsSource = new[] { text },
					ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal)
					{
						ItemSpacing = 10
					},
					ItemTemplate = new DataTemplate(() =>
					{
						var label = new Label
						{
							BackgroundColor = Colors.Red,
							TextColor = Colors.White,
							HeightRequest = ItemHeight,
							VerticalTextAlignment = TextAlignment.Center,
							HorizontalTextAlignment = TextAlignment.Center,
							VerticalOptions = verticalOptions
						};
						label.SetBinding(Label.TextProperty, ".");
						captureLabel(label);
						label.Loaded += (_, _) => markLoaded();
						return label;
					})
				};
			}

			static double GetHorizontalCenter(VisualElement element, VisualElement root)
			{
				double x = element.Width / 2;
				Element current = element;

				while (current is VisualElement visualElement && current != root)
				{
					x += visualElement.X;
					current = visualElement.Parent;
				}

				return x;
			}

			static bool TryGetVerticalHitRange(
				Window window,
				IVisualTreeElement target,
				double x,
				out double top,
				out double bottom)
			{
				top = double.NaN;
				bottom = double.NaN;
				bool found = false;

				for (int y = 0; y < Math.Ceiling(window.Height); y++)
				{
					foreach (IVisualTreeElement element in window.GetVisualTreeElements(x, y))
					{
						if (!ReferenceEquals(element, target))
							continue;

						if (!found)
							top = y;

						bottom = y + 1;
						found = true;
						break;
					}
				}

				return found;
			}
		}
	}
}

