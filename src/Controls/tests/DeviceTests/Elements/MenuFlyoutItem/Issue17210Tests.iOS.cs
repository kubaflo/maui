#if MACCATALYST
using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
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
	[Category(TestCategory.MenuFlyout)]
	[Category("Issue17210")]
	public class Issue17210 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task BoundIconImageSourceUpdatesNativeMenuCommand()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<MenuBar, MenuBarHandler>();
					handlers.AddHandler<MenuBarItem, MenuBarItemHandler>();
					handlers.AddHandler<MenuFlyoutItem, MenuFlyoutItemHandler>();
				});
			});

			MenuFlyoutItemHandler.Reset();
			try
			{
				var page = CreatePage(out var boundMenuItem, out var changeIconButton);

				await CreateHandlerAndAddToWindow(page, async () =>
				{
					var referenceA = CreateReferenceCommand("A");
					var referenceB = CreateReferenceCommand("B");
					var referenceAImage = Assert.IsType<UIImage>(referenceA.Image);
					var referenceBImage = Assert.IsType<UIImage>(referenceB.Image);

					AssertImageHasPixels(referenceAImage);
					AssertImageHasPixels(referenceBImage);
					Assert.Equal(referenceAImage.Size, referenceBImage.Size);

					string referenceAHash = GetPngHash(referenceAImage);
					string referenceBHash = GetPngHash(referenceBImage);
					Assert.NotEqual(referenceAHash, referenceBHash);

					var targetHandler = Assert.IsType<MenuFlyoutItemHandler>(boundMenuItem.ToHandler(MauiContext));
					var initialCommand = Assert.IsType<UICommand>(targetHandler.PlatformView);
					var initialImage = Assert.IsType<UIImage>(initialCommand.Image);
					string initialHash = GetPngHash(initialImage);
					Assert.Equal(referenceAHash, initialHash);

					var sourceChanged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					PropertyChangedEventHandler sourceChangedHandler = (_, args) =>
					{
						if (args.PropertyName == MenuFlyoutItem.IconImageSourceProperty.PropertyName &&
							boundMenuItem.IconImageSource is FontImageSource { Glyph: "B" })
						{
							sourceChanged.TrySetResult(true);
						}
					};

					boundMenuItem.PropertyChanged += sourceChangedHandler;
					var buttonHandler = Assert.IsType<ButtonHandler>(changeIconButton.Handler);
					buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

					await sourceChanged.Task.WaitAsync(TimeSpan.FromSeconds(2));
					boundMenuItem.PropertyChanged -= sourceChangedHandler;

					Assert.Equal(1, page.ClickCount);
					var updatedSource = Assert.IsType<FontImageSource>(boundMenuItem.IconImageSource);
					Assert.Equal("B", updatedSource.Glyph);

					string observedHash = "<not observed>";
					await AssertEventually(
						() =>
						{
							var currentCommand = Assert.IsType<UICommand>(targetHandler.PlatformView);
							var currentImage = Assert.IsType<UIImage>(currentCommand.Image);
							observedHash = GetPngHash(currentImage);
							return observedHash == referenceBHash;
						},
						message: $"MenuFlyoutItem native icon remained stale after bound glyph changed from A to B; initial={initialHash}, expected={referenceBHash}");

					Assert.True(
						observedHash == referenceBHash,
						$"MenuFlyoutItem native icon remained stale after bound glyph changed from A to B; initial={initialHash}, observed={observedHash}, expected={referenceBHash}");
				});
			}
			finally
			{
				MenuFlyoutItemHandler.Reset();
			}
		}

		UICommand CreateReferenceCommand(string glyph)
		{
			var item = new MenuFlyoutItem
			{
				Text = $"Reference {glyph}",
				IconImageSource = CreateIcon(glyph)
			};

			return Assert.IsType<UICommand>(item.CreateMenuItem(MauiContext));
		}

		static Issue17210Page CreatePage(
			out MenuFlyoutItem boundMenuItem,
			out Button changeIconButton)
		{
			var page = new Issue17210Page();
			boundMenuItem = new MenuFlyoutItem
			{
				Text = "Bound icon item"
			};
			boundMenuItem.SetBinding(MenuFlyoutItem.IconImageSourceProperty, nameof(Issue17210Page.BoundIcon));

			var menuBarItem = new MenuBarItem
			{
				Text = "Issue 17210 Menu"
			};
			menuBarItem.Add(boundMenuItem);

			var previewImage = new Image
			{
				HeightRequest = 48,
				WidthRequest = 48,
				HorizontalOptions = LayoutOptions.Center
			};
			previewImage.SetBinding(Image.SourceProperty, nameof(Issue17210Page.BoundIcon));

			changeIconButton = new Button
			{
				Text = "Change bound icon"
			};
			changeIconButton.Clicked += (_, _) => page.ChangeIcon();

			page.Content = new VerticalStackLayout
			{
				Padding = 32,
				Spacing = 18,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					previewImage,
					changeIconButton
				}
			};
			page.MenuBarItems.Add(menuBarItem);
			page.BindingContext = page;
			return page;
		}

		static void AssertImageHasPixels(UIImage image)
		{
			var cgImage = image.CGImage;
			Assert.NotNull(cgImage);
			Assert.True(cgImage.Width > 0);
			Assert.True(cgImage.Height > 0);
			Assert.True(cgImage.BytesPerRow > 0);

			using var png = image.AsPNG();
			Assert.NotNull(png);
			Assert.True(png.Length > 0);
		}

		static string GetPngHash(UIImage image)
		{
			using var png = image.AsPNG();
			Assert.NotNull(png);
			using var stream = png.AsStream();
			return Convert.ToHexString(SHA256.HashData(stream));
		}

		static FontImageSource CreateIcon(string glyph) => new()
		{
			FontFamily = "Arial",
			Glyph = glyph,
			Size = 18,
			Color = Colors.Black
		};

		sealed class Issue17210Page : ContentPage
		{
			ImageSource _boundIconSource = CreateIcon("A");

			public ImageSource BoundIcon
			{
				get => _boundIconSource;
				private set
				{
					_boundIconSource = value;
					OnPropertyChanged();
				}
			}

			public int ClickCount { get; private set; }

			public void ChangeIcon()
			{
				ClickCount++;
				BoundIcon = CreateIcon("B");
			}
		}
	}
}
#endif

