#nullable disable
using System;
using Android.Content;
using Android.Views;

namespace Microsoft.Maui.Controls.Handlers.Items
{
	internal class SizedItemContentView : ItemContentView
	{
		readonly Func<double> _width;
		readonly Func<double> _height;

		public SizedItemContentView(Context context, Func<double> width, Func<double> height)
			: base(context)
		{
			_width = width;
			_height = height;
		}

		// When false, the content is measured with an AtMost height spec so it keeps its intrinsic
		// height instead of being stretched to fill the space reported by the height callback.
		protected virtual bool FillHeight => true;

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (Content == null)
			{
				SetMeasuredDimension(0, 0);
				return;
			}

			double targetWidth = NormalizeDimension(_width());
			double targetHeight = NormalizeDimension(_height());

			if (!double.IsInfinity(targetWidth))
				targetWidth = Context.FromPixels(targetWidth);

			if (!double.IsInfinity(targetHeight))
				targetHeight = Context.FromPixels(targetHeight);

			if (Content.VirtualView.Handler is IPlatformViewHandler pvh)
			{
				var widthSpec = Context.CreateMeasureSpec(targetWidth,
					double.IsInfinity(targetWidth) ? double.NaN : targetWidth
					, minimumSize: double.NaN, maximumSize: targetWidth);

				var heightSpec = FillHeight
					? Context.CreateMeasureSpec(targetHeight, double.IsInfinity(targetHeight) ? double.NaN : targetHeight
						, minimumSize: double.NaN, maximumSize: targetHeight)
					: Context.CreateMeasureSpec(targetHeight, double.NaN
						, minimumSize: double.NaN, maximumSize: double.NaN);

				var size = pvh.MeasureVirtualView(widthSpec, heightSpec);

				SetMeasuredDimension((int)size.Width, (int)size.Height);
			}
		}

		static double NormalizeDimension(double value) => value == int.MaxValue ? double.PositiveInfinity : value;
	}

	internal class EmptyViewContentView : SizedItemContentView, IMauiRecyclerViewEmptyView
	{
		public EmptyViewContentView(Context context, Func<double> width, Func<double> height)
			: base(context, width, height)
		{
		}

		protected override bool FillHeight => false;
	}
}
