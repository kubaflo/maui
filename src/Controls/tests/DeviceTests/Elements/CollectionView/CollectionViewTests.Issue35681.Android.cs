#if ANDROID
using System.Linq;
using System.Threading.Tasks;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using ATextView = Android.Widget.TextView;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(RunInNewWindowCollection)]
	[Category(TestCategory.CollectionView)]
	public class Issue35681 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task AccessibilityRowCountExcludesHeaderAndFooter()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
				});
			});

			var items = new[] { "First item", "Second item", "Third item" };
			var collectionView = new CollectionView
			{
				Header = new Label { Text = "Collection header" },
				ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label();
					itemLabel.SetBinding(Label.TextProperty, ".");
					return itemLabel;
				}),
				EmptyView = new Label { Text = "There is nothing to see here!" },
				Footer = new Label { Text = "Collection footer" },
				ItemsSource = items
			};
			var diagnosticButton = new Button { Text = "Check list metadata" };
			var resultLabel = new Label { Text = "NO BUG:" };
			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(collectionView);
			grid.Add(diagnosticButton, 0, 1);
			grid.Add(resultLabel, 0, 2);

			Assert.Same(items, collectionView.ItemsSource);
			Assert.Equal(items, collectionView.ItemsSource.Cast<string>());

			var page = new ContentPage { Content = grid };
			var expectedNativeRows = new[]
			{
				"Collection header",
				"First item",
				"Second item",
				"Third item",
				"Collection footer"
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async (_) =>
			{
				var recyclerView = Assert.IsAssignableFrom<RecyclerView>(collectionView.Handler.PlatformView);

				await AssertEventually(
					() => recyclerView.IsLaidOut && recyclerView.Width > 0 && recyclerView.Height > 0,
					timeout: 5000,
					message: "Issue35681 MauiRecyclerView did not complete layout.");
				Assert.True(recyclerView.IsLaidOut && recyclerView.Width > 0 && recyclerView.Height > 0,
					"Issue35681 MauiRecyclerView must be laid out before metadata is requested.");

				await AssertEventually(
					() => expectedNativeRows.SequenceEqual(GetAdapterRowTexts(recyclerView, expectedNativeRows.Length)),
					timeout: 5000,
					message: "Issue35681 expected header, items, and footer were not realized in adapter order.");
				Assert.Equal(expectedNativeRows, GetAdapterRowTexts(recyclerView, expectedNativeRows.Length));

				var metadataRequested = false;
				var observedRowCount = -1;
				using var nodeInfo = recyclerView.CreateAccessibilityNodeInfo();
				metadataRequested = true;
				Assert.True(metadataRequested, "Issue35681 accessibility metadata was not requested.");

				var collectionInfo = nodeInfo.GetCollectionInfo();
				Assert.NotNull(collectionInfo);
				observedRowCount = collectionInfo.RowCount;
				Assert.NotEqual(-1, observedRowCount);
				Assert.True(observedRowCount == items.Length,
					$"Issue35681 accessibility row count was {observedRowCount}; expected {items.Length} data items with the header and footer excluded.");
			});
		}

		static string[] GetAdapterRowTexts(RecyclerView recyclerView, int count)
		{
			return Enumerable.Range(0, count)
				.Select(position => FindText(recyclerView.FindViewHolderForAdapterPosition(position)?.ItemView))
				.ToArray();
		}

		static string FindText(AView view)
		{
			if (view is ATextView textView)
				return textView.Text;

			if (view is AViewGroup viewGroup)
			{
				for (var index = 0; index < viewGroup.ChildCount; index++)
				{
					var text = FindText(viewGroup.GetChildAt(index));
					if (text is not null)
						return text;
				}
			}

			return null;
		}
	}
}
#endif
