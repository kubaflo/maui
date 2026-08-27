using System;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue25834")]
	public class Issue25834 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DefaultPaddingDoesNotClipButtonText()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
					handlers.AddHandler<CustomImageButton, ContentViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<NoTabStopButton, ButtonHandler>();
				});
			});

			var defaultButton = new NoTabStopButton
			{
				Text = "Settings",
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};
			var zeroPaddingButton = new NoTabStopButton
			{
				Text = "Settings",
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				Padding = 0
			};
			var defaultWrapper = new CustomImageButton
			{
				WidthRequest = 76,
				HeightRequest = 48,
				HorizontalOptions = LayoutOptions.Start,
				Content = defaultButton
			};
			var zeroPaddingWrapper = new CustomImageButton
			{
				WidthRequest = 76,
				HeightRequest = 48,
				HorizontalOptions = LayoutOptions.Start,
				Content = zeroPaddingButton
			};
			var page = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						defaultWrapper,
						zeroPaddingWrapper
					}
				}
			};

			var defaultLoaded = false;
			var zeroPaddingLoaded = false;
			var defaultLoadedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var zeroPaddingLoadedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			defaultButton.Loaded += (_, _) =>
			{
				defaultLoaded = true;
				defaultLoadedCompletion.TrySetResult(true);
			};
			zeroPaddingButton.Loaded += (_, _) =>
			{
				zeroPaddingLoaded = true;
				zeroPaddingLoadedCompletion.TrySetResult(true);
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await defaultLoadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
				await zeroPaddingLoadedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.True(defaultLoaded);
				Assert.True(zeroPaddingLoaded);

				var defaultHandler = Assert.IsType<ButtonHandler>(defaultButton.Handler);
				var zeroPaddingHandler = Assert.IsType<ButtonHandler>(zeroPaddingButton.Handler);
				var defaultPlatformButton = Assert.IsAssignableFrom<AppCompatButton>(defaultHandler.PlatformView);
				var zeroPaddingPlatformButton = Assert.IsAssignableFrom<AppCompatButton>(zeroPaddingHandler.PlatformView);

				await defaultPlatformButton.WaitForLayoutOrNonZeroSize();
				await zeroPaddingPlatformButton.WaitForLayoutOrNonZeroSize();

				Assert.True(defaultPlatformButton.IsAttachedToWindow);
				Assert.True(zeroPaddingPlatformButton.IsAttachedToWindow);
				Assert.Equal("Settings", defaultPlatformButton.Text);
				Assert.Equal("Settings", zeroPaddingPlatformButton.Text);

				var defaultTextLayout = defaultPlatformButton.Layout;
				var zeroPaddingTextLayout = zeroPaddingPlatformButton.Layout;
				Assert.NotNull(defaultTextLayout);
				Assert.NotNull(zeroPaddingTextLayout);
				Assert.Equal(1, zeroPaddingTextLayout.LineCount);

				var context = defaultPlatformButton.Context;
				Assert.NotNull(context);
				var expectedWidth = context.ToPixels(76);
				var expectedHeight = context.ToPixels(48);
				Assert.InRange(Math.Abs(defaultPlatformButton.Width - expectedWidth), 0, 1);
				Assert.InRange(Math.Abs(defaultPlatformButton.Height - expectedHeight), 0, 1);
				Assert.InRange(Math.Abs(zeroPaddingPlatformButton.Width - expectedWidth), 0, 1);
				Assert.InRange(Math.Abs(zeroPaddingPlatformButton.Height - expectedHeight), 0, 1);

				Assert.Equal(defaultPlatformButton.Typeface, zeroPaddingPlatformButton.Typeface);
				Assert.Equal(defaultPlatformButton.TextSize, zeroPaddingPlatformButton.TextSize);
				Assert.Equal(defaultPlatformButton.LetterSpacing, zeroPaddingPlatformButton.LetterSpacing);
				Assert.Equal(defaultPlatformButton.TransformationMethod?.GetType(), zeroPaddingPlatformButton.TransformationMethod?.GetType());

				var zeroPaddingViewport = zeroPaddingPlatformButton.Width
					- zeroPaddingPlatformButton.CompoundPaddingLeft
					- zeroPaddingPlatformButton.CompoundPaddingRight;
				var zeroPaddingLineWidth = zeroPaddingTextLayout.GetLineWidth(0);
				const float tolerance = 0.5f;
				Assert.True(
					zeroPaddingLineWidth <= zeroPaddingViewport + tolerance,
					$"Padding=0 reference unexpectedly clipped 'Settings': line width {zeroPaddingLineWidth:F2}px, viewport {zeroPaddingViewport}px.");

				var defaultViewport = defaultPlatformButton.Width
					- defaultPlatformButton.CompoundPaddingLeft
					- defaultPlatformButton.CompoundPaddingRight;
				var defaultLineWidth = defaultTextLayout.GetLineWidth(0);
				Assert.True(
					defaultTextLayout.LineCount == 1 && defaultLineWidth <= defaultViewport + tolerance,
					$"Default-padding Android Button clipped 'Settings': line count {defaultTextLayout.LineCount}, first-line width {defaultLineWidth:F2}px, viewport {defaultViewport}px, left padding {defaultPlatformButton.CompoundPaddingLeft}px, right padding {defaultPlatformButton.CompoundPaddingRight}px, native size {defaultPlatformButton.Width}x{defaultPlatformButton.Height}px, expected one line with maximum width {defaultViewport + tolerance:F2}px.");
			});
		}

		sealed class CustomImageButton : ContentView
		{
		}

		sealed class NoTabStopButton : Button
		{
		}
	}
}

