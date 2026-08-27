#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue29540")]
	public class Issue29540 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task CustomTabbedViewHandlerCanNavigateToTabbedPage()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<SwipeTabbedPage, SwipeTabbedViewHandler>();
				});
			});

			var scenarioLabel = new Label
			{
				AutomationId = "ScenarioLabel",
				FontAttributes = FontAttributes.Bold,
				FontSize = 20,
				Text = "Custom TabbedPage with a TabbedViewHandler subclass"
			};
			var hierarchyLabel = new Label
			{
				AutomationId = "HierarchyLabel",
				Text = "NavigationPage -> SwipeTabbedPage -> two ContentPage tabs"
			};
			var navigateButton = new Button
			{
				AutomationId = "NavigateButton",
				Text = "Navigate to custom TabbedPage"
			};
			var rootGrid = new Grid
			{
				Padding = 24,
				RowSpacing = 16,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			rootGrid.Add(scenarioLabel, 0, 0);
			rootGrid.Add(hierarchyLabel, 0, 1);
			rootGrid.Add(navigateButton, 0, 2);

			var rootPage = new ContentPage
			{
				Title = "TabbedViewHandler reproduction",
				Content = rootGrid
			};
			var firstTab = new ContentPage
			{
				Title = "First",
				Content = new Label
				{
					Margin = 24,
					FontSize = 20,
					Text = "First tab rendered by the custom handler"
				}
			};
			var secondTab = new ContentPage
			{
				Title = "Second",
				Content = new Label
				{
					Margin = 24,
					Text = "Second tab"
				}
			};
			var tabbedPage = new SwipeTabbedPage
			{
				Title = "Custom TabbedPage",
				Children =
				{
					firstTab,
					secondTab
				}
			};
			var navigationPage = new NavigationPage(rootPage);
			var window = new Window(navigationPage);
			var firstTabAppeared = new TaskCompletionSource();
			bool didFirstTabAppear = false;
			firstTab.Appearing += (_, _) =>
			{
				didFirstTabAppear = true;
				firstTabAppeared.TrySetResult();
			};

			NotImplementedException navigationException = null;
			Task navigationTask = Task.CompletedTask;
			navigateButton.Clicked += (_, _) => navigationTask = NavigateAsync();

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(window, async _ =>
			{
				Assert.Single(navigationPage.Navigation.NavigationStack);
				Assert.Same(rootPage, navigationPage.Navigation.NavigationStack[0]);
				Assert.Equal(2, tabbedPage.Children.Count);
				Assert.Same(firstTab, tabbedPage.Children[0]);
				Assert.Same(secondTab, tabbedPage.Children[1]);
				Assert.Equal("First", firstTab.Title);
				Assert.Equal("Second", secondTab.Title);

				var platformButton = Assert.IsType<UIButton>(navigateButton.Handler.PlatformView);
				platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await navigationTask.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.True(
					navigationException is null,
					$"Issue29540: custom TabbedViewHandler navigation threw {navigationException?.GetType().FullName}: {navigationException?.Message}");

				var customHandler = Assert.IsType<SwipeTabbedViewHandler>(tabbedPage.Handler);
				Assert.NotNull(customHandler.PlatformView);
				await firstTabAppeared.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.True(didFirstTabAppear, "The first tab did not appear after navigation.");
				Assert.Equal(2, navigationPage.Navigation.NavigationStack.Count);
				Assert.Same(tabbedPage, navigationPage.Navigation.NavigationStack[1]);
			});

			async Task NavigateAsync()
			{
				try
				{
					await navigationPage.PushAsync(tabbedPage);
				}
				catch (NotImplementedException exception)
				{
					navigationException = exception;
				}
			}
		}

		sealed class SwipeTabbedPage : TabbedPage
		{
		}

		sealed class SwipeTabbedViewHandler : TabbedViewHandler
		{
		}
	}
}
#endif

