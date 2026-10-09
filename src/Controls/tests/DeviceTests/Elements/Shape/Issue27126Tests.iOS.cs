#if IOS
using System;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Layouts;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if !MACCATALYST
	[Category("Issue27126")]
	public class Issue27126 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task LineDoesNotInterceptHitTestingAwayFromItsStroke()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Line, LineHandler>();
				});
			});

			var cleanScene = CreateScene(null);

			await CreateHandlerAndAddToWindow(cleanScene.Page, async () =>
			{
				await AssertEventually(() => cleanScene.Layout.Width > 0 && cleanScene.Target.Width > 0);

				var nativeLayout = cleanScene.Layout.Handler.PlatformView as UIView;
				var nativeTarget = cleanScene.Target.Handler.PlatformView as UIView;
				Assert.NotNull(nativeLayout);
				Assert.NotNull(nativeTarget);

				var center = GetCenterInLayout(nativeTarget, nativeLayout);
				var hitView = nativeLayout.HitTest(center, null);

				Assert.True(
					IsTargetOrDescendant(hitView, nativeTarget),
					$"The clean layout did not hit the target. Expected {nativeTarget.GetType().FullName}, observed {GetNativeType(hitView)}.");
			});

			var blockingLine = new Line
			{
				Stroke = Colors.Red,
				StrokeThickness = 4,
				X1 = 0,
				Y1 = 260,
				X2 = 1,
				Y2 = 260
			};
			AbsoluteLayout.SetLayoutBounds(blockingLine, new Rect(0, 0, 1, 260));
			AbsoluteLayout.SetLayoutFlags(blockingLine, AbsoluteLayoutFlags.WidthProportional);

			var recordedScene = CreateScene(blockingLine);
			var sizeChanged = false;
			recordedScene.Layout.SizeChanged += (_, _) =>
			{
				sizeChanged = true;
				blockingLine.X2 = recordedScene.Layout.Width;
			};

			await CreateHandlerAndAddToWindow(recordedScene.Page, async () =>
			{
				await AssertEventually(
					() => sizeChanged &&
						recordedScene.Layout.Width > 0 &&
						Math.Abs(blockingLine.X2 - recordedScene.Layout.Width) < 0.01);

				Assert.True(sizeChanged);
				Assert.InRange(Math.Abs(blockingLine.X2 - recordedScene.Layout.Width), 0, 0.01);

				var nativeLayout = recordedScene.Layout.Handler.PlatformView as UIView;
				var nativeTarget = recordedScene.Target.Handler.PlatformView as UIView;
				var nativeLine = blockingLine.Handler.PlatformView as UIView;
				Assert.NotNull(nativeLayout);
				Assert.NotNull(nativeTarget);
				Assert.NotNull(nativeLine);

				var targetHandler = Assert.IsType<LabelHandler>(recordedScene.Target.Handler);
				var nativeGestureTarget = (UIView)targetHandler.ContainerView ?? targetHandler.PlatformView;
				var gestureRecognizers = nativeGestureTarget.GestureRecognizers;
				Assert.NotNull(gestureRecognizers);
				Assert.True(gestureRecognizers.Any(gesture => gesture is UITapGestureRecognizer));

				var targetCenter = new CGPoint(
					nativeTarget.Bounds.X + nativeTarget.Bounds.Width / 2,
					nativeTarget.Bounds.Y + nativeTarget.Bounds.Height / 2);
				var centerInLayout = nativeTarget.ConvertPointToView(targetCenter, nativeLayout);

				Assert.True(nativeTarget.PointInside(targetCenter, null));
				Assert.InRange(Math.Abs(nativeTarget.Bounds.Width - 200), 0, 1);
				Assert.InRange(Math.Abs(nativeTarget.Bounds.Height - 60), 0, 1);
				Assert.InRange(Math.Abs(centerInLayout.X - nativeLayout.Bounds.Width / 2), 0, 1);
				Assert.InRange(Math.Abs(centerInLayout.Y - 150), 0, 1);
				Assert.True(
					Math.Abs(centerInLayout.Y - blockingLine.Y1) > blockingLine.StrokeThickness / 2,
					"The target center must be outside the rendered Line stroke.");

				UIView hitView = null;
				hitView = nativeLayout.HitTest(centerInLayout, null);

				Assert.True(
					IsTargetOrDescendant(hitView, nativeTarget),
					$"Line added after the target intercepted iOS hit testing. Expected {nativeTarget.GetType().FullName}, observed {GetNativeType(hitView)}.");
			});

			static (ContentPage Page, AbsoluteLayout Layout, Label Target) CreateScene(Line line)
			{
				var target = new Label
				{
					Text = "Tap this target",
					TextColor = Colors.White,
					BackgroundColor = Color.FromArgb("#0078D4"),
					FontSize = 20,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center
				};
				target.GestureRecognizers.Add(new TapGestureRecognizer());
				AbsoluteLayout.SetLayoutBounds(target, new Rect(0.5, 0.5, 200, 60));
				AbsoluteLayout.SetLayoutFlags(target, AbsoluteLayoutFlags.PositionProportional);

				var tapCountLabel = new Label
				{
					Text = "Target taps: 0",
					HorizontalTextAlignment = TextAlignment.Center
				};
				AbsoluteLayout.SetLayoutBounds(tapCountLabel, new Rect(0.5, 205, 200, 30));
				AbsoluteLayout.SetLayoutFlags(tapCountLabel, AbsoluteLayoutFlags.XProportional);

				var layout = new AbsoluteLayout
				{
					BackgroundColor = Color.FromArgb("#F2F2F2")
				};
				layout.Add(target);
				layout.Add(tapCountLabel);
				if (line is not null)
					layout.Add(line);

				var grid = new Grid
				{
					Padding = 20,
					RowSpacing = 12
				};
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				grid.RowDefinitions.Add(new RowDefinition(300));
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
				grid.Add(new Label
				{
					Text = "Line hit testing",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold
				});
				Grid.SetRow(layout, 1);
				grid.Add(layout);

				return (new ContentPage { Content = grid }, layout, target);
			}

			static CGPoint GetCenterInLayout(UIView target, UIView layout)
			{
				var center = new CGPoint(
					target.Bounds.X + target.Bounds.Width / 2,
					target.Bounds.Y + target.Bounds.Height / 2);
				return target.ConvertPointToView(center, layout);
			}

			static bool IsTargetOrDescendant(UIView hitView, UIView target) =>
				hitView is not null && (ReferenceEquals(hitView, target) || hitView.IsDescendantOfView(target));

			static string GetNativeType(UIView view) =>
				view is null ? "<null>" : view.GetType().FullName;
		}
	}
#endif
}
#endif

