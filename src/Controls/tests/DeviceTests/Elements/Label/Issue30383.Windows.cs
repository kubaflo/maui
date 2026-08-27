#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using MauiWindow = Microsoft.Maui.Controls.Window;
using WButton = Microsoft.UI.Xaml.Controls.Button;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue30383")]
	public class Issue30383 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ShadowRendersAfterOpacityAnimation()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<MauiWindow, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			(ContentPage Page, Button TriggerButton, Label ShadowLabel) CreateScene(double initialOpacity)
			{
				var triggerButton = new Button
				{
					Text = "Update Label Opacity"
				};

				var shadowLabel = new Label
				{
					Text = "HELLO",
					FontSize = 80,
					HorizontalOptions = LayoutOptions.Center,
					Opacity = initialOpacity,
					Shadow = new Microsoft.Maui.Controls.Shadow
					{
						Brush = Colors.DarkRed,
						Offset = new Point(0, 4),
						Radius = 10
					}
				};

				var layout = new VerticalStackLayout
				{
					Padding = new Thickness(20),
					Spacing = 20
				};
				layout.Add(triggerButton);
				layout.Add(shadowLabel);

				return (new ContentPage { Content = layout }, triggerButton, shadowLabel);
			}

			async Task<int> CountRenderedShadowPixels(LabelHandler handler, WrapperView wrapper)
			{
				Assert.NotNull(handler.MauiContext);
				var bitmap = await wrapper.ToBitmap(handler.MauiContext);
				var pixels = bitmap.GetPixelColors();
				Assert.NotEmpty(pixels);

				var shadowPixelCount = 0;
				foreach (var pixel in pixels)
				{
					if (pixel.A > 0 && pixel.R > pixel.G + 20 && pixel.R > pixel.B + 20)
					{
						shadowPixelCount++;
					}
				}

				return shadowPixelCount;
			}

			var cleanScene = CreateScene(1);
			var cleanShadowPixelCount = -1;

			await CreateHandlerAndAddToWindow(cleanScene.Page, async () =>
			{
				var cleanHandler = Assert.IsType<LabelHandler>(cleanScene.ShadowLabel.Handler);
				Assert.NotNull(cleanHandler.PlatformView);
				var cleanWrapper = Assert.IsType<WrapperView>(((IPlatformViewHandler)cleanHandler).ContainerView);

				await AssertHelpers.AssertEventually(
					async () =>
					{
						cleanShadowPixelCount = await InvokeOnMainThreadAsync(() => CountRenderedShadowPixels(cleanHandler, cleanWrapper));
						return cleanShadowPixelCount > 0;
					},
					timeout: 2000,
					interval: 100,
					message: "The visible control did not render any DarkRed shadow pixels.");

				Assert.True(cleanShadowPixelCount > 0);
			});

			var requiredShadowPixelCount = Math.Max(1, cleanShadowPixelCount * 9 / 10);
			var reportedScene = CreateScene(0);
			var animationCompleted = false;
			var animationCanceled = true;
			var observedFinalOpacity = -1d;
			var animationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			reportedScene.TriggerButton.Clicked += delegate
			{
				reportedScene.ShadowLabel.Animate(
					"Issue30383Opacity",
					opacity => reportedScene.ShadowLabel.Opacity = opacity,
					0,
					1,
					rate: 16,
					length: 500,
					easing: Easing.Linear,
					finished: (_, canceled) =>
					{
						animationCanceled = canceled;
						observedFinalOpacity = reportedScene.ShadowLabel.Opacity;
						animationCompleted = true;
						animationCompletion.TrySetResult(true);
					});
			};

			await CreateHandlerAndAddToWindow(reportedScene.Page, async () =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(reportedScene.ShadowLabel.Handler);
				Assert.NotNull(labelHandler.PlatformView);
				var labelWrapper = Assert.IsType<WrapperView>(((IPlatformViewHandler)labelHandler).ContainerView);
				var initialManagedOpacity = reportedScene.ShadowLabel.Opacity;
				Assert.Equal(0d, initialManagedOpacity);

				var buttonHandler = Assert.IsType<ButtonHandler>(reportedScene.TriggerButton.Handler);
				WButton platformButton = Assert.IsAssignableFrom<WButton>(buttonHandler.PlatformView);
				var automationPeer = new ButtonAutomationPeer(platformButton);
				var invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(automationPeer.GetPattern(PatternInterface.Invoke));
				invokeProvider.Invoke();

				await animationCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2));

				Assert.True(animationCompleted);
				Assert.False(animationCanceled);
				Assert.True(
					Math.Abs(observedFinalOpacity - 1) < 0.001,
					$"Animation final opacity was {observedFinalOpacity}, expected 1.");

				var targetShadowPixelCount = -1;
				await AssertHelpers.AssertEventually(
					async () =>
					{
						targetShadowPixelCount = await InvokeOnMainThreadAsync(() => CountRenderedShadowPixels(labelHandler, labelWrapper));
						return targetShadowPixelCount >= requiredShadowPixelCount;
					},
					timeout: 2000,
					interval: 100,
					message: $"Label shadow pixels after opacity animation: expected>={requiredShadowPixelCount} (clean={cleanShadowPixelCount})");
			});
		}
	}
}
#endif

