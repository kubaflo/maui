#if IOS
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 28910, "SetBlur has no effect on iOS", PlatformAffected.iOS)]
public class Issue28910 : ContentPage
{
	readonly GraphicsView _patternView;
	readonly Label _stateLabel;
	readonly Label _drawStatusLabel;
	int _initialDrawCount;
	int _blurDrawCount;
	string _supportsBlur = "unset";

	public Issue28910()
	{
		_stateLabel = new Label
		{
			AutomationId = "StateLabel",
			HorizontalOptions = LayoutOptions.Center,
			Text = "Sharp reference pattern before SetBlur(10)",
			VerticalOptions = LayoutOptions.Center
		};

		_patternView = new GraphicsView
		{
			AutomationId = "PatternView",
			HeightRequest = 300,
			WidthRequest = 400,
			Drawable = new BlurPatternDrawable(false, OnInitialPatternDrawn)
		};

		var renderBlurButton = new Button
		{
			AutomationId = "RenderBlurButton",
			Text = "Render pattern with blur radius 10"
		};
		renderBlurButton.Clicked += OnRenderBlurClicked;

		_drawStatusLabel = new Label
		{
			AutomationId = "DrawStatusLabel",
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.Center
		};
		UpdateDrawStatus();

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(30, 0),
				Spacing = 25,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					_stateLabel,
					_patternView,
					renderBlurButton,
					_drawStatusLabel
				}
			}
		};
	}

	void OnInitialPatternDrawn(bool _)
	{
		if (_initialDrawCount > 0)
			return;

		_initialDrawCount++;
		UpdateDrawStatus();
	}

	void OnRenderBlurClicked(object _, EventArgs __)
	{
		_stateLabel.Text = "After SetBlur(10): pattern should be visibly softened";
		_patternView.Drawable = new BlurPatternDrawable(true, OnBlurPatternDrawn);
		_patternView.Invalidate();
	}

	void OnBlurPatternDrawn(bool supportsBlur)
	{
		if (_blurDrawCount > 0)
			return;

		_blurDrawCount++;
		_supportsBlur = supportsBlur.ToString();
		UpdateDrawStatus();
	}

	void UpdateDrawStatus()
	{
		_drawStatusLabel.Text =
			$"InitialCompleted={_initialDrawCount > 0}; InitialDraws={_initialDrawCount}; " +
			$"BlurCompleted={_blurDrawCount > 0}; BlurDraws={_blurDrawCount}; SupportsBlur={_supportsBlur}";
	}

	sealed class BlurPatternDrawable : IDrawable
	{
		readonly bool _applyBlur;
		readonly Action<bool> _onDrawn;

		public BlurPatternDrawable(bool applyBlur, Action<bool> onDrawn)
		{
			_applyBlur = applyBlur;
			_onDrawn = onDrawn;
		}

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			IBlurrableCanvas scalingCanvas = new ScalingCanvas(canvas);

			if (_applyBlur)
				scalingCanvas.SetBlur(10);

			IPattern pattern;
			using (PictureCanvas picture = new PictureCanvas(0, 0, 10, 10))
			{
				picture.StrokeColor = Colors.Silver;
				picture.DrawLine(0, 0, 10, 10);
				picture.DrawLine(0, 10, 10, 0);
				pattern = new PicturePattern(picture.Picture, 10, 10);
			}

			PatternPaint patternPaint = new()
			{
				Pattern = pattern
			};
			canvas.SetFillPaint(patternPaint, RectF.Zero);
			canvas.FillRectangle(10, 10, 250, 250);

			_onDrawn(canvas is IBlurrableCanvas);
		}
	}
}
#endif

