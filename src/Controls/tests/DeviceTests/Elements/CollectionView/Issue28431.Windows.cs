#if WINDOWS
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue28431")]
	public class Issue28431 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CollectionViewTemplateRootMarginSpacesRealizedItems()
		{
			const double cardHeight = 71;
			const double gridVerticalPadding = 6;
			const double targetVerticalMargin = 60;
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			(ContentPage Page, CollectionView Collection, List<(Border Card, Label DateLabel)> Rows) CreateScenario(Action itemLoaded)
			{
				var rows = new List<(Border Card, Label DateLabel)>();
				var collection = new CollectionView
				{
					HeightRequest = 650,
					ItemTemplate = new DataTemplate(() =>
					{
						var morningDate = new Label();
						morningDate.SetBinding(Label.TextProperty, "Date");
						var morningWeekday = new Label();
						morningWeekday.SetBinding(Label.TextProperty, "Weekday");

						var morningCard = new Border
						{
							HeightRequest = cardHeight,
							Padding = new Thickness(10, 3),
							StrokeShape = new RoundRectangle { CornerRadius = 5 },
							Content = new VerticalStackLayout
							{
								Spacing = 1,
								Children =
								{
									morningDate,
									morningWeekday,
									new Label { Text = "Morning: 08:00" },
								}
							}
						};

						var afternoonDate = new Label();
						afternoonDate.SetBinding(Label.TextProperty, "Date");
						var afternoonWeekday = new Label();
						afternoonWeekday.SetBinding(Label.TextProperty, "Weekday");
						var afternoonCard = new Border
						{
							HeightRequest = cardHeight,
							Padding = new Thickness(10, 3),
							StrokeShape = new RoundRectangle { CornerRadius = 5 },
							Content = new VerticalStackLayout
							{
								Spacing = 1,
								Children =
								{
									afternoonDate,
									afternoonWeekday,
									new Label { Text = "Afternoon: 17:00" },
								}
							}
						};

						var grid = new Grid
						{
							ColumnDefinitions =
							{
								new ColumnDefinition(GridLength.Star),
								new ColumnDefinition(GridLength.Star),
							},
							ColumnSpacing = 6,
							Padding = new Thickness(10, 3),
							Margin = new Thickness(50, 30),
						};
						grid.Add(morningCard, 0);
						grid.Add(afternoonCard, 1);
						grid.Loaded += (_, _) => itemLoaded();
						rows.Add((morningCard, morningDate));
						return grid;
					})
				};
				collection.SetBinding(ItemsView.ItemsSourceProperty, "Schedule");

				var page = new ContentPage
				{
					Title = "CollectionView item margin",
					BindingContext = new
					{
						Schedule = new ObservableCollection<object>
						{
							new { Index = 0, Date = "Date: 2025-03-10", Weekday = "Weekday: Monday" },
							new { Index = 1, Date = "Date: 2025-03-11", Weekday = "Weekday: Tuesday" },
							new { Index = 2, Date = "Date: 2025-03-12", Weekday = "Weekday: Wednesday" },
							new { Index = 3, Date = "Date: 2025-03-13", Weekday = "Weekday: Thursday" },
						}
					},
					Content = new VerticalStackLayout
					{
						Padding = 12,
						Spacing = 8,
						Children =
						{
							new Label
							{
								Text = "CollectionView template grids use Margin=\"50,30\". The cards should have 60 units of vertical space between rows.",
								FontAttributes = FontAttributes.Bold,
							},
							new Label { Text = "Schedule rows are shown below." },
							new Button { Text = "Check item spacing" },
							collection,
						}
					}
				};

				return (page, collection, rows);
			}

			(Border Card, Label DateLabel)[] GetFirstTwoRealizedRows(List<(Border Card, Label DateLabel)> rows)
			{
				(Border Card, Label DateLabel) first = default;
				(Border Card, Label DateLabel) second = default;

				for (int i = rows.Count - 1; i >= 0; i--)
				{
					var row = rows[i];
					var bounds = ((IView)row.Card).GetPlatformViewBounds();
					if (!row.Card.GetLocationOnScreen().HasValue || bounds.Width <= 0 || bounds.Height <= 0)
						continue;

					if (first.Card is null && row.DateLabel.Text == "Date: 2025-03-10")
						first = row;
					else if (second.Card is null && row.DateLabel.Text == "Date: 2025-03-11")
						second = row;

					if (first.Card is not null && second.Card is not null)
						return new[] { first, second };
				}

				return Array.Empty<(Border Card, Label DateLabel)>();
			}

			Rect GetNativeFrame(Border card)
			{
				var location = card.GetLocationOnScreen();
				Assert.True(location.HasValue);
				var bounds = ((IView)card).GetPlatformViewBounds();
				return new Rect(location.Value.X, location.Value.Y, bounds.Width, bounds.Height);
			}

			bool CollectionIsLaidOut(CollectionView collection)
			{
				var bounds = ((IView)collection).GetPlatformViewBounds();
				return collection.GetLocationOnScreen().HasValue && bounds.Width > 0 && bounds.Height > 0;
			}

			var targetRealizationState = -1;
			var target = CreateScenario(() => targetRealizationState++);
			await CreateHandlerAndAddToWindow<IWindowHandler>(new Window(target.Page), async _ =>
			{
				await AssertEventually(
					() => targetRealizationState >= 1,
					timeout: 5000,
					message: "Issue28431 target item Loaded callbacks did not run.");
				Assert.NotEqual(-1, targetRealizationState);

				await AssertEventually(
					() => CollectionIsLaidOut(target.Collection),
					timeout: 5000,
					message: "Issue28431 target CollectionView was not laid out.");
				await AssertEventually(
					() => GetFirstTwoRealizedRows(target.Rows).Length == 2,
					timeout: 5000,
					message: "Issue28431 target rows were not realized.");

				var rows = GetFirstTwoRealizedRows(target.Rows);
				var firstFrame = GetNativeFrame(rows[0].Card);
				var secondFrame = GetNativeFrame(rows[1].Card);
				var topToTop = secondFrame.Y - firstFrame.Y;
				var visibleGap = topToTop - firstFrame.Height;
				var expectedTopToTop = cardHeight + gridVerticalPadding + targetVerticalMargin;
				var expectedGap = gridVerticalPadding + targetVerticalMargin;

				Assert.True(Math.Abs(firstFrame.Height - cardHeight) <= tolerance);
				Assert.True(Math.Abs(secondFrame.Height - cardHeight) <= tolerance);
				Assert.True(
					Math.Abs(topToTop - expectedTopToTop) <= tolerance,
					$"Issue28431 target item spacing was incorrect: first={firstFrame}, second={secondFrame}, gap={visibleGap:F1}, top-to-top={topToTop:F1}, expected gap={expectedGap:F1}, expected top-to-top={expectedTopToTop:F1}.");
			});
		}
	}
}
#endif

