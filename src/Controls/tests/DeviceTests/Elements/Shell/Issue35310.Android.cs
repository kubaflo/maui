#if ANDROID
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Graphics;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.AppBar;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shell)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue35310 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task FontImageSourceBackIconPreservesGlyphAspect()
		{
			EnsureHandlerCreated(builder =>
			{
				builder
					.ConfigureFonts(fonts => fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold"))
					.ConfigureMauiHandlers(handlers =>
					{
						SetupShellHandlers(handlers);
						handlers.AddHandler(typeof(Window), typeof(WindowHandlerStub));
						handlers.AddHandler(typeof(ContentPage), typeof(PageHandler));
						handlers.AddHandler(typeof(VerticalStackLayout), typeof(LayoutHandler));
						handlers.AddHandler(typeof(Label), typeof(LabelHandler));
						handlers.AddHandler(typeof(Button), typeof(ButtonHandler));
						handlers.AddHandler(typeof(Shell), typeof(ShellRenderer));
					});
			});

			var setupButton = new Button { Text = "Prepare Shell scenario" };
			var initialPage = new ContentPage
			{
				Title = "Issue 35310",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label { Text = "Issue 35310: Android Shell FontImageSource back icon" },
						setupButton
					}
				}
			};

			int setupClickCount = -1;
			int openClickCount = -1;
			int loadedCount = -1;
			Page currentPageMarker = null;
			ContentPage g4Page = null;
			Button openButton = null;
			Shell shell = null;
			FontImageSource iconSource = null;

			setupButton.Clicked += (_, _) =>
			{
				setupClickCount = setupClickCount < 0 ? 1 : setupClickCount + 1;
				var window = initialPage.Window;
				Assert.NotNull(window);

				var resultLabel = new Label { Text = "NO BUG:", FontSize = 18 };
				openButton = new Button { Text = "Open G4" };
				var homeLayout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label { Text = "The next action opens the reported G4 page in Shell." },
						resultLabel,
						openButton
					}
				};

				shell = new Shell();
				shell.Items.Add(new ShellContent
				{
					Title = "Commands",
					Route = "commands",
					Content = new ContentPage
					{
						Title = "Commands",
						Content = homeLayout
					}
				});

				openButton.Clicked += async (_, _) =>
				{
					openClickCount = openClickCount < 0 ? 1 : openClickCount + 1;
					homeLayout.Children.Remove(resultLabel);
					iconSource = new FontImageSource
					{
						FontFamily = "OpenSansSemibold",
						Glyph = char.ConvertFromUtf32(8249)
					};
					g4Page = new ContentPage
					{
						Title = "G4",
						Content = new VerticalStackLayout
						{
							Spacing = 8,
							Children =
							{
								new Label { Text = "1. Press the back button to navigate to the previous screen." },
								new Label { Text = "2. The test fails if the back button does not appear or does not work." },
								new Label { Text = "Expected icon shape: a narrow, upright single angle glyph." },
								resultLabel
							}
						}
					};
					g4Page.Loaded += (_, _) => loadedCount = loadedCount < 0 ? 1 : loadedCount + 1;
					Shell.SetBackButtonBehavior(g4Page, new BackButtonBehavior
					{
						Command = new Command(async () => await Shell.Current.GoToAsync("..")),
						IsEnabled = true,
						IsVisible = true,
						IconOverride = iconSource
					});

					await shell.Navigation.PushAsync(g4Page);
					currentPageMarker = shell.CurrentPage;
				};

				window.Page = shell;
			};

			var activity = MauiContext.Context.GetActivity();
			var originalOrientation = activity.RequestedOrientation;
			activity.RequestedOrientation = ScreenOrientation.Portrait;

			try
			{
				await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(initialPage), async _ =>
				{
					await AssertEventually(
						() => activity.Resources.Configuration.Orientation == global::Android.Content.Res.Orientation.Portrait,
						message: "The Android test window did not enter portrait orientation.");
					Assert.True(initialPage.Window.Width < initialPage.Window.Height);
					Assert.False(Assert.IsAssignableFrom<AView>(initialPage.Handler.PlatformView).IsSoftInputShowing());

					Assert.IsAssignableFrom<AppCompatButton>(setupButton.Handler.PlatformView).PerformClick();
					await AssertEventually(
						() => setupClickCount == 1 && shell?.Handler != null && openButton?.Handler != null,
						message: "The preparation click did not attach the Commands Shell page.");

					Assert.Equal("Commands", shell.CurrentPage.Title);
					Assert.IsAssignableFrom<AppCompatButton>(openButton.Handler.PlatformView).PerformClick();
					await AssertEventually(
						() => openClickCount == 1 && loadedCount == 1 && currentPageMarker == g4Page,
						timeout: 2000,
						message: "The Open G4 click did not complete the reported Shell transition.");

					Assert.Same(g4Page, shell.CurrentPage);
					Assert.Equal("G4", g4Page.Title);
					Assert.Equal("OpenSansSemibold", iconSource.FontFamily);
					Assert.Equal(30d, iconSource.Size);
					Assert.Null(iconSource.Color);

					MaterialToolbar toolbar = null;
					AppCompatImageButton navigationButton = null;
					await AssertEventually(() =>
					{
						toolbar = GetPlatformToolbar(shell.Handler);
						navigationButton = toolbar?
							.GetChildrenOfType<AppCompatImageButton>()
							.FirstOrDefault(button => button.Drawable != null);
						return toolbar?.IsLaidOut == true &&
							navigationButton?.IsLaidOut == true &&
							navigationButton.Width > 0 &&
							navigationButton.Height > 0;
					}, timeout: 2000, message: "The Android Shell navigation button was not laid out.");

					Assert.True(navigationButton.Left < toolbar.Width / 2, "The intended navigation button was not at the left of the toolbar.");
					Assert.Equal(toolbar.NavigationIcon.Handle, navigationButton.Drawable.Handle);

					Rect glyphBounds = new Rect(-1, -1, -1, -1);
					await AssertEventually(async () =>
					{
						using var bitmap = await navigationButton.ToBitmap(MauiContext);
						return TryGetForegroundBounds(bitmap, out glyphBounds);
					}, timeout: 2000, message: "The Android Shell navigation button rendered no foreground glyph pixels.");

					int glyphWidth = glyphBounds.Width();
					int glyphHeight = glyphBounds.Height();
					Assert.True(
						glyphHeight > glyphWidth + 2,
						$"Issue35310 back icon aspect invariant failed: rendered glyph bounds were width={glyphWidth}px, height={glyphHeight}px.");
				});
			}
			finally
			{
				activity.RequestedOrientation = originalOrientation;
			}
		}

		static bool TryGetForegroundBounds(Bitmap bitmap, out Rect bounds)
		{
			int background = bitmap.GetPixel(0, 0);
			int left = bitmap.Width;
			int top = bitmap.Height;
			int right = -1;
			int bottom = -1;

			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					int pixel = bitmap.GetPixel(x, y);
					int difference =
						Math.Abs(Color.GetRedComponent(pixel) - Color.GetRedComponent(background)) +
						Math.Abs(Color.GetGreenComponent(pixel) - Color.GetGreenComponent(background)) +
						Math.Abs(Color.GetBlueComponent(pixel) - Color.GetBlueComponent(background)) +
						Math.Abs(Color.GetAlphaComponent(pixel) - Color.GetAlphaComponent(background));

					if (difference <= 48)
						continue;

					left = Math.Min(left, x);
					top = Math.Min(top, y);
					right = Math.Max(right, x);
					bottom = Math.Max(bottom, y);
				}
			}

			bounds = right >= left && bottom >= top
				? new Rect(left, top, right + 1, bottom + 1)
				: new Rect(-1, -1, -1, -1);
			return right >= left && bottom >= top;
		}
	}
}
#endif
