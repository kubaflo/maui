#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Shapes;
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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue26526")]
	[Category(TestCategory.CollectionView)]
	public class Issue26526 : ControlsHandlerTestBase
	{
		const string ExpectedText = "If you're visiting this page, you're likely here because you're searching for a random sentence.";

		[Fact]
		public async Task ItemTextRemainsVisibleOnWhiteCardInDarkTheme()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var application = Application.Current;
			Assert.NotNull(application);
			var originalTheme = application.UserAppTheme;

			try
			{
				application.UserAppTheme = AppTheme.Dark;

				int calibrationDarkPixels = await MeasureCalibrationText();
				Assert.True(calibrationDarkPixels > 40,
					$"Issue26526 calibration did not render enough black text pixels: {calibrationDarkPixels}");

				var items = new List<string>
				{
					ExpectedText,
					"Sometimes a random word just isn't enough, and that is where the random sentence generator comes into play. By inputting the desired number, you can make a list of as many random sentences as you want or need. Producing random sentences can be helpful in a number of different ways.",
					"For writers, a random sentence can help them get their creative juices flowing. Since the topic of the sentence is completely unknown, it forces the writer to be creative when the sentence appears. There are a number of different ways a writer can use the random sentence for creativity. The most common way to use the sentence is to begin a story. Another option is to include it somewhere in the story. A much more difficult challenge is to use it to end a story. In any of these cases, it forces the writer to think creatively since they have no idea what sentence will appear from the tool.",
					"For those writers who have writers' block, this can be an excellent way to take a step to crumbling those walls.",
					"It can also be successfully used as a daily exercise to get writers to begin writing. Being shown a random sentence and using it to complete a paragraph each day can be an excellent way to begin any writing session.",
					"By taking the writer away from the subject matter that is causing the block, a random sentence may allow them to see the project they're working on in a different light and perspective. Sometimes all it takes is to get that first sentence down to help break the block.",
					"It can also be a fun way to surprise others. You might choose to share a random sentence on social media just to see what type of reaction it garners from others. It's an unexpected move that might create more conversation than a typical post or tweet.",
					"Have several random sentences generated and you'll soon be able to see if they can help with your project."
				};
				var emptyItems = new List<string>();
				var collectionView = new CollectionView { ItemsSource = emptyItems };
				var button = new Button { Text = "Open I1 - Vertical list for Item Height" };
				bool realizationCallbackOccurred = false;
				int realizedItemIndex = -1;
				Label realizedLabel = null;
				Border realizedBorder = null;

				collectionView.ItemTemplate = new DataTemplate(() =>
				{
					var itemLabel = new Label { Margin = new Thickness(0, 0, 0, 10) };
					itemLabel.SetBinding(Label.TextProperty, ".");

					var textLayout = new VerticalStackLayout();
					var metadata = new Grid { Margin = new Thickness(0, 0, 0, 10) };
					metadata.Add(new Label
					{
						Text = "Username",
						FontFamily = "Times New Roman",
						HorizontalOptions = LayoutOptions.Start
					});
					metadata.Add(new Label
					{
						Text = "Today",
						FontFamily = "Times New Roman",
						VerticalOptions = LayoutOptions.Center,
						HorizontalOptions = LayoutOptions.End
					});
					textLayout.Add(metadata);
					textLayout.Add(itemLabel);

					var itemGrid = new Grid
					{
						ColumnDefinitions =
						{
							new ColumnDefinition(new GridLength(40)),
							new ColumnDefinition(GridLength.Star)
						},
						ColumnSpacing = 10
					};
					itemGrid.Add(new VerticalStackLayout
					{
						Spacing = 5,
						VerticalOptions = LayoutOptions.Start,
						Children =
						{
							new Image
							{
								Source = "dotnet_bot.png",
								WidthRequest = 40,
								HeightRequest = 40,
								VerticalOptions = LayoutOptions.Start,
								HorizontalOptions = LayoutOptions.Center
							}
						}
					});
					itemGrid.Add(textLayout, 1);

					var itemBorder = new Border
					{
						BackgroundColor = Colors.White,
						Padding = 10,
						StrokeShape = new RoundRectangle { CornerRadius = 15 },
						Content = itemGrid
					};

					void RecordRealization()
					{
						if (!realizationCallbackOccurred && itemLabel.Text == ExpectedText)
						{
							realizedLabel = itemLabel;
							realizedBorder = itemBorder;
							realizedItemIndex = 0;
							realizationCallbackOccurred = true;
						}
					}

					itemLabel.Loaded += (sender, args) => RecordRealization();
					itemLabel.PropertyChanged += (sender, args) =>
					{
						if (args.PropertyName == Label.TextProperty.PropertyName && itemLabel.IsLoaded)
							RecordRealization();
					};

					return new VerticalStackLayout
					{
						Padding = 20,
						Children = { itemBorder }
					};
				});

				button.Clicked += (sender, args) => collectionView.ItemsSource = items;

				var header = new VerticalStackLayout
				{
					Spacing = 8,
					Children =
					{
						new Label { Text = "1. The test pass if the item heights are consistent when scrolling." },
						new Label { Text = "PASS:" },
						button
					}
				};
				var root = new Grid
				{
					Margin = 20,
					RowDefinitions =
					{
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Star)
					}
				};
				root.Add(header);
				root.Add(collectionView);
				Grid.SetRow(collectionView, 1);

				var page = new ContentPage
				{
					Title = "Item Height",
					Content = root
				};

				await CreateHandlerAndAddToWindow(page, async () =>
				{
					var pageView = page.ToPlatform();
					pageView.OverrideUserInterfaceStyle = UIUserInterfaceStyle.Dark;
					Assert.Equal(UIUserInterfaceStyle.Dark, pageView.OverrideUserInterfaceStyle);
					await AssertEventually(() => pageView.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark);
					Assert.Empty(emptyItems);
					Assert.Same(emptyItems, collectionView.ItemsSource);
					Assert.False(realizationCallbackOccurred);
					Assert.Equal(-1, realizedItemIndex);

					var buttonHandler = button.Handler as ButtonHandler;
					Assert.NotNull(buttonHandler);
					var platformButton = buttonHandler.PlatformView as UIButton;
					Assert.NotNull(platformButton);
					platformButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

					Assert.Same(items, collectionView.ItemsSource);
					await AssertEventually(() => realizationCallbackOccurred);
					Assert.True(realizationCallbackOccurred);
					Assert.Equal(0, realizedItemIndex);
					Assert.NotNull(realizedLabel);
					Assert.NotNull(realizedBorder);

					var platformLabel = realizedLabel.ToPlatform() as UILabel;
					var platformBorder = realizedBorder.ToPlatform();
					Assert.NotNull(platformLabel);
					Assert.Equal(ExpectedText, platformLabel.Text);
					await AssertEventually(() =>
						platformLabel.Bounds.Width > 0 &&
						platformLabel.Bounds.Height > 0 &&
						platformBorder.Bounds.Width > 0 &&
						platformBorder.Bounds.Height > 0);
					platformBorder.LayoutIfNeeded();

					var labelFrame = platformLabel.ConvertRectToView(platformLabel.Bounds, platformBorder);
					Assert.True(labelFrame.Width > 0 && labelFrame.Height > 0);
					var bitmap = Capture(platformBorder);
					int whitePixels = CountWhitePixels(bitmap);
					int expectedWhitePixels = Math.Max(100, bitmap.Width * bitmap.Height / 4);
					Assert.True(whitePixels >= expectedWhitePixels,
						$"Issue26526 expected a rendered white card but found only {whitePixels} white pixels; expected at least {expectedWhitePixels}");

					int targetDarkPixels = CountDarkPixels(bitmap, labelFrame);
					int expectedMinimum = Math.Max(10, calibrationDarkPixels / 4);
					Assert.True(targetDarkPixels >= expectedMinimum,
						$"Issue26526 item text did not render with visible contrast on its white card. " +
						$"Observed {targetDarkPixels} dark pixels; expected at least {expectedMinimum} from calibration {calibrationDarkPixels}. " +
						$"Label frame={labelFrame}; border frame={platformBorder.Frame}; text color={platformLabel.TextColor}.");
				});
			}
			finally
			{
				application.UserAppTheme = originalTheme;
			}
		}

		async Task<int> MeasureCalibrationText()
		{
			var calibrationLabel = new Label
			{
				Text = ExpectedText,
				TextColor = Colors.Black,
				Margin = new Thickness(0, 0, 0, 10)
			};
			var calibrationBorder = new Border
			{
				BackgroundColor = Colors.White,
				Padding = 10,
				StrokeShape = new RoundRectangle { CornerRadius = 15 },
				Content = calibrationLabel
			};

			int darkPixels = -1;
			await CreateHandlerAndAddToWindow(calibrationBorder, async () =>
			{
				var platformBorder = calibrationBorder.ToPlatform();
				var platformLabel = calibrationLabel.ToPlatform() as UILabel;
				Assert.NotNull(platformLabel);
				platformBorder.OverrideUserInterfaceStyle = UIUserInterfaceStyle.Dark;
				Assert.Equal(UIUserInterfaceStyle.Dark, platformBorder.OverrideUserInterfaceStyle);
				await AssertEventually(() => platformBorder.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark);
				await AssertEventually(() =>
					platformLabel.Bounds.Width > 0 &&
					platformLabel.Bounds.Height > 0 &&
					platformBorder.Bounds.Width > 0 &&
					platformBorder.Bounds.Height > 0);
				Assert.Equal(ExpectedText, platformLabel.Text);
				platformBorder.LayoutIfNeeded();

				var labelFrame = platformLabel.ConvertRectToView(platformLabel.Bounds, platformBorder);
				var bitmap = Capture(platformBorder);
				darkPixels = CountDarkPixels(bitmap, labelFrame);
			});

			Assert.True(darkPixels >= 0);
			return darkPixels;
		}

		static CapturedBitmap Capture(UIView view)
		{
			var format = new UIGraphicsImageRendererFormat
			{
				Opaque = false,
				Scale = UIScreen.MainScreen.Scale
			};
			using var renderer = new UIGraphicsImageRenderer(view.Bounds.Size, format);
			using var image = renderer.CreateImage(context => view.Layer.RenderInContext(context.CGContext));
			var cgImage = image.CGImage;
			Assert.NotNull(cgImage);
			using var data = cgImage.DataProvider.CopyData();
			var source = data.ToArray();
			int bytesPerPixel = (int)cgImage.BitsPerPixel / 8;
			var pixels = new byte[(int)(cgImage.Width * cgImage.Height * 4)];
			int destination = 0;

			for (int y = 0; y < cgImage.Height; y++)
			{
				nint row = y * cgImage.BytesPerRow;
				for (int x = 0; x < cgImage.Width; x++)
				{
					nint sourceIndex = row + x * bytesPerPixel;
					pixels[destination++] = source[sourceIndex];
					pixels[destination++] = source[sourceIndex + 1];
					pixels[destination++] = source[sourceIndex + 2];
					pixels[destination++] = source[sourceIndex + 3];
				}
			}

			return new CapturedBitmap(pixels, (int)cgImage.Width, (int)cgImage.Height, format.Scale);
		}

		static int CountDarkPixels(CapturedBitmap bitmap, CGRect frame)
		{
			int left = Math.Clamp((int)Math.Floor(frame.Left * bitmap.Scale), 0, bitmap.Width - 1);
			int top = Math.Clamp((int)Math.Floor(frame.Top * bitmap.Scale), 0, bitmap.Height - 1);
			int right = Math.Clamp((int)Math.Ceiling(frame.Right * bitmap.Scale), left + 1, bitmap.Width);
			int bottom = Math.Clamp((int)Math.Ceiling(frame.Bottom * bitmap.Scale), top + 1, bitmap.Height);
			int count = 0;

			for (int y = top; y < bottom; y++)
			{
				for (int x = left; x < right; x++)
				{
					int offset = (y * bitmap.Width + x) * 4;
					if (bitmap.Pixels[offset] < 100 &&
						bitmap.Pixels[offset + 1] < 100 &&
						bitmap.Pixels[offset + 2] < 100 &&
						bitmap.Pixels[offset + 3] > 200)
					{
						count++;
					}
				}
			}

			return count;
		}

		static int CountWhitePixels(CapturedBitmap bitmap)
		{
			int count = 0;
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					int offset = (y * bitmap.Width + x) * 4;
					if (bitmap.Pixels[offset] > 240 &&
						bitmap.Pixels[offset + 1] > 240 &&
						bitmap.Pixels[offset + 2] > 240 &&
						bitmap.Pixels[offset + 3] > 240)
					{
						count++;
					}
				}
			}

			return count;
		}

		readonly record struct CapturedBitmap(byte[] Pixels, int Width, int Height, double Scale);
	}
}
#endif

