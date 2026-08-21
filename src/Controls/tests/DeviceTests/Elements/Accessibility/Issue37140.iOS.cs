#if MACCATALYST
using System;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue37140")]
	[Category(TestCategory.Accessibility)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37140 : ControlsHandlerTestBase
	{
		const string TaskName = "Review accessibility";

		[Fact]
		public async Task InteractiveCollectionItemExposesButtonTrait()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddMauiControlsHandlers();
					handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
				});
			});

			var collectionView = CreateTaskCollection();
			var contentGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
				},
				RowSpacing = 15,
			};

			var headingLabel = new Label
			{
				Text = "Tasks",
				FontSize = 24,
				FontAttributes = FontAttributes.Bold,
			};
			SemanticProperties.SetHeadingLevel(headingLabel, SemanticHeadingLevel.Level1);

			contentGrid.Add(headingLabel, 0, 0);
			contentGrid.Add(collectionView, 0, 1);

			var page = new ContentPage
			{
				Title = "Task Collection",
				Content = new Grid
				{
					Padding = 20,
					Children =
					{
						new ScrollView { Content = contentGrid },
					},
				},
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var handler = Assert.IsType<CollectionViewHandler2>(collectionView.Handler);
				var nativeCollection = handler.Controller.CollectionView;
				UICollectionViewCell taskCell = null;
				UIView taskAccessibilityElement = null;

				await AssertEventually(
					() =>
					{
						taskCell = nativeCollection.CellForItem(NSIndexPath.FromItemSection(0, 0));
						return taskCell is not null;
					},
					timeout: 5000,
					message: $"The native collection cell for '{TaskName}' did not materialize.");

				var taskIndexPath = nativeCollection.IndexPathForCell(taskCell);
				Assert.NotNull(taskIndexPath);
				Assert.Equal(0, taskIndexPath.Section);
				Assert.Equal(0, taskIndexPath.Item);

				await AssertEventually(
					() =>
					{
						taskAccessibilityElement = FindAccessibilityElement(taskCell, TaskName);
						return taskAccessibilityElement is not null;
					},
					timeout: 5000,
					message: $"The native accessibility element labeled '{TaskName}' did not materialize.");

				var traits = taskAccessibilityElement.AccessibilityTraits;
				Assert.True(
					(traits & UIAccessibilityTrait.Button) == UIAccessibilityTrait.Button,
					$"Interactive task accessibility element '{TaskName}' must expose UIAccessibilityTrait.Button; observed traits: {traits}");
			});
		}

		static CollectionView CreateTaskCollection()
		{
			return new CollectionView
			{
				ItemsSource = new[] { TaskName },
				SelectionMode = SelectionMode.Single,
				ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
				{
					ItemSpacing = 15,
				},
				ItemTemplate = new DataTemplate(() =>
				{
					var semanticGrid = new Grid { Margin = -20 };
					semanticGrid.SetBinding(SemanticProperties.DescriptionProperty, ".");

					var taskLabel = new Label
					{
						Margin = new Thickness(70, 0, 0, 0),
						HorizontalOptions = LayoutOptions.Start,
						VerticalOptions = LayoutOptions.Center,
						LineBreakMode = LineBreakMode.WordWrap,
					};
					taskLabel.SetBinding(Label.TextProperty, ".");
					semanticGrid.Add(taskLabel);

					var checkBox = new CheckBox
					{
						WidthRequest = 50,
						HorizontalOptions = LayoutOptions.Start,
						VerticalOptions = LayoutOptions.Center,
						IsChecked = false,
					};
					AutomationProperties.SetIsInAccessibleTree(checkBox, true);
					checkBox.SetBinding(SemanticProperties.DescriptionProperty, ".");

					var itemGrid = new Grid
					{
						ColumnDefinitions = { new ColumnDefinition(GridLength.Star) },
						ColumnSpacing = 15,
						Padding = 20,
					};
					itemGrid.Add(semanticGrid);
					itemGrid.Add(checkBox);

					return new Border
					{
						StrokeShape = new RoundRectangle { CornerRadius = 20 },
						Content = itemGrid,
					};
				}),
			};
		}

		static UIView FindAccessibilityElement(UIView root, string label)
		{
			if (root.IsAccessibilityElement &&
				string.Equals(root.AccessibilityLabel, label, StringComparison.Ordinal))
			{
				return root;
			}

			foreach (var subview in root.Subviews)
			{
				var match = FindAccessibilityElement(subview, label);
				if (match is not null)
					return match;
			}

			return null;
		}
	}
}
#endif

