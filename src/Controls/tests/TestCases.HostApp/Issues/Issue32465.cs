using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 32465, "GraphicsView line stroke rendering is inconsistent on Android", PlatformAffected.Android)]
public class Issue32465 : NavigationPage
{
	public Issue32465() : base(new Issue32465SetupPage())
	{
	}
}

sealed class Issue32465SetupPage : ContentPage
{
	readonly Label _drawResultLabel;

	public Issue32465SetupPage()
	{
		_drawResultLabel = new Label
		{
			AutomationId = "Issue32465InitialDrawResult",
			Text = "NOT DRAWN"
		};

		var openGridButton = new Button
		{
			AutomationId = "Issue32465OpenGridButton",
			Text = "Open grid reproduction"
		};
		openGridButton.Clicked += OnOpenGridClicked;

		Content = new VerticalStackLayout
		{
			Margin = 24,
			Spacing = 16,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "GraphicsView grid stroke reproduction",
					FontAttributes = FontAttributes.Bold
				},
				new Label { Text = "Open a fresh page to render the Android grid." },
				_drawResultLabel,
				openGridButton
			}
		};
	}

	async void OnOpenGridClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new Issue32465GridPage());
	}
}

sealed class Issue32465GridPage : ContentPage
{
	public Issue32465GridPage()
	{
		var drawResultLabel = new Label
		{
			AutomationId = "Issue32465DrawResult",
			Text = "NOT DRAWN"
		};
		var drawable = new Issue32465GridDrawable((drawCount, drawWidth, drawHeight) =>
			drawResultLabel.Dispatcher.Dispatch(() =>
			{
				drawResultLabel.Text = string.Create(
					CultureInfo.InvariantCulture,
					$"DRAWN count={drawCount} width={drawWidth:F3} height={drawHeight:F3} stroke=1 color=Grey");
			}));

		var graphicsView = new GraphicsView
		{
			AutomationId = "Issue32465GraphicsView",
			Drawable = drawable
		};

		var overlay = new VerticalStackLayout
		{
			AutomationId = "Issue32465Overlay",
			Margin = 12,
			Padding = 12,
			Spacing = 6,
			VerticalOptions = LayoutOptions.Start,
			BackgroundColor = Colors.White,
			Children =
			{
				new Label
				{
					Text = "GraphicsView grid stroke reproduction",
					FontAttributes = FontAttributes.Bold
				},
				new Label { Text = "Every grid line uses the same 1 DIP stroke." },
				drawResultLabel
			}
		};

		Content = new Grid
		{
			BackgroundColor = Colors.White,
			Children =
			{
				graphicsView,
				overlay
			}
		};
	}
}

sealed class Issue32465GridDrawable(Action<int, float, float> drawCompleted) : IDrawable
{
	int _drawCount;

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.StrokeColor = Colors.Grey;
		canvas.StrokeSize = 1;

		float columnWidth = dirtyRect.Width / 6;
		for (int column = 1; column < 6; column++)
		{
			float x = dirtyRect.Left + (column * columnWidth);
			canvas.DrawLine(x, dirtyRect.Top, x, dirtyRect.Bottom);
		}

		float rowHeight = dirtyRect.Height / 10;
		for (int row = 1; row < 10; row++)
		{
			float y = dirtyRect.Top + (row * rowHeight);
			canvas.DrawLine(dirtyRect.Left, y, dirtyRect.Right, y);
		}

		if (++_drawCount == 1)
			drawCompleted(_drawCount, dirtyRect.Width, dirtyRect.Height);
	}
}

