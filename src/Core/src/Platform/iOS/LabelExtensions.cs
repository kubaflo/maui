using System;
using Foundation;
using Microsoft.Maui.Graphics;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public static class LabelExtensions
	{
		// WCAG AA contrast ratio for normal sized text.
		const double MinimumContrastRatio = 4.5;

		// A backdrop that is nearly transparent lets whatever is behind it show through,
		// so it cannot be trusted as the surface the text is drawn on.
		const float OpaqueBackdropAlpha = 0.9f;

		const int MaxBackdropSearchDepth = 16;

		public static void UpdateTextColor(this UILabel platformLabel, ITextStyle textStyle, UIColor? defaultColor = null)
		{
			// Default value of color documented to be black in iOS docs
			var textColor = textStyle.TextColor;

			if (textColor is null && defaultColor is null)
			{
				platformLabel.TextColor = GetDefaultTextColor(textStyle);
				return;
			}

			platformLabel.TextColor = textColor.ToPlatform(defaultColor ?? ColorExtensions.LabelColor);
		}

		// The system label color follows the current appearance. That is only safe when the surface behind
		// the text follows the appearance too. If an ancestor paints a literal (theme independent) color,
		// the appearance can move the text out from under it - e.g. near white system text over a fixed
		// white card in dark mode - so the text color is pinned to whatever is measurably readable there.
		static UIColor GetDefaultTextColor(ITextStyle textStyle)
		{
			if (TryGetOpaqueBackdropColor(textStyle, out var backdrop))
			{
				var blackContrast = ContrastRatio(backdrop, Colors.Black);
				var whiteContrast = ContrastRatio(backdrop, Colors.White);

				if (blackContrast >= MinimumContrastRatio && whiteContrast < MinimumContrastRatio)
					return UIColor.Black;

				if (whiteContrast >= MinimumContrastRatio && blackContrast < MinimumContrastRatio)
					return UIColor.White;
			}

			return ColorExtensions.LabelColor;
		}

		static bool TryGetOpaqueBackdropColor(ITextStyle textStyle, out Color backdrop)
		{
			backdrop = Colors.Transparent;

			var element = textStyle as IElement;

			for (int depth = 0; element is not null && depth < MaxBackdropSearchDepth; depth++)
			{
				if (element is IView view &&
					view.Background is SolidPaint solidPaint &&
					solidPaint.Color is Color color &&
					color.Alpha >= OpaqueBackdropAlpha)
				{
					backdrop = color;
					return true;
				}

				element = element.Parent;
			}

			return false;
		}

		static double ContrastRatio(Color first, Color second)
		{
			var firstLuminance = RelativeLuminance(first);
			var secondLuminance = RelativeLuminance(second);

			return (Math.Max(firstLuminance, secondLuminance) + 0.05) / (Math.Min(firstLuminance, secondLuminance) + 0.05);
		}

		static double RelativeLuminance(Color color) =>
			(0.2126 * ToLinearChannel(color.Red)) +
			(0.7152 * ToLinearChannel(color.Green)) +
			(0.0722 * ToLinearChannel(color.Blue));

		static double ToLinearChannel(float channel)
		{
			double value = Math.Clamp((double)channel, 0d, 1d);

			return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
		}

		public static void UpdateCharacterSpacing(this UILabel platformLabel, ITextStyle textStyle)
		{
			var textAttr = platformLabel.AttributedText?.WithCharacterSpacing(textStyle.CharacterSpacing);

			if (textAttr != null)
				platformLabel.AttributedText = textAttr;
		}

		public static void UpdateFont(this UILabel platformLabel, ITextStyle textStyle, IFontManager fontManager) =>
			platformLabel.UpdateFont(textStyle, fontManager, UIFont.LabelFontSize);

		public static void UpdateFont(this UILabel platformLabel, ITextStyle textStyle, IFontManager fontManager, double defaultSize)
		{
			var uiFont = fontManager.GetFont(textStyle.Font, defaultSize);
			platformLabel.Font = uiFont;
		}

		public static void UpdateHorizontalTextAlignment(this UILabel platformLabel, ILabel label)
		{
			platformLabel.TextAlignment = label.HorizontalTextAlignment.ToPlatformHorizontal(platformLabel.EffectiveUserInterfaceLayoutDirection);
		}

		// Don't use this method, it doesn't work. But we can't remove it.
		public static void UpdateVerticalTextAlignment(this UILabel platformLabel, ILabel label)
		{
			if (!platformLabel.Bounds.IsEmpty)
				platformLabel.InvalidateMeasure(label);
		}

		internal static void UpdateVerticalTextAlignment(this MauiLabel platformLabel, ILabel label)
		{
			platformLabel.VerticalAlignment = label.VerticalTextAlignment.ToPlatformVertical();
		}

		public static void UpdatePadding(this MauiLabel platformLabel, ILabel label)
		{
			platformLabel.TextInsets = new UIEdgeInsets(
				(float)label.Padding.Top,
				(float)label.Padding.Left,
				(float)label.Padding.Bottom,
				(float)label.Padding.Right);
		}

		public static void UpdateTextDecorations(this UILabel platformLabel, ILabel label)
		{
			var modAttrText = platformLabel.AttributedText?.WithDecorations(label.TextDecorations);

			if (modAttrText != null)
				platformLabel.AttributedText = modAttrText;
		}

		public static void UpdateLineHeight(this UILabel platformLabel, ILabel label)
		{
			var modAttrText = platformLabel.AttributedText?.WithLineHeight(label.LineHeight);

			if (modAttrText != null)
				platformLabel.AttributedText = modAttrText;
		}

		internal static void UpdateTextHtml(this UILabel platformLabel, string text)
		{
			var attr = new NSAttributedStringDocumentAttributes
			{
				DocumentType = NSDocumentType.HTML,
#if IOS17_5_OR_GREATER || MACCATALYST17_5_OR_GREATER
				CharacterEncoding = NSStringEncoding.UTF8
#else
				StringEncoding = NSStringEncoding.UTF8
#endif
			};

			NSError nsError = new();

			// NOTE: Sometimes this will crash with some sort of consistency error.
			// https://github.com/dotnet/maui/issues/25946
			// The caller should ensure that this extension is dispatched. We cannot
			// do it here as we need to re-apply the formatting and we cannot call
			// into Controls from Core.
			// This is observed with CarouselView 1 but not with 2, so hopefully this
			// will be just disappear once we switch.
#pragma warning disable CS8601
#pragma warning disable CS0618
			platformLabel.AttributedText = new NSAttributedString(text, attr, ref nsError);
#pragma warning restore CS0618
#pragma warning restore CS8601
		}

		internal static void UpdateTextPlainText(this UILabel platformLabel, IText label)
		{
			platformLabel.Text = label.Text;
		}
	}
}
