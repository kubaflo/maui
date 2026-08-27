#if ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;
using Bitmap = Android.Graphics.Bitmap;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category("Issue26526")]
	public class Issue26526 : ControlsHandlerTestBase
	{
		const double RequiredContrast = 4.5;

		[Fact]
		public async Task UsernameRemainsVisibleOnWhiteCardAfterSwitchingToDarkTheme()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<StackLayout, LayoutHandler>();
					handlers.AddHandler<CollectionView, CollectionViewHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			Application attachedApplication = null;
			var originalTheme = AppTheme.Unspecified;
			var themeWasSet = false;

			try
			{
				Label firstUsernameLabel = null;
				Border firstItemBorder = null;
				Image firstItemImage = null;

				var labelStyle = new Style(typeof(Label))
				{
					Setters =
					{
						new Setter
						{
							Property = Label.TextColorProperty,
							Value = new AppThemeBinding { Light = Color.FromArgb("#212121"), Dark = Colors.White }
						},
						new Setter { Property = Label.BackgroundColorProperty, Value = Colors.Transparent },
						new Setter { Property = Label.FontSizeProperty, Value = 14d }
					}
				};

				var switchThemeButton = new Button { Text = "Switch to Dark Theme" };
				switchThemeButton.Clicked += (_, _) =>
				{
					if (attachedApplication is null)
						throw new InvalidOperationException("The application must be attached before changing its theme.");

					attachedApplication.UserAppTheme = AppTheme.Dark;
				};

				var instructionLabel = new Label
				{
					Text = "1. The test pass if the item heights are consistent when scrolling."
				};

				var collectionView = new CollectionView
				{
					ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical),
					ItemsSource = new List<string>
					{
						"If you're visiting this page, you're likely here because you're searching for a random sentence.",
						"Sometimes a random word just isn't enough, and that is where the random sentence generator comes into play. By inputting the desired number, you can make a list of as many random sentences as you want or need. Producing random sentences can be helpful in a number of different ways.",
						"For writers, a random sentence can help them get their creative juices flowing. Since the topic of the sentence is completely unknown, it forces the writer to be creative when the sentence appears.",
						"For those writers who have writers' block, this can be an excellent way to take a step to crumbling those walls.",
						"It can also be successfully used as a daily exercise to get writers to begin writing."
					},
					ItemTemplate = new DataTemplate(() =>
					{
						var usernameLabel = new Label
						{
							Text = "Username",
							HorizontalOptions = LayoutOptions.Start
						};
						var todayLabel = new Label
						{
							Text = "Today",
							VerticalOptions = LayoutOptions.Center,
							HorizontalOptions = LayoutOptions.End
						};
						var bodyLabel = new Label { Margin = new Thickness(0, 0, 0, 10) };
						bodyLabel.SetBinding(Label.TextProperty, ".");

						var headingGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
						headingGrid.Add(usernameLabel);
						headingGrid.Add(todayLabel);

						var textStack = new VerticalStackLayout();
						textStack.Add(headingGrid);
						textStack.Add(bodyLabel);

						var itemImage = new Image
						{
							Source = "dotnet_bot.png",
							WidthRequest = 40,
							HeightRequest = 40,
							VerticalOptions = LayoutOptions.Start,
							HorizontalOptions = LayoutOptions.Center
						};
						var imageStack = new VerticalStackLayout
						{
							VerticalOptions = LayoutOptions.Start,
							Spacing = 5
						};
						imageStack.Add(itemImage);

						var itemGrid = new Grid
						{
							ColumnDefinitions =
							{
								new ColumnDefinition(new GridLength(40)),
								new ColumnDefinition(GridLength.Star)
							},
							ColumnSpacing = 10
						};
						itemGrid.Add(imageStack);
						itemGrid.Add(textStack, 1);

						var itemBorder = new Border
						{
							BackgroundColor = Colors.White,
							Padding = 10,
							StrokeShape = new RoundRectangle { CornerRadius = 15 },
							Content = itemGrid
						};

						firstUsernameLabel ??= usernameLabel;
						firstItemBorder ??= itemBorder;
						firstItemImage ??= itemImage;

						var itemStack = new VerticalStackLayout { Padding = 20 };
						itemStack.Add(itemBorder);
						return itemStack;
					})
				};

				var rootGrid = new Grid
				{
					Margin = 20,
					RowDefinitions =
					{
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Star)
					}
				};
				rootGrid.Add(switchThemeButton);
				rootGrid.Add(new StackLayout { Children = { instructionLabel } }, 0, 1);
				rootGrid.Add(collectionView, 0, 2);

				var page = new ContentPage
				{
					Title = "Item Height",
					Resources = new ResourceDictionary { labelStyle },
					Content = rootGrid
				};

				await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
				{
					attachedApplication = Assert.IsAssignableFrom<Application>(page.Window.Parent);
					originalTheme = attachedApplication.UserAppTheme;
					attachedApplication.UserAppTheme = AppTheme.Light;
					themeWasSet = true;

					await AssertEventually(() =>
						firstUsernameLabel?.Handler?.PlatformView is global::Android.Widget.TextView textView &&
						textView.Width > 0 &&
						textView.Height > 0 &&
						textView.CurrentTextColor == AColor.Rgb(0x21, 0x21, 0x21).ToArgb() &&
						firstItemBorder?.Handler?.PlatformView is AView borderView &&
						borderView.Width > 0 &&
						borderView.Height > 0 &&
						firstItemImage?.Handler?.PlatformView is AView imageView &&
						imageView.Width > 0 &&
						imageView.Height > 0,
						timeout: 5000);

					Assert.Equal("Username", firstUsernameLabel.Text);
					Assert.Equal(40, firstItemImage.WidthRequest);
					Assert.Equal(40, firstItemImage.HeightRequest);
					Assert.Equal(Colors.White, firstItemBorder.BackgroundColor);

					var nativeLabel = Assert.IsAssignableFrom<global::Android.Widget.TextView>(firstUsernameLabel.Handler.PlatformView);
					var nativeBorder = Assert.IsAssignableFrom<AView>(firstItemBorder.Handler.PlatformView);
					Assert.Equal(AColor.Rgb(0x21, 0x21, 0x21).ToArgb(), nativeLabel.CurrentTextColor);

					using (var lightBitmap = await nativeBorder.ToBitmap(MauiContext))
					{
						var lightSurface = SampleWhiteCard(lightBitmap);
						Assert.True(Contrast(new AColor(nativeLabel.CurrentTextColor), lightSurface) >= RequiredContrast);
					}

					var themeChanged = false;
					var observedTheme = AppTheme.Unspecified;
					void OnRequestedThemeChanged(object sender, AppThemeChangedEventArgs args)
					{
						themeChanged = true;
						observedTheme = args.RequestedTheme;
					}

					attachedApplication.RequestedThemeChanged += OnRequestedThemeChanged;
					try
					{
						var nativeButton = Assert.IsType<ButtonHandler>(switchThemeButton.Handler).PlatformView;
						nativeButton.PerformClick();

						await AssertEventually(() => themeChanged && observedTheme == AppTheme.Dark, timeout: 5000);
						Assert.Equal(AppTheme.Dark, attachedApplication.UserAppTheme);
						Assert.Equal(AppTheme.Dark, attachedApplication.RequestedTheme);
						await AssertEventually(
							() => firstUsernameLabel.Handler?.PlatformView is global::Android.Widget.TextView currentLabel &&
								currentLabel.Width > 0 &&
								currentLabel.Height > 0 &&
								currentLabel.CurrentTextColor == firstUsernameLabel.TextColor.ToPlatform().ToArgb() &&
								firstItemBorder.Handler?.PlatformView is AView currentBorder &&
								currentBorder.Width > 0 &&
								currentBorder.Height > 0,
							timeout: 5000);

						var currentNativeLabel = Assert.IsAssignableFrom<global::Android.Widget.TextView>(firstUsernameLabel.Handler.PlatformView);
						var currentNativeBorder = Assert.IsAssignableFrom<AView>(firstItemBorder.Handler.PlatformView);
						Assert.Equal("Username", currentNativeLabel.Text);

						using var darkBitmap = await currentNativeBorder.ToBitmap(MauiContext);
						var darkSurface = SampleWhiteCard(darkBitmap);
						var textColor = new AColor(currentNativeLabel.CurrentTextColor);
						var contrast = Contrast(textColor, darkSurface);
						Assert.True(
							contrast >= RequiredContrast,
							$"Issue26526: Username text must remain visibly distinct from its white card after switching to Dark theme. Text={textColor}, Surface={darkSurface}, Contrast={contrast:F2}, Required={RequiredContrast:F1}");
					}
					finally
					{
						attachedApplication.RequestedThemeChanged -= OnRequestedThemeChanged;
					}
				});
			}
			finally
			{
				if (themeWasSet)
					attachedApplication.UserAppTheme = originalTheme;
			}
		}

		static AColor SampleWhiteCard(Bitmap bitmap)
		{
			Assert.True(bitmap.Width > 10 && bitmap.Height > 10);
			var sampleCenters = new[]
			{
				(bitmap.Width - 5, bitmap.Height / 2),
				(bitmap.Width / 2, bitmap.Height - 5)
			};

			var red = 0;
			var green = 0;
			var blue = 0;
			var count = 0;
			foreach (var center in sampleCenters)
			{
				for (var x = center.Item1 - 1; x <= center.Item1 + 1; x++)
				{
					for (var y = center.Item2 - 1; y <= center.Item2 + 1; y++)
					{
						var color = new AColor(bitmap.GetPixel(x, y));
						Assert.True(color.R >= 250 && color.G >= 250 && color.B >= 250,
							$"Expected the rendered item card sample at ({x},{y}) to be white, but it was {color}.");
						red += color.R;
						green += color.G;
						blue += color.B;
						count++;
					}
				}
			}

			return AColor.Rgb(red / count, green / count, blue / count);
		}

		static double Contrast(AColor foreground, AColor background)
		{
			var foregroundLuminance = Luminance(foreground);
			var backgroundLuminance = Luminance(background);
			var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
			var darker = Math.Min(foregroundLuminance, backgroundLuminance);
			return (lighter + 0.05) / (darker + 0.05);
		}

		static double Luminance(AColor color)
		{
			static double Linearize(byte component)
			{
				var value = component / 255d;
				return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
			}

			return 0.2126 * Linearize(color.R) +
				0.7152 * Linearize(color.G) +
				0.0722 * Linearize(color.B);
		}
	}
}
#endif

