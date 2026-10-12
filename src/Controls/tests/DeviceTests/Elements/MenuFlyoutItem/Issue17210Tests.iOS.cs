#if MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.MenuFlyout, "Issue17210")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue17210 : ControlsHandlerTestBase
	{
		const string FailureSignature = "MenuFlyoutItem native icon did not update after bound glyph changed:";

		[Fact]
		public async Task BoundIconImageSourceUpdatesNativeMenuImage()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<MenuBar, MenuBarHandler>();
					handlers.AddHandler<MenuBarItem, MenuBarItemHandler>();
					handlers.AddHandler<MenuFlyoutItem, MenuFlyoutItemHandler>();
				});
			});

			var viewModel = new Issue17210ViewModel();
			var boundMenuItem = new MenuFlyoutItem { Text = "Bound icon" };
			boundMenuItem.SetBinding(MenuFlyoutItem.IconImageSourceProperty, nameof(Issue17210ViewModel.CurrentIcon));

			var fileMenu = new MenuBarItem { Text = "File" };
			fileMenu.Add(boundMenuItem);

			var boundSourceImage = new Image
			{
				HeightRequest = 32,
				WidthRequest = 32,
			};
			boundSourceImage.SetBinding(Image.SourceProperty, nameof(Issue17210ViewModel.CurrentIcon));

			var expectedGlyphLabel = new Label
			{
				Text = "Expected menu glyph: A",
				VerticalOptions = LayoutOptions.Center,
			};
			var updateButton = new Button { Text = "Update bound icon to B" };
			var clicked = false;
			updateButton.Clicked += (_, _) =>
			{
				clicked = true;
				viewModel.CurrentIcon = CreateIcon("B");
				expectedGlyphLabel.Text = "Expected menu glyph: B";
			};

			var sourceRow = new HorizontalStackLayout
			{
				Spacing = 12,
				Children =
				{
					new Label { Text = "Bound source:", VerticalOptions = LayoutOptions.Center },
					boundSourceImage,
					expectedGlyphLabel,
				},
			};
			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { FontSize = 20, Text = "MenuFlyoutItem bound icon update" },
					new Label { Text = "The File menu starts with glyph A. Update the binding; the menu icon should change to glyph B." },
					sourceRow,
					updateButton,
				},
			};
			var page = new ContentPage
			{
				Title = "Issue 17210",
				Content = content,
				BindingContext = viewModel,
			};
			page.MenuBarItems.Add(fileMenu);
			var boundHandler = await InvokeOnMainThreadAsync(
				() => CreateHandler<MenuFlyoutItemHandler>(boundMenuItem));

			var observedGlyph = "<not observed>";
			boundMenuItem.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == nameof(MenuFlyoutItem.IconImageSource) &&
					boundMenuItem.IconImageSource is FontImageSource source)
				{
					observedGlyph = source.Glyph;
				}
			};

			try
			{
				await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
				{
					var boundSource = Assert.IsType<FontImageSource>(boundMenuItem.IconImageSource);
					Assert.Equal("A", boundSource.Glyph);

					Assert.Same(boundHandler, boundMenuItem.Handler);
					var initialImage = boundHandler.PlatformView.Image;
					Assert.NotNull(initialImage);
					var initialNativeElement = boundHandler.PlatformView;

					var expectedMenuItem = new MenuFlyoutItem
					{
						Text = "Expected glyph B",
						IconImageSource = CreateIcon("B"),
					};
					var expectedHandler = CreateHandler<MenuFlyoutItemHandler>(expectedMenuItem);
					var expectedImage = expectedHandler.PlatformView.Image;
					Assert.NotNull(expectedImage);
					Assert.False(ImagesEqual(initialImage, expectedImage));

					var nativeButton = Assert.IsAssignableFrom<UIButton>(updateButton.Handler.PlatformView);
					nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

					Assert.True(clicked, "The native Button activation did not invoke Button.Clicked.");
					await AssertEventually(
						() => observedGlyph == "B",
						message: "MenuFlyoutItem.IconImageSource did not report the bound glyph-B change.");

					var updatedSource = Assert.IsType<FontImageSource>(boundMenuItem.IconImageSource);
					Assert.Equal("B", updatedSource.Glyph);
					Assert.Same(initialNativeElement, boundHandler.PlatformView);

					await AssertEventually(
						() => boundHandler.PlatformView.Image is UIImage observedImage &&
							ImagesEqual(observedImage, expectedImage),
						message: FailureSignature);

					var updatedImage = boundHandler.PlatformView.Image;
					Assert.NotNull(updatedImage);
					Assert.True(ImagesEqual(updatedImage, expectedImage), FailureSignature);
					Assert.False(ImagesEqual(updatedImage, initialImage), FailureSignature);
				});
			}
			finally
			{
				MenuFlyoutItemHandler.Reset();
			}
		}

		static FontImageSource CreateIcon(string glyph) =>
			new FontImageSource
			{
				FontFamily = "Arial",
				Glyph = glyph,
				Size = 24,
			};

		static bool ImagesEqual(UIImage first, UIImage second)
		{
			using var firstPng = first.AsPNG();
			using var secondPng = second.AsPNG();
			Assert.NotNull(firstPng);
			Assert.NotNull(secondPng);
			return firstPng.IsEqual(secondPng);
		}

		public sealed class Issue17210ViewModel : BindableObject
		{
			FontImageSource _boundIconSource = CreateIcon("A");

			public FontImageSource CurrentIcon
			{
				get => _boundIconSource;
				set
				{
					_boundIconSource = value;
					OnPropertyChanged();
				}
			}
		}
	}
}
#endif

