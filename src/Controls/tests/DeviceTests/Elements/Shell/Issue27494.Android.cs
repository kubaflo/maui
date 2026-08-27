using System.Threading.Tasks;
using Google.Android.Material.Tabs;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using ShellRenderer = Microsoft.Maui.Controls.Handlers.Compatibility.ShellRenderer;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue27494")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue27494 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RuntimeAddedShellContentTitleUpdatesNativeTab()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler(typeof(Microsoft.Maui.Controls.Window), typeof(WindowHandlerStub));
					handlers.AddHandler(typeof(Page), typeof(PageHandler));
					handlers.AddHandler(typeof(Layout), typeof(LayoutHandler));
					handlers.AddHandler(typeof(Label), typeof(LabelHandler));
					handlers.AddHandler(typeof(Button), typeof(ButtonHandler));
					handlers.AddHandler(typeof(ScrollView), typeof(ScrollViewHandler));
				});
			});

			var homeContent = CreateShellContent("Home");
			var settingsContent = CreateShellContent("Settings");
			var profileContent = CreateShellContent("Profile");
			var rootTab = new Tab
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
						Items =
						{
							rootTab
						}
					}
				}
			};

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async handler =>
			{
				var shellContext = Assert.IsAssignableFrom<IShellContext>(handler);
				var tabLayout = shellContext.CurrentDrawerLayout.GetFirstChildOfType<TabLayout>();
				Assert.NotNull(tabLayout);

				await AssertEventually(() => tabLayout.TabCount == 3);
				Assert.Same(homeContent, rootTab.Items[0]);
				Assert.Same(settingsContent, rootTab.Items[1]);
				Assert.Same(profileContent, rootTab.Items[2]);
				Assert.Equal("Home", GetNativeTabTitle(tabLayout, 0));
				Assert.Equal("Settings", GetNativeTabTitle(tabLayout, 1));
				Assert.Equal("Profile", GetNativeTabTitle(tabLayout, 2));

				homeContent.Title = "Updated";
				await AssertEventually(() => GetNativeTabTitle(tabLayout, 0) == "Updated");
				Assert.Equal("Updated", GetNativeTabTitle(tabLayout, 0));

				var runtimeContent = CreateShellContent("New Tab");
				rootTab.Items.Add(runtimeContent);

				await AssertEventually(() => tabLayout.TabCount == 4);
				Assert.Same(runtimeContent, rootTab.Items[3]);
				Assert.Equal("New Tab", GetNativeTabTitle(tabLayout, 3));

				var observedTitle = "<title change not observed>";
				runtimeContent.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == ShellContent.TitleProperty.PropertyName)
						observedTitle = runtimeContent.Title;
				};

				runtimeContent.Title = "Updated Title";

				Assert.Equal("Updated Title", observedTitle);
				Assert.Equal("Updated Title", runtimeContent.Title);
				await AssertEventually(
					() => GetNativeTabTitle(tabLayout, 3) == "Updated Title",
					message: "Runtime-added ShellContent native tab title was 'New Tab'; expected 'Updated Title'.");
			});
		}

		static ShellContent CreateShellContent(string title) =>
			new ShellContent
			{
				Title = title,
				ContentTemplate = new DataTemplate(CreateContentPage)
			};

		static ContentPage CreateContentPage() =>
			new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 12,
						Children =
						{
							new Label { Text = "ShellContent title reproduction", FontSize = 20 },
							new Button { Text = "Change Title" },
							new Button { Text = "Add New Tab" },
							new Button { Text = "Update New Tab Title" },
							new Label { Text = "Model title: not added" },
							new Button { Text = "Record Stale Title" }
						}
					}
				}
			};

		static string GetNativeTabTitle(TabLayout tabLayout, int index)
		{
			var tab = tabLayout.GetTabAt(index);
			Assert.NotNull(tab);
			var tabView = Assert.IsAssignableFrom<global::Android.Views.ViewGroup>(tab.View);
			var textView = tabView.GetFirstChildOfType<global::Android.Widget.TextView>();
			Assert.NotNull(textView);
			Assert.NotNull(textView.Text);
			return textView.Text;
		}
	}
}

