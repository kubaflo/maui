#if ANDROID
using Android.Graphics;
using AndroidX.AppCompat.Widget;
using AColor = Android.Graphics.Color;

namespace Maui.Controls.Sample.Issues;

public partial class Issue37145
{
	const int ColorTolerance = 16;

	partial void ScheduleRenderMeasurement(int generation)
	{
		if (_issueRadioButton.Handler?.PlatformView is not AppCompatRadioButton nativeRadioButton)
		{
			_resultLabel.Text = $"generation={generation};error=native RadioButton unavailable";
			return;
		}
		#endif

		nativeRadioButton.Post(() => MeasureRenderedBorder(nativeRadioButton, generation));
	}

	void MeasureRenderedBorder(AppCompatRadioButton nativeRadioButton, int generation)
	{
		int width = nativeRadioButton.Width;
		int height = nativeRadioButton.Height;
		if (width <= 0 || height <= 0 || Bitmap.Config.Argb8888 is null)
		{
			_resultLabel.Text = $"generation={generation};error=empty native bounds";
			return;
		}

		using var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
		using var canvas = new Canvas(bitmap);
		nativeRadioButton.Draw(canvas);

		int redPixels = CountBorderPixels(bitmap, AColor.Red);
		int bluePixels = CountBorderPixels(bitmap, AColor.Blue);
		var location = new int[2];
		nativeRadioButton.GetLocationOnScreen(location);

		_resultLabel.Text =
			$"generation={generation};identity={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(nativeRadioButton)};" +
			$"x={location[0]};y={location[1]};width={width};height={height};" +
			$"redBorderPixels={redPixels};blueBorderPixels={bluePixels}";
	}

	static int CountBorderPixels(Bitmap bitmap, AColor expectedColor)
	{
		int borderBand = Math.Min(20, Math.Min(bitmap.Width, bitmap.Height) / 2);
		int count = 0;

		for (int y = 0; y < bitmap.Height; y++)
		{
			for (int x = 0; x < bitmap.Width; x++)
			{
				if (x >= borderBand && x < bitmap.Width - borderBand &&
					y >= borderBand && y < bitmap.Height - borderBand)
				continue;

				var pixel = new AColor(bitmap.GetPixel(x, y));
				if (Math.Abs(pixel.R - expectedColor.R) <= ColorTolerance &&
					Math.Abs(pixel.G - expectedColor.G) <= ColorTolerance &&
					Math.Abs(pixel.B - expectedColor.B) <= ColorTolerance &&
					pixel.A > 0)
					count++;
			}
		}

		return count;
	}
}
