#if WINDOWS
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WNavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27494")]
	public class Issue27494 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RuntimeAddedShellContentTitleUpdatesNativeTab()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.SetupShellHandlers();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var homeContent = new ShellContent
			{
				Title = "Home",
				Content = new ContentPage
				{
					Content = new ScrollView
					{
						Content = new VerticalStackLayout
						{
							Padding = 24,
							Spacing = 14,
							Children =
							{
								new Label { Text = "Runtime-added ShellContent title", FontSize = 22, FontAttributes = FontAttributes.Bold },
								new HorizontalStackLayout
								{
									Spacing = 6,
									Children =
									{
										new Label { Text = "First title property:", FontAttributes = FontAttributes.Bold },
										new Label { Text = "Home" }
									}
								},
								new HorizontalStackLayout
								{
									Spacing = 6,
									Children =
									{
										new Label { Text = "New title property:", FontAttributes = FontAttributes.Bold },
										new Label { Text = "Not added" }
									}
								},
								new Button { Text = "Change Title" },
								new Button { Text = "Add New Tab" },
								new Button { Text = "Update New Tab Title" },
								new Button { Text = "Check Rendered Tab Title" }
							}
						}
					}
				}
			};
			var settingsContent = CreateCenteredContent("Settings", "This is the Settings page.");
			var profileContent = CreateCenteredContent("Profile", "This is the Profile page.");
			var issueTab = new Tab
			{
				Title = "Nested Tabs",
				Items =
				{
					homeContent,
					settingsContent,
					profileContent
				}
			};
			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items =
				{
					new TabBar
					{
						Items = { issueTab }
					}
				}
			};

			await CreateHandlerAndAddToWindow<ShellHandler>(shell, async handler =>
			{
				Assert.NotNull(handler.PlatformView);
				Assert.NotNull(shell.CurrentItem);
				Assert.NotNull(shell.CurrentItem.Handler);

				var navigationView = shell.CurrentItem.Handler.PlatformView as MauiNavigationView;
				Assert.NotNull(navigationView);
				await AssertEventually(() => navigationView.IsLoaded);
				Assert.Equal(WNavigationViewPaneDisplayMode.Top, navigationView.PaneDisplayMode);

				NavigationViewItemViewModel homeNativeItem = null;
				await AssertEventually(() =>
				{
					var nativeItems = GetShellContentItems(navigationView);
					homeNativeItem = nativeItems.SingleOrDefault(item => ReferenceEquals(item.Data, homeContent));
					return nativeItems.Count == 3 && homeNativeItem is not null;
				});

				homeContent.Title = "Updated";
				await AssertEventually(() => Equals(homeNativeItem.Content, "Updated"));
				Assert.Equal("Updated", homeNativeItem.Content);

				var dynamicContent = CreateCenteredContent("New Tab", "This is a dynamically added tab.");
				issueTab.Items.Add(dynamicContent);

				NavigationViewItemViewModel dynamicNativeItem = null;
				await AssertEventually(() =>
				{
					var nativeItems = GetShellContentItems(navigationView);
					dynamicNativeItem = nativeItems.SingleOrDefault(item => ReferenceEquals(item.Data, dynamicContent));
					return nativeItems.Count == 4 && dynamicNativeItem is not null;
				});
				Assert.Same(dynamicContent, dynamicNativeItem.Data);
				Assert.Equal("New Tab", dynamicNativeItem.Content);

				string changedProperty = null;
				dynamicContent.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == nameof(ShellContent.Title))
						changedProperty = args.PropertyName;
				};

				dynamicContent.Title = "Updated Title";

				string observedNativeTitle = null;
				await AssertEventually(() =>
				{
					observedNativeTitle = dynamicNativeItem.Content as string;
					return changedProperty == nameof(ShellContent.Title) && observedNativeTitle is not null;
				});
				Assert.Equal(nameof(ShellContent.Title), changedProperty);
				Assert.Equal("Updated Title", dynamicContent.Title);
				Assert.True(
					observedNativeTitle == "Updated Title",
					$"Runtime-added ShellContent native tab title did not update. Observed: '{observedNativeTitle}'; Expected: 'Updated Title'.");
			});
		}

		static ShellContent CreateCenteredContent(string title, string text) =>
			new ShellContent
			{
				Title = title,
				Content = new ContentPage
				{
					Content = new Label
					{
						Text = text,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center
					}
				}
			};

		static List<NavigationViewItemViewModel> GetShellContentItems(MauiNavigationView navigationView)
		{
			var result = new List<NavigationViewItemViewModel>();
			if (navigationView.MenuItemsSource is IEnumerable<NavigationViewItemViewModel> items)
				AddShellContentItems(items, result);

			return result;
		}

		static void AddShellContentItems(
			IEnumerable<NavigationViewItemViewModel> items,
			List<NavigationViewItemViewModel> result)
		{
			foreach (var item in items)
			{
				if (item.Data is ShellContent)
					result.Add(item);

				if (item.MenuItemsSource is not null)
					AddShellContentItems(item.MenuItemsSource, result);
			}
		}
	}
}
#endif

