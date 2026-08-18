using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 37281, "Scrolling redraws content inside a shadowed container on Android", PlatformAffected.Android)]
public class Issue37281 : ContentPage
{
	const int RowCount = 120;

	readonly Label _statusLabel;
	readonly ScrollView _affectedScrollView;
	readonly RedrawProbeDrawable _drawable;
	bool _initialDrawCompleted;
	bool _armed;
	bool _scrollStarted;
	bool _scrollReported;
	int _cycle;
	int _postArmDrawCount;
	double _armScrollY;
	double _lastScrolledCallbackY = -1;

	public Issue37281()
	{
		BackgroundColor = Color.FromArgb("#ECEFF1");

		_statusLabel = new Label
		{
			AutomationId = "Issue37281Status",
			FontSize = 10,
			FontAttributes = FontAttributes.Bold,
			HeightRequest = 24,
			LineBreakMode = LineBreakMode.NoWrap,
			TextColor = Colors.DarkGreen
		};

		Button armButton = new Button
		{
			AutomationId = "Issue37281ArmButton",
			Text = "Begin redraw check"
		};
		armButton.Clicked += OnArmClicked;

		VerticalStackLayout rowsLayout = new VerticalStackLayout
		{
			Padding = 16,
			Spacing = 12
		};

		for (int i = 1; i <= RowCount; i++)
		{
			rowsLayout.Children.Add(new Label
			{
				AutomationId = $"ScrollableRow{i}",
				Text = $"Scrollable row {i}",
				FontSize = 18,
				TextColor = Colors.Black
			});
		}

		_drawable = new RedrawProbeDrawable(OnProbeDrawn);
		GraphicsView drawProbe = new GraphicsView
		{
			Drawable = _drawable,
			InputTransparent = true
		};

		Grid scrollContent = new Grid
		{
			Children =
			{
				drawProbe,
				rowsLayout
			}
		};

		_affectedScrollView = new ScrollView
		{
			AutomationId = "AffectedScrollView",
			Margin = 8,
			Shadow = new Shadow
			{
				Brush = Colors.Black,
				Offset = new Point(6, 6),
				Radius = 12,
				Opacity = 0.8f
			},
			Content = scrollContent
		};
		_affectedScrollView.Scrolled += OnAffectedScrollViewScrolled;

		Grid root = new Grid
		{
			Padding = 16,
			RowSpacing = 10,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Star)
			},
			Children =
			{
				new Label
				{
					Text = "Shadowed ScrollView redraw probe",
					FontSize = 20,
					FontAttributes = FontAttributes.Bold
				},
				_statusLabel,
				armButton,
				_affectedScrollView
			}
		};
		Grid.SetRow(_statusLabel, 1);
		Grid.SetRow(armButton, 2);
		Grid.SetRow(_affectedScrollView, 3);

		Content = root;
		UpdateStatus();
	}

	void OnArmClicked(object sender, EventArgs e)
	{
		_cycle++;
		_armed = true;
		_scrollStarted = false;
		_scrollReported = false;
		_postArmDrawCount = 0;
		_armScrollY = _affectedScrollView.ScrollY;
		_lastScrolledCallbackY = -1;
		_drawable.ShowRedraw = false;
		_statusLabel.TextColor = Colors.DarkGreen;
		UpdateStatus();
	}

	void OnAffectedScrollViewScrolled(object sender, ScrolledEventArgs e)
	{
		_scrollStarted = true;
		if (!_scrollReported && e.ScrollY > _armScrollY + 1)
		{
			_scrollReported = true;
			_lastScrolledCallbackY = e.ScrollY;
			UpdateStatus();
		}
	}

	void OnProbeDrawn()
	{
		bool statusChanged = !_initialDrawCompleted;
		_initialDrawCompleted = true;

		if (_armed && _scrollStarted && _postArmDrawCount == 0)
		{
			_postArmDrawCount = 1;
			_drawable.ShowRedraw = true;
			_statusLabel.TextColor = Colors.DarkRed;
			statusChanged = true;
		}

		if (statusChanged)
			UpdateStatus();
	}

	void UpdateStatus()
	{
		string initial = _initialDrawCompleted ? "complete" : "pending";
		string scrolled = _scrollReported ? "yes" : "no";
		_statusLabel.Text = string.Create(
			CultureInfo.InvariantCulture,
			$"Initial={initial}; Rows={RowCount}; Shadow=6,6/12/0.8; Cycle={_cycle}; Draws={_postArmDrawCount}; Scrolled={scrolled}; CallbackY={_lastScrolledCallbackY:0.##}; NativeY={GetNativeScrollY()}; Offset={_affectedScrollView.ScrollY:0.##}");
	}

	int GetNativeScrollY()
	{
		var handler = _affectedScrollView.Handler;
		if (handler is null)
			return -1;

#if ANDROID
		if (handler.PlatformView is Microsoft.Maui.Platform.MauiScrollView platformView)
			return platformView.ScrollY;
#endif
		return -1;
	}

	sealed class RedrawProbeDrawable : IDrawable
	{
		readonly Action _drawn;

		public RedrawProbeDrawable(Action drawn)
		{
			_drawn = drawn;
		}

		public bool ShowRedraw { get; set; }

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			_drawn();
			canvas.FillColor = ShowRedraw ? Color.FromArgb("#FFCDD2") : Colors.White;
			canvas.FillRectangle(dirtyRect);
		}
	}
}
