#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Compatibility;
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
	[Category(TestCategory.Shell)]
	[Category("Issue34610")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34610Tests : ControlsHandlerTestBase
	{
		[Fact]
		public async Task TitleViewFillsNavigationBarWithoutInsetsOrContentGap()
		{
			const double tolerance = 1;

			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					SetupShellHandlers(handlers);
					handlers.AddHandler<Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<Shell, ShellRenderer>();
				});
			});

			var referenceTitleGrid = CreateTitleGrid(out _, out _, out _);
			var referenceContentBox = new BoxView { Color = Colors.DodgerBlue };
			var referenceLayout = new Grid
			{
				RowSpacing = 0,
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			Grid.SetRow(referenceContentBox, 1);
			referenceLayout.Add(referenceTitleGrid);
			referenceLayout.Add(referenceContentBox);

			await CreateHandlerAndAddToWindow<LayoutHandler>(referenceLayout, async handler =>
			{
				await OnFrameSetToNotEmpty(referenceTitleGrid);
				await OnFrameSetToNotEmpty(referenceContentBox);
				await AssertEventually(() =>
				{
					var titleView = referenceTitleGrid.ToPlatform();
					var contentView = referenceContentBox.ToPlatform();
					return titleView.Window is not null &&
						contentView.Window is not null &&
						titleView.Bounds.Width > 0 &&
						titleView.Bounds.Height > 0 &&
						contentView.Bounds.Width > 0 &&
						contentView.Bounds.Height > 0;
				});

				UIView rootView = handler.PlatformView;
				UIView titleView = referenceTitleGrid.ToPlatform();
				UIView contentView = referenceContentBox.ToPlatform();
				var rootFrame = rootView.ConvertRectToView(rootView.Bounds, null);
				var titleFrame = titleView.ConvertRectToView(titleView.Bounds, null);
				var contentFrame = contentView.ConvertRectToView(contentView.Bounds, null);
				double leftInset = titleFrame.Left - rootFrame.Left;
				double rightInset = rootFrame.Right - titleFrame.Right;
				double verticalGap = contentFrame.Top - titleFrame.Bottom;

				Assert.True(Math.Abs(leftInset) <= tolerance && Math.Abs(rightInset) <= tolerance,
					$"Reference TitleView horizontal insets were left={leftInset:F2}, right={rightInset:F2}; expected 0 +/- {tolerance:F2}.");
				Assert.True(Math.Abs(verticalGap) <= tolerance,
					$"Reference TitleView vertical gap was {verticalGap:F2}; expected 0 +/- {tolerance:F2}.");
			});

			var titleGrid = CreateTitleGrid(out var menuLabel, out var titleLabel, out var settingsLabel);
			var contentBox = new BoxView { Color = Colors.DodgerBlue };
			var page = new ContentPage
			{
				Padding = 0,
				Content = contentBox
			};
			Shell.SetNavBarHasShadow(page, false);
			Shell.SetTitleView(page, titleGrid);

			var shell = new Shell
			{
				Items =
				{
					new ShellContent { Content = page }
				}
			};

			bool layoutCompleted = false;
			double horizontalInset = double.NaN;
			double verticalContentGap = double.NaN;

			await CreateHandlerAndAddToWindow<ShellRenderer>(shell, async handler =>
			{
				await OnFrameSetToNotEmpty(page);
				await OnFrameSetToNotEmpty(titleGrid);
				await OnFrameSetToNotEmpty(contentBox);
				await AssertEventually(() =>
				{
					var titleView = titleGrid.ToPlatform();
					var boxView = contentBox.ToPlatform();
					var navigationBar = GetPlatformToolbar(handler);
					return titleView.Window is not null &&
						boxView.Window is not null &&
						navigationBar?.Window is not null &&
						titleView.Bounds.Width > 0 &&
						titleView.Bounds.Height > 0 &&
						boxView.Bounds.Width > 0 &&
						boxView.Bounds.Height > 0 &&
						navigationBar.Bounds.Width > 0 &&
						navigationBar.Bounds.Height > 0;
				});

				Assert.Equal("☰", menuLabel.Text);
				Assert.Equal("MY APP TITLE", titleLabel.Text);
				Assert.Equal("⚙", settingsLabel.Text);
				Assert.Equal(24d, menuLabel.FontSize);
				Assert.Equal(16d, titleLabel.FontSize);
				Assert.Equal(FontAttributes.Bold, titleLabel.FontAttributes);
				Assert.Equal(new Thickness(10, 0), menuLabel.Margin);
				Assert.Equal(new Thickness(10, 0), settingsLabel.Margin);
				Assert.Equal(Colors.Red, titleGrid.BackgroundColor);
				Assert.Equal(Colors.DodgerBlue, contentBox.Color);
				Assert.Equal(new Thickness(0), titleGrid.Padding);
				Assert.Equal(new Thickness(0), titleGrid.Margin);
				Assert.Equal(0d, titleGrid.ColumnSpacing);
				Assert.Same(titleGrid, Shell.GetTitleView(page));
				Assert.Same(contentBox, page.Content);

				UIView titleView = titleGrid.ToPlatform();
				UIView boxView = contentBox.ToPlatform();
				var navigationBar = GetPlatformToolbar(handler);
				Assert.NotNull(navigationBar);
				Assert.Same(titleView, GetTitleView(handler));
				Assert.NotNull(titleView.Window);
				Assert.NotNull(boxView.Window);
				Assert.NotNull(navigationBar.Window);
				Assert.NotEqual(CoreGraphics.CGRect.Empty, titleView.Frame);

				var titleFrame = titleView.ConvertRectToView(titleView.Bounds, null);
				var navigationFrame = navigationBar.ConvertRectToView(navigationBar.Bounds, null);
				var contentFrame = boxView.ConvertRectToView(boxView.Bounds, null);
				double leftInset = titleFrame.Left - navigationFrame.Left;
				double rightInset = navigationFrame.Right - titleFrame.Right;
				horizontalInset = Math.Max(leftInset, rightInset);
				verticalContentGap = contentFrame.Top - titleFrame.Bottom;
				layoutCompleted = true;

				Assert.True(layoutCompleted);
				Assert.True(Math.Abs(leftInset) <= tolerance && Math.Abs(rightInset) <= tolerance,
					$"Shell TitleView horizontal inset was left={leftInset:F2}, right={rightInset:F2}; title={titleFrame}, navigationBar={navigationFrame}, expected 0 +/- {tolerance:F2}.");
				Assert.True(Math.Abs(verticalContentGap) <= tolerance,
					$"Shell TitleView vertical gap was {verticalContentGap:F2}; title={titleFrame}, content={contentFrame}, expected 0 +/- {tolerance:F2}.");
			});

			Assert.True(layoutCompleted);
			Assert.False(double.IsNaN(horizontalInset));
			Assert.False(double.IsNaN(verticalContentGap));

			static Grid CreateTitleGrid(out Label menuLabel, out Label titleLabel, out Label settingsLabel)
			{
				var titleGrid = new Grid
				{
					BackgroundColor = Colors.Red,
					Padding = 0,
					Margin = 0,
					ColumnSpacing = 0,
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Fill,
					ColumnDefinitions =
					{
						new ColumnDefinition(GridLength.Auto),
						new ColumnDefinition(GridLength.Star),
						new ColumnDefinition(GridLength.Auto)
					}
				};

				menuLabel = new Label
				{
					Text = "☰",
					FontSize = 24,
					TextColor = Colors.White,
					VerticalOptions = LayoutOptions.Center,
					Margin = new Thickness(10, 0)
				};
				titleLabel = new Label
				{
					Text = "MY APP TITLE",
					TextColor = Colors.White,
					FontSize = 16,
					FontAttributes = FontAttributes.Bold,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				settingsLabel = new Label
				{
					Text = "⚙",
					FontSize = 24,
					TextColor = Colors.White,
					VerticalOptions = LayoutOptions.Center,
					Margin = new Thickness(10, 0)
				};

				Grid.SetColumn(titleLabel, 1);
				Grid.SetColumn(settingsLabel, 2);
				titleGrid.Add(menuLabel);
				titleGrid.Add(titleLabel);
				titleGrid.Add(settingsLabel);
				return titleGrid;
			}
		}
	}
}
#endif

