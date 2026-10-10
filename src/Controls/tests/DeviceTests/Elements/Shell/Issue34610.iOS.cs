using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category(TestCategory.Shell)]
	[Category("Issue34610")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34610 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task TitleViewFillsWindowWidth()
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				EnsureHandlerCreated(builder =>
				{
					builder.ConfigureMauiHandlers(handlers =>
					{
						handlers.SetupShellHandlers();
						handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
						handlers.AddHandler<BoxView, BoxViewHandler>();
					});
				});

				var menuLabel = new Label
				{
					Text = "☰",
					FontSize = 24,
					TextColor = Colors.White,
					VerticalOptions = LayoutOptions.Center,
					Margin = new Thickness(10, 0)
				};
				var titleLabel = new Label
				{
					Text = "MY APP TITLE",
					TextColor = Colors.White,
					FontSize = 16,
					FontAttributes = FontAttributes.Bold,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				var settingsLabel = new Label
				{
					Text = "⚙",
					FontSize = 24,
					TextColor = Colors.White,
					VerticalOptions = LayoutOptions.Center,
					Margin = new Thickness(10, 0)
				};
				Grid.SetColumn(titleLabel, 1);
				Grid.SetColumn(settingsLabel, 2);

				var titleView = new Grid
				{
					BackgroundColor = Colors.Red,
					Padding = 0,
					Margin = 0,
					ColumnSpacing = 0,
					ColumnDefinitions = new ColumnDefinitionCollection
					{
						new ColumnDefinition(GridLength.Auto),
						new ColumnDefinition(GridLength.Star),
						new ColumnDefinition(GridLength.Auto)
					}
				};
				titleView.Children.Add(menuLabel);
				titleView.Children.Add(titleLabel);
				titleView.Children.Add(settingsLabel);

				var contentBox = new BoxView
				{
					Color = Colors.DodgerBlue
				};
				var page = new ContentPage
				{
					Padding = 0,
					Content = contentBox
				};
				Shell.SetNavBarHasShadow(page, false);
				Shell.SetTitleView(page, titleView);

				var shellContent = new ShellContent
				{
					Route = "Issue34610",
					Content = page
				};
				var shell = new Shell
				{
					FlyoutBehavior = FlyoutBehavior.Disabled,
					CurrentItem = shellContent
				};

				bool pageLoaded = false;
				var loadedCompletion = new TaskCompletionSource<bool>();
				page.Loaded += (_, _) =>
				{
					pageLoaded = true;
					loadedCompletion.TrySetResult(true);
				};

				await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async _ =>
				{
					await loadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));
					Assert.True(pageLoaded, "The Shell ContentPage did not reach its Loaded transition.");

					Assert.NotNull(titleView.Handler);
					var titlePlatformView = (titleView.Handler as IPlatformViewHandler)?.PlatformView as UIView;
					Assert.NotNull(titlePlatformView);

					Assert.NotNull(contentBox.Handler);
					var contentPlatformView = (contentBox.Handler as IPlatformViewHandler)?.PlatformView as UIView;
					Assert.NotNull(contentPlatformView);

					await AssertEventually(
						() => titlePlatformView.Frame.Width > 0 &&
							titlePlatformView.Frame.Height > 0 &&
							contentPlatformView.Frame.Width > 0 &&
							contentPlatformView.Frame.Height > 0 &&
							titlePlatformView.Window is not null &&
							contentPlatformView.Window is not null,
						message: "The Shell TitleView and page content did not receive non-empty native frames.");

					var rootWindow = titlePlatformView.Window;
					Assert.NotNull(rootWindow);
					Assert.Same(rootWindow, contentPlatformView.Window);

					Assert.Same(titleView, Shell.GetTitleView(page));
					Assert.Same(menuLabel, titleView.Children[0]);
					Assert.Same(titleLabel, titleView.Children[1]);
					Assert.Same(settingsLabel, titleView.Children[2]);
					Assert.Equal("☰", menuLabel.Text);
					Assert.Equal("MY APP TITLE", titleLabel.Text);
					Assert.Equal("⚙", settingsLabel.Text);
					Assert.Equal(0, Grid.GetColumn(menuLabel));
					Assert.Equal(1, Grid.GetColumn(titleLabel));
					Assert.Equal(2, Grid.GetColumn(settingsLabel));

					const double expectedLeftEdge = 0;
					const double tolerance = 1;
					var contentFrame = contentPlatformView.ConvertRectToView(contentPlatformView.Bounds, rootWindow);
					Assert.True(
						Math.Abs(contentFrame.X - expectedLeftEdge) <= tolerance,
						$"The fill BoxView did not establish the zero-inset coordinate oracle. Its native left edge was {contentFrame.X:F1}; expected {expectedLeftEdge:F1} with tolerance {tolerance:F1}.");

					var titleFrame = titlePlatformView.ConvertRectToView(titlePlatformView.Bounds, rootWindow);
					Assert.True(
						Math.Abs(titleFrame.X - expectedLeftEdge) <= tolerance,
						$"Shell TitleView did not fill the iOS window width. Its native left edge was {titleFrame.X:F1}; expected {expectedLeftEdge:F1} with tolerance {tolerance:F1}.");
				});
			});
		}
	}
#endif
}

