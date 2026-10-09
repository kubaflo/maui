#if ANDROID
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AImageView = Android.Widget.ImageView;
using ATextView = Android.Widget.TextView;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue31128")]
	public class Issue31128 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DynamicIndicatorTemplateReplacesDefaultIndicators()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<IndicatorView, IndicatorViewHandler>();
				});
			});

			var customIndicators = new List<Label>();
			var clickCallback = -1;
			var assignedTemplate = new DataTemplate(() =>
			{
				var customIndicator = new Label
				{
					Text = "CUSTOM",
					FontSize = 14,
					TextColor = Colors.Red,
					Padding = 4
				};

				customIndicators.Add(customIndicator);
				return customIndicator;
			});
			var indicatorView = new IndicatorView
			{
				HorizontalOptions = LayoutOptions.Center,
				ItemsSource = new[] { "One", "Two", "Three" }
			};
			var button = new Button
			{
				Text = "Set IndicatorTemplate"
			};
			button.Clicked += (_, _) =>
			{
				indicatorView.IndicatorTemplate = assignedTemplate;
				clickCallback = 1;
			};

			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 18,
						Children =
						{
							new Label
							{
								Text = "Dynamic IndicatorTemplate",
								FontSize = 24,
								FontAttributes = FontAttributes.Bold
							},
							new Label
							{
								Text = "The three default indicators are shown below before the template is assigned."
							},
							indicatorView,
							button
						}
					}
				}
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				var indicatorHandler = Assert.IsType<IndicatorViewHandler>(indicatorView.Handler);
				var pageControl = Assert.IsType<MauiPageControl>(indicatorHandler.PlatformView);
				var buttonHandler = Assert.IsType<ButtonHandler>(button.Handler);
				var nativeButton = buttonHandler.PlatformView;
				Assert.NotNull(nativeButton);

				await pageControl.WaitForLayoutOrNonZeroSize();

				GetNativeIndicatorCounts(pageControl, out var initialImageViewCount, out var initialCustomTextViewCount);
				Assert.Equal(3, indicatorView.Count);
				Assert.Equal(3, pageControl.ChildCount);
				Assert.Equal(3, initialImageViewCount);
				Assert.Equal(0, initialCustomTextViewCount);

				nativeButton.PerformClick();

				Assert.Equal(1, clickCallback);
				Assert.Same(assignedTemplate, indicatorView.IndicatorTemplate);

				var indicatorLayout = Assert.IsAssignableFrom<Microsoft.Maui.Controls.Layout>(indicatorView.IndicatorLayout);
				Assert.Equal(3, indicatorLayout.Children.Count);
				Assert.Equal(3, customIndicators.Count);
				foreach (var child in indicatorLayout.Children)
				{
					var customIndicator = Assert.IsType<Label>(child);
					Assert.Contains(customIndicator, customIndicators);
					Assert.Equal("CUSTOM", customIndicator.Text);
					Assert.Equal(14d, customIndicator.FontSize);
					Assert.Equal(Colors.Red, customIndicator.TextColor);
					Assert.Equal(new Thickness(4), customIndicator.Padding);
				}

				var imageViewCount = -1;
				var customTextViewCount = -1;
				var layoutChildCount = -1;
				var templateRendered = await AssertHelpers.Wait(() =>
				{
					GetNativeIndicatorCounts(pageControl, out imageViewCount, out customTextViewCount);
					layoutChildCount = indicatorLayout.Children.Count;
					return imageViewCount == 0 && layoutChildCount == 3 && customTextViewCount == 3;
				});

				Assert.True(
					templateRendered,
					$"Dynamic IndicatorTemplate did not replace the default Android indicator visuals. ImageViews: {imageViewCount}; layout children: {layoutChildCount}; CUSTOM TextViews: {customTextViewCount}.");
				Assert.Equal(0, imageViewCount);
				Assert.Equal(3, layoutChildCount);
				Assert.Equal(3, customTextViewCount);
			});
		}

		static void GetNativeIndicatorCounts(AView view, out int imageViewCount, out int customTextViewCount)
		{
			imageViewCount = 0;
			customTextViewCount = 0;
			CountNativeIndicators(view, ref imageViewCount, ref customTextViewCount);
		}

		static void CountNativeIndicators(AView view, ref int imageViewCount, ref int customTextViewCount)
		{
			if (view is AImageView)
			{
				imageViewCount++;
			}

			if (view is ATextView textView && textView.Text == "CUSTOM")
			{
				customTextViewCount++;
			}

			if (view is not AViewGroup viewGroup)
			{
				return;
			}

			for (var index = 0; index < viewGroup.ChildCount; index++)
			{
				var child = viewGroup.GetChildAt(index);
				if (child is not null)
				{
					CountNativeIndicators(child, ref imageViewCount, ref customTextViewCount);
				}
			}
		}
	}
}
#endif

