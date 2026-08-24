using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using NativeAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using WAppBarButton = Microsoft.UI.Xaml.Controls.AppBarButton;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34071")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34071 : ControlsHandlerTestBase
	{
#if WINDOWS
		[Fact]
		public async Task ShellForegroundColorAppliesToToolbarItem()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
				});
			});

			var expectedColor = Colors.Purple.ToWindowsColor();
			var oracleButton = new Button
			{
				Text = "Color oracle",
				TextColor = Colors.Purple
			};

			await CreateHandlerAndAddToWindow<ButtonHandler>(oracleButton, handler =>
			{
				var oracleBrush = Assert.IsType<WSolidColorBrush>(handler.PlatformView.Foreground);
				Assert.Equal(expectedColor, oracleBrush.Color);
			});

			const string toolbarAutomationId = "AffectedToolbarItem";
			ContentPage realizedPage = null;
			WAppBarButton nativeToolbarButton = null;
			int templateRealizationCount = 0;
			int loadedCount = -1;

			var shellContent = new ShellContent
			{
				Title = "Home",
				ContentTemplate = new DataTemplate(() =>
				{
					templateRealizationCount++;
					loadedCount = 0;

					var expectedColorRow = new HorizontalStackLayout
					{
						HorizontalOptions = LayoutOptions.Center,
						Spacing = 10,
						Children =
						{
							new BoxView
							{
								Color = Colors.Purple,
								HeightRequest = 28,
								WidthRequest = 28
							},
							new Label
							{
								Text = "Expected icon color: Purple",
								VerticalTextAlignment = TextAlignment.Center
							}
						}
					};

					var description = new VerticalStackLayout
					{
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center,
						Spacing = 16,
						Children =
						{
							new Label
							{
								Text = "The toolbar icon should use Shell.ForegroundColor.",
								HorizontalTextAlignment = TextAlignment.Center,
								FontSize = 20
							},
							expectedColorRow
						}
					};

					var content = new Grid
					{
						Padding = 24,
						RowSpacing = 20,
						RowDefinitions =
						{
							new RowDefinition(GridLength.Star),
							new RowDefinition(GridLength.Auto),
							new RowDefinition(GridLength.Auto)
						}
					};
					content.Add(description);
					content.Add(new Label
					{
						Text = "Toolbar foreground state",
						HorizontalTextAlignment = TextAlignment.Center,
						FontAttributes = FontAttributes.Bold
					}, 0, 1);
					content.Add(new Button
					{
						Text = "Check toolbar icon color"
					}, 0, 2);

					var toolbarItem = new ToolbarItem
					{
						AutomationId = toolbarAutomationId,
						IconImageSource = "groceries.png",
						Order = ToolbarItemOrder.Primary
					};

					realizedPage = new ContentPage
					{
						Title = "Home",
						Content = content,
						ToolbarItems = { toolbarItem }
					};
					realizedPage.Loaded += (_, _) => loadedCount++;
					return realizedPage;
				})
			};

			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items = { shellContent }
			};
			Shell.SetForegroundColor(shell, Colors.Purple);
			var testWindow = new Microsoft.Maui.Controls.Window(shell)
			{
				Width = 1280,
				Height = 720
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(testWindow, async handler =>
			{
				await AssertHelpers.AssertEventually(() => realizedPage is not null);
				Assert.NotNull(realizedPage);
				await OnLoadedAsync(realizedPage);
				await AssertHelpers.AssertEventually(() => loadedCount > 0);

				Assert.Equal(1, templateRealizationCount);
				Assert.NotNull(shell.Handler);
				Assert.NotNull(shell.Handler.PlatformView);
				Assert.Equal(Colors.Purple, Shell.GetForegroundColor(shell));
				Assert.Equal(FlyoutBehavior.Disabled, shell.FlyoutBehavior);
				Assert.Equal("Home", realizedPage.Title);

				var toolbarItem = Assert.Single(realizedPage.ToolbarItems);
				Assert.Equal(ToolbarItemOrder.Primary, toolbarItem.Order);
				Assert.NotNull(toolbarItem.IconImageSource);

				await AssertHelpers.AssertEventually(() =>
				{
					nativeToolbarButton = GetPlatformToolbar(handler).CommandBar.PrimaryCommands
						.OfType<WAppBarButton>()
						.SingleOrDefault(button => NativeAutomationProperties.GetAutomationId(button) == toolbarAutomationId);
					return nativeToolbarButton is not null;
				});

				Assert.NotNull(nativeToolbarButton);
				Assert.Equal(toolbarItem, nativeToolbarButton.DataContext);
				Assert.Equal(toolbarAutomationId, NativeAutomationProperties.GetAutomationId(nativeToolbarButton));
				Assert.IsType<Microsoft.UI.Xaml.Controls.Image>(nativeToolbarButton.Content);

				var foregroundBrush = Assert.IsType<WSolidColorBrush>(nativeToolbarButton.Foreground);
				var observedColor = foregroundBrush.Color;
				bool colorsMatch =
					Math.Abs(observedColor.R - expectedColor.R) <= 1 &&
					Math.Abs(observedColor.G - expectedColor.G) <= 1 &&
					Math.Abs(observedColor.B - expectedColor.B) <= 1 &&
					Math.Abs(observedColor.A - expectedColor.A) <= 1;

				Assert.True(
					colorsMatch,
					$"Windows Shell toolbar foreground mismatch: observed RGBA ({observedColor.R}, {observedColor.G}, {observedColor.B}, {observedColor.A}), expected RGBA ({expectedColor.R}, {expectedColor.G}, {expectedColor.B}, {expectedColor.A}).");
			});
		}
#endif
	}
}

