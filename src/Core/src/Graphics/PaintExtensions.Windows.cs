#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WColor = Windows.UI.Color;
using WGradientStop = Microsoft.UI.Xaml.Media.GradientStop;
using WLinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;
using WPoint = Windows.Foundation.Point;
using WRadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Microsoft.Maui.Graphics
{
	public static partial class PaintExtensions
	{
		public static WBrush? ToPlatform(this Paint paint)
		{
			if (paint is SolidPaint solidPaint)
				return solidPaint.CreateBrush();

			if (paint is LinearGradientPaint linearGradientPaint)
				return linearGradientPaint.CreateBrush();

			if (paint is RadialGradientPaint radialGradientPaint)
				return radialGradientPaint.CreateBrush();

			if (paint is ImagePaint imagePaint)
				return imagePaint.CreateBrush();

			if (paint is PatternPaint patternPaint)
				return patternPaint.CreateBrush();

			return null;
		}

		public static WBrush? CreateBrush(this SolidPaint solidPaint)
		{
			var brush = new WSolidColorBrush
			{
				Color = solidPaint.Color.ToWindowsColor()
			};

			return brush;
		}

		public static WBrush? CreateBrush(this LinearGradientPaint linearGradientPaint)
		{
			var brush = new WLinearGradientBrush
			{
				StartPoint = linearGradientPaint.StartPoint.ToPlatform(),
				EndPoint = linearGradientPaint.EndPoint.ToPlatform()
			};

			brush.GradientStops.AddRange(linearGradientPaint.GradientStops);

			return brush;
		}

		public static WBrush? CreateBrush(this RadialGradientPaint radialGradientPaint)
		{
			var brush = new WRadialGradientBrush
			{
				GradientOrigin = new WPoint(radialGradientPaint.Center.X, radialGradientPaint.Center.Y),
				Center = radialGradientPaint.Center.ToPlatform(),
				RadiusX = radialGradientPaint.Radius,
				RadiusY = radialGradientPaint.Radius
			};

			brush.GradientStops.AddRange(radialGradientPaint.GradientStops);

			return brush;
		}

		public static WBrush? CreateBrush(this ImagePaint imagePaint)
		{
			throw new NotImplementedException();
		}

		public static WBrush? CreateBrush(this PatternPaint patternPaint)
		{
			throw new NotImplementedException();
		}

		// A WinUI gradient brush interpolates between two stops and quantizes the result to 8 bits without
		// dithering, so a segment that only spans a handful of sRGB levels renders as a few wide plateaus of
		// identical color instead of a smooth ramp. Inserting extra stops whose colors alternate one level
		// above and below the authored ramp forces the color to change many times inside what would otherwise
		// be a single plateau, while the average color stays on the authored ramp.
		const int AntiBandingStopsPerSegment = 48;
		const int AntiBandingMaximumChannelDelta = 32;
		const float AntiBandingMinimumSegment = 1f / 512f;

		static void AddRange(this IList<WGradientStop> nativeStops, IEnumerable<PaintGradientStop> stops)
		{
			var orderedStops = stops.OrderBy(x => x.Offset).ToList();

			for (int i = 0; i < orderedStops.Count; i++)
			{
				var stop = orderedStops[i];
				var color = stop.Color.ToWindowsColor();

				nativeStops.Add(new WGradientStop
				{
					Color = color,
					Offset = stop.Offset
				});

				if (i + 1 < orderedStops.Count)
				{
					var nextStop = orderedStops[i + 1];
					nativeStops.AddAntiBandingStops(color, stop.Offset, nextStop.Color.ToWindowsColor(), nextStop.Offset);
				}
			}
		}

		static void AddAntiBandingStops(this IList<WGradientStop> nativeStops, WColor start, float startOffset, WColor end, float endOffset)
		{
			if (start.A != byte.MaxValue || end.A != byte.MaxValue)
				return;

			var length = endOffset - startOffset;
			if (length < AntiBandingMinimumSegment)
				return;

			var delta = Math.Max(Math.Abs(start.R - end.R), Math.Max(Math.Abs(start.G - end.G), Math.Abs(start.B - end.B)));
			if (delta == 0 || delta > AntiBandingMaximumChannelDelta)
				return;

			for (int i = 1; i < AntiBandingStopsPerSegment; i++)
			{
				var amount = (double)i / AntiBandingStopsPerSegment;
				var displacement = (i & 1) == 0 ? 1 : -1;

				nativeStops.Add(new WGradientStop
				{
					Color = WColor.FromArgb(
						byte.MaxValue,
						DisplaceChannel(start.R, end.R, amount, displacement),
						DisplaceChannel(start.G, end.G, amount, displacement),
						DisplaceChannel(start.B, end.B, amount, displacement)),
					Offset = startOffset + (float)(length * amount)
				});
			}
		}

		static byte DisplaceChannel(byte start, byte end, double amount, int displacement)
		{
			var value = (int)Math.Round(start + ((end - start) * amount)) + displacement;
			return (byte)Math.Clamp(value, 0, 255);
		}
	}
}