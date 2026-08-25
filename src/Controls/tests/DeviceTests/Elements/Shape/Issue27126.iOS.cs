#if IOS && !MACCATALYST
using System;
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

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shape)]
	[Category("Issue27126")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue27126 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task LineDoesNotInterceptTapOutsideItsStroke()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Line, LineHandler>();
				});
			});

			var baselineScene = CreateScene(includeLine: false);
			var baselineLayoutCompleted = false;
			baselineScene.Layout.SizeChanged += (_, _) => baselineLayoutCompleted = true;

			await CreateHandlerAndAddToWindow(baselineScene.Page, () =>
			{
				Assert.True(baselineLayoutCompleted, "The baseline layout callback did not run after window attachment.");

				var (window, windowCenter, labelView, labelFrame) = GetHitTestGeometry(baselineScene);
				var baselineHitView = window.HitTest(windowCenter, null);
				Assert.NotNull(baselineHitView);
				Assert.True(
					IsLabelOrDescendant(baselineHitView, labelView),
					$"Baseline native hit testing did not resolve to the Label. Actual: {baselineHitView.GetType().FullName}; point: {windowCenter}; Label frame: {labelFrame}.");
			});

			var reportedScene = CreateScene(includeLine: true);
			var reportedLayoutCompleted = false;
			reportedScene.Layout.SizeChanged += (_, _) => reportedLayoutCompleted = true;

			await CreateHandlerAndAddToWindow(reportedScene.Page, () =>
			{
				Assert.True(reportedLayoutCompleted, "The reported layout callback did not run after window attachment.");
				Assert.Same(reportedScene.Line, reportedScene.Layout.Children[reportedScene.Layout.Children.Count - 1]);
				Assert.InRange(Math.Abs(reportedScene.Line.X2 - reportedScene.Layout.Width), 0, 0.5);
				AssertHasPlatformView(reportedScene.Page);
				AssertHasPlatformView(reportedScene.Layout);
				AssertHasPlatformView(reportedScene.Label);
				AssertHasPlatformView(reportedScene.CheckButton);
				AssertHasPlatformView(reportedScene.InformationLabel);
				AssertHasPlatformView(reportedScene.Line);

				var (window, windowCenter, labelView, labelFrame) = GetHitTestGeometry(reportedScene);
				var lineView = Assert.IsAssignableFrom<UIView>(reportedScene.Line.Handler.PlatformView);
				var lineFrame = lineView.ConvertRectToView(lineView.Bounds, window);
				var strokePoint = lineView.ConvertPointToView(
					new CGPoint(reportedScene.Line.X1, reportedScene.Line.Y1),
					window);

				Assert.True(
					lineFrame.Contains(windowCenter),
					$"The issue-derived window center {windowCenter} must be inside the oversized Line frame {lineFrame}.");
				Assert.True(
					Math.Abs(windowCenter.Y - strokePoint.Y) > reportedScene.Line.StrokeThickness / 2,
					$"The hit-test point {windowCenter} must be away from the Line stroke at Y={strokePoint.Y}.");

				var hitView = window.HitTest(windowCenter, null);
				Assert.NotNull(hitView);
				Assert.True(
					IsLabelOrDescendant(hitView, labelView),
					$"Expected the blue Label to remain the native hit-test target after adding the Line. Expected: {labelView.GetType().FullName}; actual: {hitView.GetType().FullName}; point: {windowCenter}; Label frame: {labelFrame}; Line frame: {lineFrame}.");
			});
		}

		static Scene CreateScene(bool includeLine)
		{
			var targetLabel = new Label
			{
				Text = "Tap target: the center of this blue area should receive the tap.",
				TextColor = Colors.Black,
				BackgroundColor = Colors.LightBlue,
				FontSize = 22,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
			};
			targetLabel.GestureRecognizers.Add(new TapGestureRecognizer());
			AbsoluteLayout.SetLayoutBounds(targetLabel, new Rect(0, 180, 1, 300));
			AbsoluteLayout.SetLayoutFlags(targetLabel, AbsoluteLayoutFlags.WidthProportional);

			var checkButton = new Button
			{
				Text = "Check missed tap",
			};
			AbsoluteLayout.SetLayoutBounds(checkButton, new Rect(40, 520, 350, 55));

			var informationLabel = new Label
			{
				Text = "Tap the blue area, then press the button.",
				TextColor = Colors.Black,
				BackgroundColor = Colors.LightYellow,
				FontSize = 18,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
			};
			AbsoluteLayout.SetLayoutBounds(informationLabel, new Rect(40, 585, 350, 50));

			var issueLine = new Line
			{
				Stroke = Colors.Red,
				StrokeThickness = 4,
				X1 = 0,
				Y1 = 500,
				X2 = 430,
				Y2 = 500,
			};

			var layout = new AbsoluteLayout
			{
				BackgroundColor = Colors.White,
				Children =
				{
					targetLabel,
					checkButton,
					informationLabel,
				},
			};
			layout.SizeChanged += (_, _) =>
			{
				if (layout.Width > 0)
					issueLine.X2 = layout.Width;
			};

			if (includeLine)
				layout.Children.Add(issueLine);

			return new Scene
			{
				Page = new ContentPage { Content = layout },
				Layout = layout,
				Label = targetLabel,
				CheckButton = checkButton,
				InformationLabel = informationLabel,
				Line = issueLine,
			};
		}

		static (UIWindow Window, CGPoint WindowCenter, UIView LabelView, CGRect LabelFrame) GetHitTestGeometry(Scene scene)
		{
			Assert.NotNull(scene.Label.Handler);
			Assert.NotNull(scene.Label.Handler.PlatformView);
			var labelView = Assert.IsAssignableFrom<UIView>(scene.Label.Handler.PlatformView);
			Assert.NotNull(labelView.Window);
			var window = labelView.Window;
			var windowCenter = new CGPoint(window.Bounds.GetMidX(), window.Bounds.GetMidY());
			var labelFrame = labelView.ConvertRectToView(labelView.Bounds, window);
			Assert.True(
				labelFrame.Contains(windowCenter),
				$"The visible window center {windowCenter} must be inside the blue Label frame {labelFrame}.");

			return (window, windowCenter, labelView, labelFrame);
		}

		static void AssertHasPlatformView(Element element)
		{
			Assert.NotNull(element.Handler);
			Assert.NotNull(element.Handler.PlatformView);
		}

		static bool IsLabelOrDescendant(UIView hitView, UIView labelView) =>
			hitView == labelView || hitView.IsDescendantOfView(labelView);

		sealed class Scene
		{
			public ContentPage Page { get; set; }
			public AbsoluteLayout Layout { get; set; }
			public Label Label { get; set; }
			public Button CheckButton { get; set; }
			public Label InformationLabel { get; set; }
			public Line Line { get; set; }
		}
	}
}
#endif

