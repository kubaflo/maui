#if ANDROID
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AView = Android.Views.View;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue34872")]
	[Category(TestCategory.Page)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue34872Tests : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ContentPageSafeAreaEdgesDefaultRemainsEdgeToEdge()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var activity = MauiContext.Context.GetActivity();
			Assert.NotNull(activity);

			{
				var topLabel = new Label
				{
					Text = "EDGE-TO-EDGE TOP REFERENCE",
					BackgroundColor = Color.FromArgb("#FF8C00"),
					TextColor = Colors.Black,
					FontAttributes = FontAttributes.Bold,
					HorizontalTextAlignment = TextAlignment.Center,
					Padding = 8
				};
				var bottomLabel = new Label
				{
					Text = "BOTTOM EDGE REFERENCE",
					BackgroundColor = Color.FromArgb("#FF8C00"),
					TextColor = Colors.Black,
					FontAttributes = FontAttributes.Bold,
					HorizontalTextAlignment = TextAlignment.Center,
					Padding = 8
				};
				var defaultButton = new Button { Text = "Default" };
				var resultLabel = new Label
				{
					Text = "PASS: waiting for edge-to-edge reference",
					BackgroundColor = Color.FromArgb("#F5F5F5"),
					TextColor = Colors.Black,
					FontAttributes = FontAttributes.Bold,
					HorizontalTextAlignment = TextAlignment.Center,
					Padding = 12
				};
				var centerContent = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 14,
					VerticalOptions = LayoutOptions.Center,
					Children =
					{
						new Label
						{
							Text = "ContentPage.SafeAreaEdges",
							TextColor = Colors.White,
							FontSize = 22,
							FontAttributes = FontAttributes.Bold,
							HorizontalTextAlignment = TextAlignment.Center
						},
						new Label
						{
							Text = "The orange edge bands should not move inward after Default.",
							TextColor = Colors.White,
							HorizontalTextAlignment = TextAlignment.Center
						},
						defaultButton,
						resultLabel
					}
				};
				var rootGrid = new Grid
				{
					SafeAreaEdges = SafeAreaEdges.None,
					BackgroundColor = Color.FromArgb("#202020"),
					RowDefinitions =
					{
						new RowDefinition(GridLength.Auto),
						new RowDefinition(GridLength.Star),
						new RowDefinition(GridLength.Auto)
					}
				};
				rootGrid.Add(topLabel);
				rootGrid.Add(centerContent, 0, 1);
				rootGrid.Add(bottomLabel, 0, 2);

				var page = new ContentPage
				{
					SafeAreaEdges = SafeAreaEdges.None,
					BackgroundColor = Color.FromArgb("#202020"),
					Content = rootGrid
				};

				int clickCount = -1;
				int propertyChangeCount = -1;
				var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				var propertyChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

				defaultButton.Clicked += (_, _) => page.SafeAreaEdges = SafeAreaEdges.Default;
				defaultButton.Clicked += OnButtonClicked;
				page.PropertyChanged += OnPagePropertyChanged;

				await CreateHandlerAndAddToWindow(page, async () =>
				{
					var pageView = (AView)((IPlatformViewHandler)page.Handler).PlatformView;
					var gridView = (AView)((IPlatformViewHandler)rootGrid.Handler).PlatformView;
					var topView = (AView)((IPlatformViewHandler)topLabel.Handler).PlatformView;
					var bottomView = (AView)((IPlatformViewHandler)bottomLabel.Handler).PlatformView;
					var buttonView = (AView)((IPlatformViewHandler)defaultButton.Handler).PlatformView;

					await pageView.WaitForLayoutOrNonZeroSize();
					await gridView.WaitForLayoutOrNonZeroSize();
					await buttonView.WaitForLayoutOrNonZeroSize();

					Assert.Equal(Orientation.Portrait, activity.Resources.Configuration.Orientation);

					var rootInsets = ViewCompat.GetRootWindowInsets(pageView);
					Assert.NotNull(rootInsets);

					var systemBarInsets = rootInsets.GetInsets(WindowInsetsCompat.Type.SystemBars());
					var imeInsets = rootInsets.GetInsets(WindowInsetsCompat.Type.Ime());
					Assert.True(systemBarInsets.Top + systemBarInsets.Bottom > 0,
						"System bar insets must be nonzero for the safe-area scenario.");
					Assert.Equal(0, imeInsets.Bottom);

					Assert.Same(gridView, topView.Parent);
					Assert.Same(gridView, bottomView.Parent);
					Assert.True(topView.Width > 0 && topView.Height > 0);
					Assert.True(bottomView.Width > 0 && bottomView.Height > 0);
					Assert.True(buttonView.Width > 0 && buttonView.Height > 0);

					var pageBoundsBefore = NativeBounds.From(pageView);
					var gridBoundsBefore = NativeBounds.From(gridView);
					var topBounds = NativeBounds.From(topView);
					var bottomBounds = NativeBounds.From(bottomView);
					Assert.True(topBounds.Top < bottomBounds.Top);
					AssertEdgesEqual(gridBoundsBefore, pageBoundsBefore, 2,
						"SafeAreaEdges.None must initially fill the native page surface");

					Assert.Equal(0, pageView.PaddingTop);
					Assert.Equal(0, pageView.PaddingBottom);

					long downTime = global::Android.OS.SystemClock.UptimeMillis();
					using (var down = MotionEvent.Obtain(
						downTime, downTime, MotionEventActions.Down,
						buttonView.Width / 2f, buttonView.Height / 2f, 0))
					{
						buttonView.DispatchTouchEvent(down);
					}

					using (var up = MotionEvent.Obtain(
						downTime, global::Android.OS.SystemClock.UptimeMillis(), MotionEventActions.Up,
						buttonView.Width / 2f, buttonView.Height / 2f, 0))
					{
						buttonView.DispatchTouchEvent(up);
					}

					await clicked.Task.WaitAsync(TimeSpan.FromSeconds(5));
					await propertyChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));

					Assert.Equal(1, clickCount);
					Assert.Equal(1, propertyChangeCount);
					Assert.Equal(SafeAreaEdges.Default, page.SafeAreaEdges);

					await AssertEventually(
						() => pageView.PaddingTop > 0 || pageView.PaddingBottom > 0,
						timeout: 5000,
						message: "The native page did not apply the system-bar insets after the Default transition.");

					Assert.Same(gridView, topView.Parent);
					Assert.Same(gridView, bottomView.Parent);
					Assert.True(topView.Width > 0 && topView.Height > 0);
					Assert.True(bottomView.Width > 0 && bottomView.Height > 0);

					Assert.True(
						pageView.PaddingLeft == 0 &&
						pageView.PaddingTop == 0 &&
						pageView.PaddingRight == 0 &&
						pageView.PaddingBottom == 0,
						$"ContentPage SafeAreaEdges.Default must remain edge-to-edge with zero native padding; " +
						$"actual [{pageView.PaddingLeft},{pageView.PaddingTop},{pageView.PaddingRight},{pageView.PaddingBottom}], " +
						$"system bars [{systemBarInsets.Left},{systemBarInsets.Top},{systemBarInsets.Right},{systemBarInsets.Bottom}]");
				});

				void OnButtonClicked(object sender, EventArgs args)
				{
					clickCount = clickCount < 0 ? 1 : clickCount + 1;
					clicked.TrySetResult();
				}

				void OnPagePropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName != ContentPage.SafeAreaEdgesProperty.PropertyName)
						return;

					propertyChangeCount = propertyChangeCount < 0 ? 1 : propertyChangeCount + 1;
					propertyChanged.TrySetResult();
				}
			}
		}

		static void AssertEdgesEqual(NativeBounds actual, NativeBounds expected, int tolerance, string message)
		{
			bool equal =
				Math.Abs(actual.Left - expected.Left) <= tolerance &&
				Math.Abs(actual.Top - expected.Top) <= tolerance &&
				Math.Abs(actual.Right - expected.Right) <= tolerance &&
				Math.Abs(actual.Bottom - expected.Bottom) <= tolerance;

			Assert.True(equal,
				$"{message}; actual {actual}, expected {expected}, tolerance {tolerance}px");
		}

		readonly struct NativeBounds
		{
			NativeBounds(int left, int top, int right, int bottom)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}

			public int Left { get; }
			public int Top { get; }
			public int Right { get; }
			public int Bottom { get; }
			public static NativeBounds From(AView view)
			{
				var location = new int[2];
				view.GetLocationOnScreen(location);
				return new NativeBounds(
					location[0],
					location[1],
					location[0] + view.Width,
					location[1] + view.Height);
			}

			public override string ToString() =>
				$"[L={Left},T={Top},R={Right},B={Bottom},W={Right - Left},H={Bottom - Top}]";
		}
	}
}
#endif
