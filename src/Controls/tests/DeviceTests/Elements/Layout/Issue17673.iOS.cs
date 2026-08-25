using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.Layout)]
	[Category("Issue17673")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue17673 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ProportionalChildrenRetainPlatformDefaultButtonHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var explanatoryLabel = new Label
			{
				Text = "Two platform-default buttons occupy proportional bounds (0,0,1,1) in the AbsoluteLayout below."
			};
			var createButton = new Button
			{
				Text = "Create reported layout"
			};
			var measurementLabel = new Label
			{
				Text = "Height has not been checked."
			};
			var detailsLabel = new Label
			{
				Text = "The layout result will appear here."
			};
			var rootLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					explanatoryLabel,
					createButton,
					measurementLabel,
					detailsLabel
				}
			};
			var page = new ContentPage
			{
				Title = "AbsoluteLayout sizing",
				Content = rootLayout
			};

			AbsoluteLayout insertedLayout = null;
			Button bottomButton = null;
			Button topButton = null;
			bool layoutCallbackObserved = false;
			double callbackHeight = -1;

			createButton.Command = new Command(() =>
			{
				bottomButton = new Button { Text = "Bottom Button" };
				topButton = new Button { Text = "Click Me!", InputTransparent = false };
				insertedLayout = new AbsoluteLayout { bottomButton, topButton };

				AbsoluteLayout.SetLayoutFlags(bottomButton, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
				AbsoluteLayout.SetLayoutBounds(bottomButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutBounds(topButton, new Rect(0, 0, 1, 1));
				AbsoluteLayout.SetLayoutFlags(topButton, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);

				insertedLayout.SizeChanged += (_, _) =>
				{
					if (insertedLayout.Handler?.PlatformView is UIView nativeLayout)
					{
						callbackHeight = nativeLayout.Frame.Height;
						layoutCallbackObserved = true;
					}
				};

				createButton.IsEnabled = false;
				rootLayout.Children.Insert(2, insertedLayout);
			});

			Assert.Equal("Create reported layout", createButton.Text);
			Assert.Null(createButton.Style);
			Assert.False(createButton.IsSet(VisualElement.BackgroundProperty));
			Assert.Equal(-1, createButton.WidthRequest);
			Assert.Equal(-1, createButton.HeightRequest);
			Assert.Equal(4, rootLayout.Children.Count);

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await new Func<bool>(() => createButton.Handler?.PlatformView is UIButton button &&
					button.Window is not null &&
					button.Frame.Height > 0).AssertEventually(
						timeout: 5000,
						message: "The platform-default create Button was not attached and laid out.");

				var createNativeButton = Assert.IsAssignableFrom<UIButton>(createButton.Handler.PlatformView);
				var createRequiredHeight = createNativeButton.IntrinsicContentSize.Height;
				const double tolerance = 0.5;

				Assert.NotNull(createNativeButton.Superview);
				Assert.NotNull(createNativeButton.Window);
				Assert.True(createRequiredHeight > 0);
				Assert.True(
					createNativeButton.Frame.Height + tolerance >= createRequiredHeight,
					$"The clean VerticalStackLayout Button height was {createNativeButton.Frame.Height:F2}, but its native required height was {createRequiredHeight:F2}.");

				Assert.False(layoutCallbackObserved);
				Assert.Equal(-1, callbackHeight);
				createNativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await new Func<bool>(() => rootLayout.Children.Count == 5).AssertEventually(
					timeout: 5000,
					message: "The create Button command did not insert the AbsoluteLayout.");
				await new Func<bool>(() => layoutCallbackObserved).AssertEventually(
					timeout: 5000,
					message: "The inserted AbsoluteLayout did not receive a post-insertion layout callback.");

				Assert.True(layoutCallbackObserved);
				Assert.True(callbackHeight >= 0);
				Assert.False(createButton.IsEnabled);
				Assert.NotNull(insertedLayout);
				Assert.NotNull(bottomButton);
				Assert.NotNull(topButton);
				Assert.Same(insertedLayout, rootLayout.Children[2]);
				Assert.Equal(2, insertedLayout.Children.Count);
				Assert.Same(bottomButton, insertedLayout.Children[0]);
				Assert.Same(topButton, insertedLayout.Children[1]);
				Assert.Equal("Bottom Button", bottomButton.Text);
				Assert.Equal("Click Me!", topButton.Text);
				Assert.False(topButton.InputTransparent);
				Assert.Null(bottomButton.Style);
				Assert.False(bottomButton.IsSet(VisualElement.BackgroundProperty));
				Assert.Equal(-1, bottomButton.WidthRequest);
				Assert.Equal(-1, bottomButton.HeightRequest);
				Assert.Null(topButton.Style);
				Assert.False(topButton.IsSet(VisualElement.BackgroundProperty));
				Assert.Equal(-1, topButton.WidthRequest);
				Assert.Equal(-1, topButton.HeightRequest);
				Assert.Equal(Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(bottomButton));
				Assert.Equal(Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All, AbsoluteLayout.GetLayoutFlags(topButton));
				Assert.Equal(new Rect(0, 0, 1, 1), AbsoluteLayout.GetLayoutBounds(bottomButton));
				Assert.Equal(new Rect(0, 0, 1, 1), AbsoluteLayout.GetLayoutBounds(topButton));

				var nativeLayout = Assert.IsAssignableFrom<UIView>(insertedLayout.Handler.PlatformView);
				var nativeBottomButton = Assert.IsAssignableFrom<UIButton>(bottomButton.Handler.PlatformView);
				var nativeTopButton = Assert.IsAssignableFrom<UIButton>(topButton.Handler.PlatformView);

				Assert.NotNull(nativeLayout.Superview);
				Assert.NotNull(nativeLayout.Window);
				Assert.True(nativeBottomButton.IsDescendantOfView(nativeLayout));
				Assert.True(nativeTopButton.IsDescendantOfView(nativeLayout));
				Assert.NotNull(nativeBottomButton.Window);
				Assert.NotNull(nativeTopButton.Window);

				var requiredHeight = Math.Max(
					nativeBottomButton.IntrinsicContentSize.Height,
					nativeTopButton.IntrinsicContentSize.Height);
				var actualHeight = nativeLayout.Frame.Height;

				Assert.True(requiredHeight > 0);
				Assert.True(
					actualHeight + tolerance >= requiredHeight,
					$"AbsoluteLayout native height did not retain its platform-default Button child height. Observed {actualHeight:F2}, expected at least {requiredHeight:F2}, tolerance {tolerance:F2}.");
			});
		}
	}
#endif
}

