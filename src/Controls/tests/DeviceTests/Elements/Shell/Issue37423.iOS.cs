using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category("Issue37423")]
	public class Issue37423 : ControlsHandlerTestBase
	{
		const double ColorTolerance = 0.01;

		[Fact]
		public async Task ShellAppearancePreservesNavigationTabbedPageLiquidGlassBackground()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(26))
				return;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<NavigationPage, NavigationRenderer>();
					handlers.AddHandler<TabbedPage, TabbedRenderer>();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var referenceTabbedPage = new TabbedPage
			{
				Children =
				{
					new NavigationPage(new ContentPage { Title = "First" }) { Title = "First" },
					new NavigationPage(new ContentPage { Title = "Second" }) { Title = "Second" }
				}
			};
			UITabBar referenceTabBar = null;
			var expected = (Red: double.NaN, Green: double.NaN, Blue: double.NaN, Alpha: double.NaN);

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(referenceTabbedPage), async _ =>
			{
				await AssertEventually(() =>
				{
					referenceTabBar = FindAttachedTabBar(referenceTabbedPage.CurrentPage);
					return IsMeasuredInWindow(referenceTabBar);
				});

				Assert.NotNull(referenceTabBar);
				expected = GetRgba(referenceTabBar.BackgroundColor);
			});

			Assert.True(Math.Abs(expected.Alpha) <= ColorTolerance,
				$"The iOS 26 NavigationPage/TabbedPage system tab bar should be transparent, but was {Format(expected)}.");

			var shell = CreateConfiguredShell();
			var appearanceObserver = new AppearanceObserver();
			bool appeared = false;
			shell.CurrentPage.Appearing += (_, _) => appeared = true;
			UITabBar shellTabBar = null;
			ShellAppearance appliedAppearance = null;
			var observed = (Red: double.NaN, Green: double.NaN, Blue: double.NaN, Alpha: double.NaN);

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async _ =>
			{
				((IShellController)shell).AddAppearanceObserver(appearanceObserver, shell.CurrentPage);

				await AssertEventually(() =>
				{
					shellTabBar = FindAttachedTabBar(shell.CurrentPage);
					appliedAppearance = appearanceObserver.AppliedAppearance;
					return appeared && appliedAppearance is not null && IsMeasuredInWindow(shellTabBar);
				});

				Assert.True(appeared, "The configured Shell page did not appear.");
				Assert.NotNull(appliedAppearance);
				Assert.Equal(Color.FromArgb("#FEFCF9"), appliedAppearance.TabBarBackgroundColor);
				Assert.Equal(Color.FromArgb("#28599A"), appliedAppearance.TabBarTitleColor);
				Assert.Equal(Color.FromArgb("#6F6F6F"), appliedAppearance.TabBarUnselectedColor);
				Assert.NotNull(shellTabBar);
				observed = GetRgba(shellTabBar.BackgroundColor);
			});

			bool matchesSystemDefault =
				Math.Abs(observed.Red - expected.Red) <= ColorTolerance &&
				Math.Abs(observed.Green - expected.Green) <= ColorTolerance &&
				Math.Abs(observed.Blue - expected.Blue) <= ColorTolerance &&
				Math.Abs(observed.Alpha - expected.Alpha) <= ColorTolerance;

			Assert.True(
				Math.Abs(observed.Alpha) <= ColorTolerance && matchesSystemDefault,
				$"Issue37423: iOS 26 Shell tab bar added an opaque native background. Observed {Format(observed)}; expected system default {Format(expected)}.");
		}

		static Shell CreateConfiguredShell()
		{
			var shell = new Shell
			{
				Items =
				{
					new TabBar
					{
						Items =
						{
							new Tab
							{
								Title = "First",
								Items = { new ShellContent { Title = "First", Content = new ContentPage() } }
							},
							new Tab
							{
								Title = "Second",
								Items = { new ShellContent { Title = "Second", Content = new ContentPage() } }
							}
						}
					}
				}
			};

			Shell.SetTabBarBackgroundColor(shell, Color.FromArgb("#FEFCF9"));
			Shell.SetTabBarTitleColor(shell, Color.FromArgb("#28599A"));
			Shell.SetTabBarUnselectedColor(shell, Color.FromArgb("#6F6F6F"));
			return shell;
		}

		static UITabBar FindAttachedTabBar(Page currentPage)
		{
			if (currentPage.Handler is not IPlatformViewHandler pageHandler)
				return null;

			var container = pageHandler.PlatformView.FindParent(view => view.NextResponder is UITabBarController);
			return container?.NextResponder is UITabBarController controller ? controller.TabBar : null;
		}

		static bool IsMeasuredInWindow(UITabBar tabBar) =>
			tabBar is not null &&
			tabBar.Window is not null &&
			tabBar.Frame.Width > 0 &&
			tabBar.Frame.Height > 0;

		static (double Red, double Green, double Blue, double Alpha) GetRgba(UIColor color)
		{
			if (color is null)
				return (0, 0, 0, 0);

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return ((double)red, (double)green, (double)blue, (double)alpha);
		}

		static string Format((double Red, double Green, double Blue, double Alpha) rgba) =>
			$"RGBA({rgba.Red:F3}, {rgba.Green:F3}, {rgba.Blue:F3}, {rgba.Alpha:F3})";

		sealed class AppearanceObserver : IAppearanceObserver
		{
			public ShellAppearance AppliedAppearance { get; private set; }

			public void OnAppearanceChanged(ShellAppearance appearance) =>
				AppliedAppearance = appearance;
		}
	}
#endif
}

