using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AView = Android.Views.View;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if ANDROID
	[Category("Issue31797")]
	public class Issue31797 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ReplacingControlTemplatePreservesResultContentHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<TemplatedTaskLoader, ContentViewHandler>();
					handlers.AddHandler<SillyDudeView, ContentViewHandler>();
					handlers.AddHandler<RefreshView, RefreshViewHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<ActivityIndicator, ActivityIndicatorHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			double cleanHeight = 0;
			var cleanResult = new ResultViews();
			cleanResult.LoadedToken = -1;
			var cleanLoader = new TemplatedTaskLoader
			{
				ControlTemplate = CreateResultTemplate(cleanResult)
			};
			var cleanPage = CreatePage(cleanLoader);

			await CreateHandlerAndAddToWindow(cleanPage, async () =>
			{
				await AssertEventually(
					() => cleanResult.LoadedToken == 1 &&
						HasPositiveNativeFrame(cleanLoader) &&
						HasPositiveNativeFrame(cleanResult.RefreshView) &&
						HasPositiveNativeFrame(cleanResult.SillyDude) &&
						HasPositiveNativeFrame(cleanResult.FirstLabel) &&
						HasPositiveNativeFrame(cleanResult.SecondLabel),
					message: "The clean result template did not finish native layout.");

				Assert.Equal("SillyDude result content", cleanResult.FirstLabel.Text);
				Assert.Equal("The result template should size itself to this visible content.", cleanResult.SecondLabel.Text);
				AssertNativeViewVisibleAndAttached(cleanResult.RefreshView);
				AssertNativeViewVisibleAndAttached(cleanResult.SillyDude);
				AssertNativeViewVisibleAndAttached(cleanResult.FirstLabel);
				AssertNativeViewVisibleAndAttached(cleanResult.SecondLabel);
				AssertContained(cleanResult.ResultStack, cleanResult.FirstLabel);
				AssertContained(cleanResult.ResultStack, cleanResult.SecondLabel);
				AssertContained(cleanLoader, cleanResult.RefreshView);

				cleanHeight = GetNativeView(cleanLoader).Height;
			});

			Label loadingLabel = null;
			var loadingTemplate = new ControlTemplate(() =>
			{
				loadingLabel = new Label
				{
					Text = "Loading view",
					HorizontalTextAlignment = TextAlignment.Center,
					TextColor = Colors.DarkBlue,
					FontAttributes = FontAttributes.Bold
				};

				return new Border
				{
					BackgroundColor = Colors.LightBlue,
					Padding = 20,
					Content = new VerticalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new ActivityIndicator
							{
								IsRunning = true,
								Color = Colors.DarkBlue
							},
							loadingLabel
						}
					}
				};
			});

			var targetLoader = new TemplatedTaskLoader
			{
				ControlTemplate = loadingTemplate
			};
			var targetPage = CreatePage(targetLoader);
			var targetResult = new ResultViews();
			targetResult.LoadedToken = -1;

			await CreateHandlerAndAddToWindow(targetPage, async () =>
			{
				await AssertEventually(
					() => loadingLabel != null &&
						loadingLabel.Text == "Loading view" &&
						HasPositiveNativeFrame(loadingLabel) &&
						HasPositiveNativeFrame(targetLoader),
					message: "The loading template did not finish native layout.");

				AssertNativeViewVisibleAndAttached(loadingLabel);
				double initialHeight = GetNativeView(targetLoader).Height;

				targetLoader.ControlTemplate = CreateResultTemplate(targetResult);

				await AssertEventually(
					() => targetResult.LoadedToken == 1,
					message: "The replacement result subtree did not raise Loaded.");
				await AssertEventually(
					() => targetResult.RefreshView != null &&
						targetResult.SillyDude != null &&
						targetResult.FirstLabel != null &&
						targetResult.SecondLabel != null &&
						HasPositiveNativeFrame(targetLoader) &&
						HasPositiveNativeFrame(targetResult.RefreshView) &&
						HasPositiveNativeFrame(targetResult.SillyDude) &&
						HasPositiveNativeFrame(targetResult.FirstLabel) &&
						HasPositiveNativeFrame(targetResult.SecondLabel),
					message: "The replacement result template did not finish native layout.");
				var loaderNativeView = GetNativeView(targetLoader);
				double finalHeight = loaderNativeView.Height;
				double contentHeight = GetNativeView(targetResult.ResultStack).Height;
				double density = loaderNativeView.Resources.DisplayMetrics.Density;
				double tolerance = Math.Max(1, density);
				double expectedHeight = Math.Max(initialHeight, cleanHeight) - tolerance;

				Assert.True(
					finalHeight >= expectedHeight,
					$"Issue31797 result template native height collapsed after ControlTemplate replacement: " +
					$"initial={initialHeight:F1}px, clean={cleanHeight:F1}px, final={finalHeight:F1}px, " +
					$"content={contentHeight:F1}px, density={density:F2}, tolerance={tolerance:F1}px, " +
					$"expected>={expectedHeight:F1}px.");

				AssertNativeViewVisibleAndAttached(targetResult.RefreshView);
				AssertNativeViewVisibleAndAttached(targetResult.SillyDude);
				AssertNativeViewVisibleAndAttached(targetResult.FirstLabel);
				AssertNativeViewVisibleAndAttached(targetResult.SecondLabel);
				AssertContained(targetResult.ResultStack, targetResult.FirstLabel);
				AssertContained(targetResult.ResultStack, targetResult.SecondLabel);
			});
		}

		static ContentPage CreatePage(TemplatedTaskLoader loader)
		{
			return new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 12,
					Children = { loader }
				}
			};
		}

		static ControlTemplate CreateResultTemplate(ResultViews result)
		{
			return new ControlTemplate(() =>
			{
				result.FirstLabel = new Label
				{
					Text = "SillyDude result content",
					FontSize = 20,
					FontAttributes = FontAttributes.Bold,
					TextColor = Colors.DarkBlue
				};
				result.SecondLabel = new Label
				{
					Text = "The result template should size itself to this visible content.",
					TextColor = Colors.DarkBlue
				};
				result.ResultStack = new VerticalStackLayout
				{
					Padding = 20,
					Spacing = 12,
					BackgroundColor = Colors.LightGoldenrodYellow,
					Children =
					{
						result.FirstLabel,
						result.SecondLabel
					}
				};
				result.SillyDude = new SillyDudeView
				{
					Content = new ScrollView
					{
						Content = result.ResultStack
					}
				};
				result.SillyDude.Loaded += (_, _) => result.LoadedToken = 1;
				result.RefreshView = new RefreshView
				{
					Content = result.SillyDude
				};
				return result.RefreshView;
			});
		}

		static AView GetNativeView(VisualElement element)
		{
			Assert.NotNull(element);
			Assert.NotNull(element.Handler);
			var platformHandler = Assert.IsAssignableFrom<IPlatformViewHandler>(element.Handler);
			return Assert.IsAssignableFrom<AView>(platformHandler.PlatformView);
		}

		static bool HasPositiveNativeFrame(VisualElement element)
		{
			if (element == null || element.Handler == null)
				return false;

			var nativeView = ((IPlatformViewHandler)element.Handler).PlatformView as AView;
			return nativeView != null && nativeView.Width > 0 && nativeView.Height > 0;
		}

		static void AssertNativeViewVisibleAndAttached(VisualElement element)
		{
			var nativeView = GetNativeView(element);
			Assert.True(nativeView.IsAttachedToWindow);
			Assert.True(nativeView.IsShown);
			Assert.True(nativeView.Width > 0);
			Assert.True(nativeView.Height > 0);
		}

		static void AssertContained(VisualElement parent, VisualElement child)
		{
			var parentView = GetNativeView(parent);
			var childView = GetNativeView(child);
			var parentLocation = new int[2];
			var childLocation = new int[2];
			parentView.GetLocationOnScreen(parentLocation);
			childView.GetLocationOnScreen(childLocation);

			Assert.True(childLocation[0] >= parentLocation[0]);
			Assert.True(childLocation[1] >= parentLocation[1]);
			Assert.True(childLocation[0] + childView.Width <= parentLocation[0] + parentView.Width);
			Assert.True(childLocation[1] + childView.Height <= parentLocation[1] + parentView.Height);
		}

		sealed class TemplatedTaskLoader : ContentView
		{
		}

		sealed class SillyDudeView : ContentView
		{
		}

		sealed class ResultViews
		{
			public int LoadedToken;
			public RefreshView RefreshView;
			public SillyDudeView SillyDude;
			public VerticalStackLayout ResultStack;
			public Label FirstLabel;
			public Label SecondLabel;
		}
	}
#endif
}

