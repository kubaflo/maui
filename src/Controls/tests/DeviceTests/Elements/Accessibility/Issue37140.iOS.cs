#if MACCATALYST
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using ShellHandler = Microsoft.Maui.Controls.Handlers.Compatibility.ShellRenderer;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Accessibility)]
	[Category("Issue37140")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue37140 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SelectableTaskItemExposesButtonAccessibilityTrait()
		{
			const string taskName = "Write release notes";

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CheckBox, CheckBoxHandler>();
				});
			});

			var collectionView = new CollectionView
			{
				HeightRequest = 180,
				SelectionMode = SelectionMode.Single,
				ItemsSource = new[] { taskName },
				ItemTemplate = new DataTemplate(() =>
				{
					var itemGrid = new Grid
					{
						ColumnDefinitions = new ColumnDefinitionCollection
						{
							new ColumnDefinition(GridLength.Star),
							new ColumnDefinition(GridLength.Auto)
						}
					};
					itemGrid.SetBinding(SemanticProperties.DescriptionProperty, ".");

					var taskLabel = new Label
					{
						FontSize = 18,
						VerticalOptions = LayoutOptions.Center
					};
					taskLabel.SetBinding(Label.TextProperty, ".");

					var checkBox = new CheckBox
					{
						IsChecked = false,
						VerticalOptions = LayoutOptions.Center
					};
					Grid.SetColumn(checkBox, 1);

					itemGrid.Add(taskLabel);
					itemGrid.Add(checkBox);

					return new Border
					{
						Padding = 16,
						Stroke = Color.FromArgb("#808080"),
						StrokeShape = new RoundRectangle { CornerRadius = 12 },
						Content = itemGrid
					};
				})
			};

			var dashboard = new VerticalStackLayout
			{
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 28,
						Text = "Tasks"
					},
					collectionView
				}
			};

			var page = new ContentPage
			{
				Title = "Dashboard",
				Content = new Grid
				{
					Padding = 24,
					Children =
					{
						new ScrollView { Content = dashboard }
					}
				}
			};

			var shell = new Shell();
			shell.Items.Add(new ShellContent
			{
				Title = "Dashboard",
				Route = "Dashboard",
				ContentTemplate = new DataTemplate(() => page)
			});

			var observedTraits = unchecked((UIAccessibilityTrait)(1UL << 63));
			var observedTraitsCaptured = false;

			await CreateHandlerAndAddToWindow<ShellHandler>(shell, async _ =>
			{
				await AssertEventually(
					() => collectionView.IsLoaded,
					message: "Issue37140: the task CollectionView did not load.");
				Assert.True(collectionView.IsLoaded, "Issue37140: the task CollectionView must be loaded before inspecting native accessibility state.");

				var platformView = collectionView.ToPlatform();
				Assert.NotNull(platformView);

				var nativeCollectionView = platformView as UICollectionView
					?? platformView.GetParentOfType<UICollectionView>()
					?? platformView.Subviews.OfType<UICollectionView>().FirstOrDefault();
				Assert.NotNull(nativeCollectionView);

				UICollectionViewCell realizedCell = null;
				await AssertEventually(
					() =>
					{
						realizedCell = nativeCollectionView.VisibleCells.SingleOrDefault();
						return realizedCell is not null;
					},
					message: "Issue37140: the task CollectionView did not realize its single item.");
				Assert.NotNull(realizedCell);

				UIView semanticTaskView = null;
				await AssertEventually(
					() =>
					{
						semanticTaskView = FindSubviewWithAccessibilityLabel(realizedCell.ContentView, taskName);
						return semanticTaskView is not null;
					},
					message: $"Issue37140: the realized cell did not expose the expected task '{taskName}'.");
				Assert.NotNull(semanticTaskView);
				Assert.Equal(taskName, semanticTaskView.AccessibilityLabel);

				observedTraits = semanticTaskView.AccessibilityTraits;
				observedTraitsCaptured = true;
			});

			Assert.True(observedTraitsCaptured, "Issue37140: native accessibility traits were not captured after the target task item was realized.");
			Assert.True(
				(observedTraits & UIAccessibilityTrait.Button) == UIAccessibilityTrait.Button,
				$"Issue37140: task item '{taskName}' native accessibility traits must include Button; observed traits: {observedTraits}");

			static UIView FindSubviewWithAccessibilityLabel(UIView root, string accessibilityLabel)
			{
				if (root.AccessibilityLabel == accessibilityLabel)
					return root;

				foreach (var subview in root.Subviews)
				{
					var match = FindSubviewWithAccessibilityLabel(subview, accessibilityLabel);
					if (match is not null)
						return match;
				}

				return null;
			}
		}
	}
}
#endif

