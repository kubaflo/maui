#if MACCATALYST
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue35516")]
	[Category(TestCategory.Shell)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35516 : ControlsHandlerTestBase
	{
		const string ExpectedQuery = "Hello World";

		[Fact]
		public async Task QueryChangeUpdatesAttachedNativeSearchBar()
		{
			if (!OperatingSystem.IsMacCatalystVersionAtLeast(26))
				return;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var searchHandler = new SearchHandler();
			var enterTextButton = new Button
			{
				Text = "Enter Text",
				HorizontalOptions = LayoutOptions.Center
			};
			enterTextButton.Clicked += (_, _) => searchHandler.Query = ExpectedQuery;
			var managedQueryLabel = new Label
			{
				Text = "Managed Query: Empty",
				HorizontalTextAlignment = TextAlignment.Center
			};
			var expectedBehaviorLabel = new Label
			{
				Text = "The search box should display the managed query.",
				FontSize = 20,
				HorizontalTextAlignment = TextAlignment.Center
			};

			var contentPage = new ContentPage
			{
				Title = "Search Query Test",
				Content = new VerticalStackLayout
				{
					Padding = new Thickness(24),
					Spacing = 16,
					VerticalOptions = LayoutOptions.Center,
					Children =
					{
						new Label
						{
							Text = "Shell SearchHandler is ready",
							HorizontalTextAlignment = TextAlignment.Center
						},
						new Label
						{
							Text = "Expected search box text after trigger: Hello World",
							HorizontalTextAlignment = TextAlignment.Center
						},
						enterTextButton,
						managedQueryLabel,
						expectedBehaviorLabel
					}
				}
			};
			Shell.SetSearchHandler(contentPage, searchHandler);

			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items =
				{
					new ShellContent
					{
						Title = "Search Query Test",
						Content = contentPage
					}
				}
			};

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async handler =>
			{
				var shellContext = (IShellContext)handler;
				var shellItemRenderer = Assert.IsType<ShellItemRenderer>(shellContext.CurrentShellItemRenderer);
				var sectionRenderer = Assert.IsType<ShellSectionRenderer>(shellItemRenderer.CurrentRenderer);
				var navigationItem = Assert.IsAssignableFrom<UINavigationItem>(sectionRenderer.TopViewController.NavigationItem);
				var searchController = Assert.IsAssignableFrom<UISearchController>(navigationItem.SearchController);
				var searchBar = Assert.IsAssignableFrom<UISearchBar>(searchController.SearchBar);

				await AssertEventually(
					() => searchBar.Window is not null,
					message: "The native Shell search bar was not attached to a window.");
				Assert.True(string.IsNullOrEmpty(searchBar.Text), $"Native Shell search text started as '{searchBar.Text}'.");

				var buttonHandler = Assert.IsAssignableFrom<ButtonHandler>(enterTextButton.Handler);
				var nativeButton = buttonHandler.PlatformView;
				Assert.NotNull(nativeButton.Window);

				var observedQuery = "<query callback not observed>";
				var callbackCount = 0;
				var queryChanged = new TaskCompletionSource<bool>();
				searchHandler.PropertyChanged += OnSearchHandlerPropertyChanged;

				nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await queryChanged.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.Equal(1, callbackCount);
				Assert.Equal(ExpectedQuery, observedQuery);
				Assert.Equal(ExpectedQuery, searchHandler.Query);
				Assert.Same(searchController, navigationItem.SearchController);
				Assert.NotNull(searchBar.Window);

				await AssertEventually(
					() => searchBar.Text == ExpectedQuery,
					message: "Native Shell search text was '<empty>'; expected 'Hello World' after SearchHandler.Query changed.");

				void OnSearchHandlerPropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName != SearchHandler.QueryProperty.PropertyName)
						return;

					callbackCount++;
					observedQuery = searchHandler.Query;
					managedQueryLabel.Text = "Managed Query: Hello World";
					queryChanged.TrySetResult(true);
				}
			});
		}
	}
}
#endif

