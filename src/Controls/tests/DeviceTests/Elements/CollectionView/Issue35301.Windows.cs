using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using WCornerRadius = Microsoft.UI.Xaml.CornerRadius;
using WListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue35301")]
	public class Issue35301 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DefaultCollectionViewDisablesWinUISelectionChrome()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var items = new[] { "Apple" };
			var collectionView = new CollectionView
			{
				SelectionMode = SelectionMode.Single,
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label
					{
						Padding = new Thickness(0, 12)
					};
					itemLabel.SetBinding(Label.TextProperty, ".");
					return itemLabel;
				})
			};

			var headingLabel = new Label { Text = "CollectionView" };
			var instructionLabel = new Label { Text = "Select an item" };
			var grid = new Grid
			{
				Padding = 16,
				RowSpacing = 12,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			grid.Add(headingLabel);
			grid.Add(instructionLabel, row: 1);
			grid.Add(collectionView, row: 2);

			var page = new ContentPage { Content = grid };
			var window = new Window(page);
			bool attachmentObserved = false;

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(window, async _ =>
			{
				var collectionViewHandler = Assert.IsType<CollectionViewHandler>(collectionView.Handler);
				var listView = Assert.IsAssignableFrom<WListViewBase>(collectionViewHandler.PlatformView);
				attachmentObserved = true;

				Assert.True(attachmentObserved);
				Assert.Same(items, collectionView.ItemsSource);
				Assert.NotNull(collectionView.ItemTemplate);
				Assert.Null(collectionView.Style);
				Assert.Equal(SelectionMode.Single, collectionView.SelectionMode);
				Assert.Null(collectionView.SelectedItem);

				await AssertEventually(() =>
					collectionView.LogicalChildrenInternal
						.OfType<Label>()
						.Any(label => label.Text == "Apple" && label.Handler?.PlatformView is not null));

				bool hasCornerRadius = listView.Resources.TryGetValue("ListViewItemCornerRadius", out var cornerRadiusValue);
				bool cornerRadiusIsZero = cornerRadiusValue is WCornerRadius cornerRadius
					&& Math.Abs(cornerRadius.TopLeft) <= 0.001
					&& Math.Abs(cornerRadius.TopRight) <= 0.001
					&& Math.Abs(cornerRadius.BottomRight) <= 0.001
					&& Math.Abs(cornerRadius.BottomLeft) <= 0.001;
				bool hasSelectionIndicator = listView.Resources.TryGetValue(
					"ListViewItemSelectionIndicatorVisualEnabled",
					out var selectionIndicatorValue);
				bool selectionIndicatorIsDisabled = selectionIndicatorValue is false;

				Assert.True(
					hasCornerRadius && cornerRadiusIsZero && hasSelectionIndicator && selectionIndicatorIsDisabled,
					$"Issue35301 default Windows CollectionView styling was not restored: " +
					$"corner resource present={hasCornerRadius}, corner value={cornerRadiusValue ?? "<missing>"}, " +
					$"selection indicator resource present={hasSelectionIndicator}, " +
					$"selection indicator value={selectionIndicatorValue ?? "<missing>"}.");
			});
		}
	}
}

