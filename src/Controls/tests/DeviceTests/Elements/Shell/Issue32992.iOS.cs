#if IOS && !MACCATALYST
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using ShellRenderer = Microsoft.Maui.Controls.Handlers.Compatibility.ShellRenderer;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	[Category("Issue32992")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue32992 : ControlsHandlerTestBase
	{
		const double ColorTolerance = 0.01;

		[Fact]
		public async Task NullTabBarBackgroundRestoresPlatformDefault()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers => SetupShellHandlers(handlers));
			});

			int applyClickCount = -1;
			int removeClickCount = -1;
			int propertyChangeToken = int.MinValue;
			int dispatcherToken = int.MinValue;
			var dispatcherCompleted = new TaskCompletionSource<bool>();

			var applyButton = new Button { Text = "Apply TabBar Color" };
			var removeButton = new Button { Text = "Remove TabBar Color" };
			var resultLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				Text = "NO BUG:"
			};
			var colorStateLabel = new Label { Text = "Tab bar color request: null (platform default)" };
			var shell = new Shell { FlyoutBehavior = FlyoutBehavior.Disabled };

			applyButton.Clicked += (_, _) =>
			{
				applyClickCount++;
				Shell.SetTabBarBackgroundColor(shell, Colors.LightBlue);
				colorStateLabel.Text = "Tab bar color request: LightBlue";
			};

			removeButton.Clicked += (_, _) =>
			{
				removeClickCount++;
				shell.SetValue(Shell.TabBarBackgroundColorProperty, null);
				colorStateLabel.Text = "Tab bar color request: null (platform default)";
				shell.Dispatcher.Dispatch(() =>
				{
					dispatcherToken = 1;
					dispatcherCompleted.TrySetResult(true);
				});
			};

			shell.PropertyChanged += OnShellPropertyChanged;

			var controlsPage = new ContentPage
			{
				Title = "Tab color reset",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label
						{
							FontAttributes = FontAttributes.Bold,
							FontSize = 22,
							Text = "Shell TabBarBackgroundColor reset"
						},
						new Label
						{
							Text = "Apply LightBlue, then remove the color. The tab bar should return to its platform default background."
						},
						resultLabel,
						colorStateLabel,
						applyButton,
						removeButton
					}
				}
			};

			var firstTab = new Tab { Title = "First" };
			firstTab.Items.Add(new ShellContent { Title = "First", Content = controlsPage });

			var secondTab = new Tab { Title = "Second" };
			secondTab.Items.Add(new ShellContent
			{
				Title = "Second",
				Content = new ContentPage
				{
					Title = "Second",
					Content = new Label
					{
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center,
						Text = "Second tab"
					}
				}
			});

			var tabBarItem = new TabBar();
			tabBarItem.Items.Add(firstTab);
			tabBarItem.Items.Add(secondTab);
			shell.Items.Add(tabBarItem);

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async _ =>
			{
				await OnFrameSetToNotEmpty(controlsPage.Content);

				var tabBarContainer = ((IPlatformViewHandler)controlsPage.Handler)
					.PlatformView.FindParent(view => view.NextResponder is UITabBarController);
				Assert.NotNull(tabBarContainer);

				var tabBarController = Assert.IsAssignableFrom<UITabBarController>(tabBarContainer.NextResponder);
				var nativeTabBar = tabBarController.TabBar;
				Assert.True(nativeTabBar.Bounds.Width > 0 && nativeTabBar.Bounds.Height > 0);
				Assert.Equal(2, nativeTabBar.Items?.Length);
				Assert.Equal(new[] { "First", "Second" }, nativeTabBar.Items.Select(item => item.Title));

				var initialBackground = GetEffectiveBackground(nativeTabBar);

				Tap(applyButton);
				Assert.Equal(0, applyClickCount);
				await AssertionExtensions.AssertEventually(
					() => ColorsMatch(GetEffectiveBackground(nativeTabBar), Colors.LightBlue.ToPlatform()),
					timeout: 2000,
					message: "The Apply action did not reach the native iOS tab bar.");
				Assert.False(ColorsMatch(initialBackground, Colors.LightBlue.ToPlatform()),
					"The active platform default must be distinguishable from the requested LightBlue color.");

				Tap(removeButton);
				Assert.Equal(0, removeClickCount);
				Assert.Null(Shell.GetTabBarBackgroundColor(shell));
				Assert.Equal(1, propertyChangeToken);

				await dispatcherCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
				Assert.Equal(1, dispatcherToken);

				var observedBackground = GetEffectiveBackground(nativeTabBar);
				await AssertionExtensions.AssertEventually(
					() => ColorsMatch(GetEffectiveBackground(nativeTabBar), initialBackground),
					timeout: 2000,
					message: $"iOS Shell tab bar background remained stale after null reset: observed {Describe(observedBackground)}; expected {Describe(initialBackground)}");
			});

			void OnShellPropertyChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == Shell.TabBarBackgroundColorProperty.PropertyName &&
					Shell.GetTabBarBackgroundColor(shell) is null)
				{
					propertyChangeToken = 1;
				}
			}
		}

		static UIColor GetEffectiveBackground(UITabBar tabBar) =>
			tabBar.StandardAppearance.BackgroundColor ?? tabBar.BarTintColor;

		static void Tap(Button button)
		{
			var handler = Assert.IsType<ButtonHandler>(button.Handler);
			handler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);
		}

		static bool ColorsMatch(UIColor first, UIColor second) =>
			ColorComparison.ARGBEquivalent(first, second, ColorTolerance);

		static string Describe(UIColor color)
		{
			if (color is null)
				return "platform-default(null)";

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return FormattableString.Invariant($"rgba({red:F3},{green:F3},{blue:F3},{alpha:F3})");
		}
	}
}
#endif
