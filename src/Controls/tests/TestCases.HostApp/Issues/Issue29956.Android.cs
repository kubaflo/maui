#if ANDROID
using Android.Graphics;
using AColor = Android.Graphics.Color;
using AImageButton = Google.Android.Material.ImageView.ShapeableImageView;

namespace Maui.Controls.Sample.Issues;

public partial class Issue29956
{
	const int ConfiguredBorderWidth = 10;

	partial void QueueNativeMeasurement(string phase, Label targetLabel)
	{
		if (AffectedImageButton.Handler?.PlatformView is not AImageButton nativeImageButton)
		{
			targetLabel.Text = $"Generation={_measurementGeneration};Phase={phase};Error=NativeViewUnavailable";
			return;
		}

		nativeImageButton.Post(() => CaptureNativeMeasurement(nativeImageButton, phase, targetLabel));
	}

	void CaptureNativeMeasurement(AImageButton nativeImageButton, string phase, Label targetLabel)
	{
		if (nativeImageButton.Width <= 0 || nativeImageButton.Height <= 0)
		{
			targetLabel.Text = $"Generation={_measurementGeneration};Phase={phase};Error=EmptyNativeFrame";
			return;
		}

		var bitmapConfig = Bitmap.Config.Argb8888;
		if (bitmapConfig is null)
		{
			targetLabel.Text = $"Generation={_measurementGeneration};Phase={phase};Error=BitmapConfigUnavailable";
			return;
		}

		using var bitmap = Bitmap.CreateBitmap(nativeImageButton.Width, nativeImageButton.Height, bitmapConfig);
		if (bitmap is null)
		{
			targetLabel.Text = $"Generation={_measurementGeneration};Phase={phase};Error=BitmapUnavailable";
			return;
		}

		using (var canvas = new Canvas(bitmap))
			nativeImageButton.Draw(canvas);

		var density = nativeImageButton.Resources?.DisplayMetrics?.Density ?? 0;
		if (density <= 0)
		{
			targetLabel.Text = $"Generation={_measurementGeneration};Phase={phase};Error=DensityUnavailable";
			return;
		}

		var borderPixels = Math.Max(3, (int)Math.Round(ConfiguredBorderWidth * density));
		var stripStart = Math.Max(1, borderPixels / 10);
		var stripEnd = Math.Max(stripStart + 1, borderPixels * 2 / 5);
		var width = bitmap.Width;
		var height = bitmap.Height;

		var top = CountRedPixels(bitmap, borderPixels, width - borderPixels, stripStart, stripEnd);
		var bottom = CountRedPixels(bitmap, borderPixels, width - borderPixels, height - stripEnd, height - stripStart);
		var left = CountRedPixels(bitmap, stripStart, stripEnd, borderPixels, height - borderPixels);
		var right = CountRedPixels(bitmap, width - stripEnd, width - stripStart, borderPixels, height - borderPixels);

		var location = new int[2];
		nativeImageButton.GetLocationOnScreen(location);
		var widthDip = (int)Math.Round(width / density);
		var heightDip = (int)Math.Round(height / density);
		_measurementGeneration++;
		targetLabel.Text =
			$"Generation={_measurementGeneration};Phase={phase};Width={width};Height={height};WidthDip={widthDip};HeightDip={heightDip};X={location[0]};Y={location[1]};" +
			$"Drawable={nativeImageButton.Drawable is not null};Top={top.Red}/{top.Total};Bottom={bottom.Red}/{bottom.Total};" +
			$"Left={left.Red}/{left.Total};Right={right.Red}/{right.Total}";
	}

	static BorderSample CountRedPixels(Bitmap bitmap, int xStart, int xEnd, int yStart, int yEnd)
	{
		xStart = Math.Clamp(xStart, 0, bitmap.Width);
		xEnd = Math.Clamp(xEnd, 0, bitmap.Width);
		yStart = Math.Clamp(yStart, 0, bitmap.Height);
		yEnd = Math.Clamp(yEnd, 0, bitmap.Height);

		var xStep = Math.Max(1, (xEnd - xStart) / 40);
		var yStep = Math.Max(1, (yEnd - yStart) / 40);
		var red = 0;
		var total = 0;

		for (var y = yStart; y < yEnd; y += yStep)
		{
			for (var x = xStart; x < xEnd; x += xStep)
			{
				var color = new AColor(bitmap.GetPixel(x, y));
				if (color.A > 200 && color.R > 180 && color.G < 100 && color.B < 100)
					red++;

				total++;
			}
		}

		return new BorderSample(red, total);
	}

	readonly record struct BorderSample(int Red, int Total);
}
#endif
