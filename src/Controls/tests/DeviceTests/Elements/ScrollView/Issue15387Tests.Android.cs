#if ANDROID
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.ScrollView)]
	[Category("Issue15387")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue15387 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ScrollToOriginCompletesDuringInitialAppearing()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
				});
			});

			string[] items =
			[
				"Bindable item ready",
				"Bindable item 2",
				"Bindable item 3",
				"Bindable item 4",
				"Bindable item 5",
				"Bindable item 6",
				"Bindable item 7",
				"Bindable item 8",
				"Bindable item 9",
				"Bindable item 10"
			];

			var itemLayout = new StackLayout
			{
				Spacing = 8
			};
			BindableLayout.SetItemTemplate(itemLayout, new DataTemplate(() =>
			{
				var label = new Label
				{
					Padding = 8
				};
				label.SetBinding(Label.TextProperty, ".");
				return label;
			}));
			BindableLayout.SetItemsSource(itemLayout, items);

			var scrollView = new ScrollView
			{
				Content = itemLayout
			};
			var lifecycleLabel = new Label
			{
				Text = "Waiting for OnAppearing"
			};
			var statusLabel = new Label
			{
				Text = "Scroll completion pending",
				FontAttributes = FontAttributes.Bold
			};
			var completionButton = new Button
			{
				Text = "Check completion"
			};
			var bottomLayout = new VerticalStackLayout
			{
				Spacing = 8,
				Children =
				{
					statusLabel,
					completionButton
				}
			};
			var grid = new Grid
			{
				Padding = 20,
				RowSpacing = 12,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				}
			};
			grid.Add(new Label
			{
				Text = "Issue 15387 ScrollToAsync",
				FontAttributes = FontAttributes.Bold,
				FontSize = 20
			});
			grid.Add(lifecycleLabel, 0, 1);
			grid.Add(scrollView, 0, 2);
			grid.Add(bottomLayout, 0, 3);

			var page = new ContentPage
			{
				Title = "Issue 15387 ScrollToAsync",
				Content = grid
			};
			var window = new Window(page);
			var scrollStarted = new TaskCompletionSource<Task>();
			var continuationReached = new TaskCompletionSource();
			int appearingCount = -1;

			page.Appearing += async (_, _) =>
			{
				appearingCount = appearingCount < 0 ? 1 : appearingCount + 1;
				lifecycleLabel.Text = "Before ScrollToAsync";

				Task scrollOperation = scrollView.ScrollToAsync(0, 0, false);
				scrollStarted.TrySetResult(scrollOperation);
				await scrollOperation;

				lifecycleLabel.Text = "Before ScrollToAsync\nAfter ScrollToAsync";
				statusLabel.Text = "Scroll completion observed";
				continuationReached.TrySetResult();
			};

			Assert.Equal(-1, appearingCount);
			Assert.False(scrollStarted.Task.IsCompleted);

			await CreateHandlerAndAddToWindow(window, async () =>
			{
				await AssertHelpers.AssertEventually(
					() => scrollStarted.Task.IsCompleted,
					timeout: 2000,
					message: "Initial Appearing did not run");
				Assert.Equal(1, appearingCount);
				Task scrollOperation = await scrollStarted.Task;

				Label[] generatedItems = itemLayout.Children.OfType<Label>().ToArray();
				Assert.Equal(items.Length, generatedItems.Length);
				Assert.Equal(items, generatedItems.Select(label => label.Text).ToArray());

				var scrollViewHandler = Assert.IsType<ScrollViewHandler>(scrollView.Handler);
				var platformScrollView = Assert.IsType<MauiScrollView>(scrollViewHandler.PlatformView);
				Assert.True(platformScrollView.IsAttachedToWindow);

				await AssertHelpers.AssertEventually(
					() => scrollOperation.IsCompleted,
					timeout: 2000,
					message: $"Issue15387: ScrollToAsync did not complete after initial OnAppearing; callbacks={appearingCount}, items={generatedItems.Length}, attached={platformScrollView.IsAttachedToWindow}, scrollX={platformScrollView.ScrollX}, scrollY={platformScrollView.ScrollY}");

				Assert.True(
					scrollOperation.IsCompletedSuccessfully,
					$"Issue15387: ScrollToAsync did not complete after initial OnAppearing; callbacks={appearingCount}, items={generatedItems.Length}, attached={platformScrollView.IsAttachedToWindow}, scrollX={platformScrollView.ScrollX}, scrollY={platformScrollView.ScrollY}");

				await AssertHelpers.AssertEventually(
					() => continuationReached.Task.IsCompleted,
					timeout: 2000,
					message: "The OnAppearing continuation did not run after ScrollToAsync completed");
			});
		}
	}
}
#endif

