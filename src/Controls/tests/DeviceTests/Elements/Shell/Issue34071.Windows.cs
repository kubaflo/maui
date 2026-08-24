#if WINDOWS
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Xunit;
using WAppBarButton = Microsoft.UI.Xaml.Controls.AppBarButton;
using WAutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34071")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34071 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ShellForegroundColorAppliesToToolbarItem()
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

			ContentPage contentPage = null;
			VerticalStackLayout contentLayout = null;
			BoxView colorOracle = null;
			ToolbarItem toolbarItem = null;

			var shellContent = new ShellContent
			{
				Title = "Home",
				ContentTemplate = new DataTemplate(() =>
				{
					toolbarItem = new ToolbarItem
					{
						AutomationId = "AffectedToolbarItem",
						IconImageSource = new FontImageSource
						{
							Glyph = "X",
							Size = 20
						}
					};

					colorOracle = new BoxView
					{
						Color = Colors.Purple,
						HeightRequest = 24,
						WidthRequest = 24
					};

					contentLayout = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Center,
						Children =
						{
							new Label
							{
								Text = "The toolbar icon above should use Shell.ForegroundColor.",
								HorizontalTextAlignment = TextAlignment.Center
							},
							new HorizontalStackLayout
							{
								Spacing = 8,
								HorizontalOptions = LayoutOptions.Center,
								Children =
								{
									colorOracle,
									new Label
									{
										Text = "Expected toolbar foreground: Purple",
										VerticalTextAlignment = TextAlignment.Center
									}
								}
							},
							new Label
							{
								AutomationId = "ForegroundDescription",
								Text = "Toolbar foreground is configured by Shell.ForegroundColor.",
								FontAttributes = FontAttributes.Bold,
								HorizontalTextAlignment = TextAlignment.Center
							},
							new Button
							{
								AutomationId = "CheckForegroundButton",
								Text = "Check toolbar foreground"
							}
						}
					};

					contentPage = new ContentPage
					{
						Title = "Home",
						Content = contentLayout
					};
					contentPage.ToolbarItems.Add(toolbarItem);
					return contentPage;
				})
			};

			var shell = new Shell
			{
				FlyoutBehavior = FlyoutBehavior.Disabled,
				Items = { shellContent }
			};
			Shell.SetForegroundColor(shell, Colors.Purple);

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(
				new Microsoft.Maui.Controls.Window(shell),
				async handler =>
				{
					await AssertEventually(
						() => contentPage is not null &&
							contentLayout is not null &&
							colorOracle is not null &&
							toolbarItem is not null &&
							contentPage.Handler is not null &&
							colorOracle.Handler is not null,
						timeout: 5000,
						message: "Issue34071 page hierarchy did not materialize after Shell attachment.");

					Assert.NotNull(contentPage);
					Assert.NotNull(contentLayout);
					Assert.NotNull(colorOracle);
					Assert.NotNull(toolbarItem);
					Assert.Equal(Colors.Purple, Shell.GetForegroundColor(shell));
					Assert.Equal("AffectedToolbarItem", toolbarItem.AutomationId);
					Assert.True(string.IsNullOrEmpty(toolbarItem.Text));
					var iconSource = Assert.IsType<FontImageSource>(toolbarItem.IconImageSource);
					Assert.Equal("X", iconSource.Glyph);
					Assert.True(string.IsNullOrEmpty(iconSource.FontFamily));

					await OnFrameSetToNotEmpty(contentLayout);
					Assert.True(contentLayout.Width > 0 && contentLayout.Height > 0);

					var nativeBoxView = Assert.IsAssignableFrom<WFrameworkElement>(colorOracle.Handler.PlatformView);
					await nativeBoxView.AssertContainsColor(Colors.Purple, handler.MauiContext);

					WAppBarButton nativeToolbarItem = null;
					await AssertEventually(
						() =>
						{
							var commandBar = GetPlatformToolbar(handler)?.CommandBar;
							if (commandBar is null)
								return false;

							nativeToolbarItem = commandBar.PrimaryCommands
								.OfType<WAppBarButton>()
								.SingleOrDefault(button =>
									WAutomationProperties.GetAutomationId(button) == "AffectedToolbarItem");
							return nativeToolbarItem is not null;
						},
						timeout: 5000,
						message: "Issue34071 native toolbar item did not materialize after Shell attachment.");

					Assert.NotNull(nativeToolbarItem);
					Assert.Same(toolbarItem, nativeToolbarItem.DataContext);
					Assert.Equal("AffectedToolbarItem", WAutomationProperties.GetAutomationId(nativeToolbarItem));

					var observedA = -1;
					var observedR = -1;
					var observedG = -1;
					var observedB = -1;
					await AssertEventually(
						() =>
						{
							if (nativeToolbarItem.Foreground is not WSolidColorBrush foregroundBrush)
								return false;

							observedA = foregroundBrush.Color.A;
							observedR = foregroundBrush.Color.R;
							observedG = foregroundBrush.Color.G;
							observedB = foregroundBrush.Color.B;
							return true;
						},
						timeout: 5000,
						message: "Issue34071 toolbar foreground was not a solid color brush.");

					Assert.True(
						observedA == 255 && observedR == 128 && observedG == 0 && observedB == 128,
						$"Issue34071 toolbar foreground mismatch after Shell attachment: expected ARGB 255,128,0,128; observed ARGB {observedA},{observedR},{observedG},{observedB}.");
				});
		}
	}
}
#endif

