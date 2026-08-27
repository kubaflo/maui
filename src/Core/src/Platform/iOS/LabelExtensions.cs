using System;
using Foundation;
using Microsoft.Maui.Graphics;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public static class LabelExtensions
	{
		public static void UpdateTextColor(this UILabel platformLabel, ITextStyle textStyle, UIColor? defaultColor = null)
		{
			// Default value of color documented to be black in iOS docs
			var textColor = textStyle.TextColor;
			platformLabel.TextColor = textColor.ToPlatform(defaultColor ?? ColorExtensions.LabelColor);
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
			var attributedText = new NSAttributedString(text, attr, ref nsError);
#pragma warning restore CS0618
#pragma warning restore CS8601

			platformLabel.AttributedText = AddMissingListMarkers(attributedText);
		}

		// The HTML importer describes <ul>/<ol> items with NSTextList metadata on the paragraph style
		// instead of always writing the marker glyphs into the string. UILabel draws the string only,
		// so those lists render without any bullets or numbers. Materialize the markers as real text.
		static NSAttributedString AddMissingListMarkers(NSAttributedString source)
		{
			var text = source?.Value;

			if (source is null || string.IsNullOrEmpty(text))
				return source!;

			NSMutableAttributedString? result = null;
			NativeHandle currentList = NativeHandle.Zero;
			nint itemNumber = 0;
			int insertedLength = 0;
			int paragraphStart = 0;

			for (int i = 0; i <= text.Length; i++)
			{
				if (i < text.Length && text[i] != '\n' && text[i] != '\r' && text[i] != '\u2028' && text[i] != '\u2029')
					continue;

				var paragraphLength = i - paragraphStart;

				if (paragraphLength > 0)
				{
					var list = GetInnermostTextList(source, paragraphStart);

					if (list is null)
					{
						currentList = NativeHandle.Zero;
					}
					else
					{
						if (list.Handle == currentList)
						{
							itemNumber++;
						}
						else
						{
							currentList = list.Handle;
							itemNumber = list.StartingItemNumber;
						}

						var marker = list.GetMarker(itemNumber);

						if (!string.IsNullOrEmpty(marker) &&
							!text.AsSpan(paragraphStart, paragraphLength).TrimStart(" \t\u00a0").StartsWith(marker, StringComparison.Ordinal))
						{
							result ??= new NSMutableAttributedString(source);

							var attributes = source.GetAttributes(paragraphStart, out _);
							var markerText = marker + "\t";
							var markerString = attributes is null
								? new NSAttributedString(markerText)
								: new NSAttributedString(markerText, attributes);

							result.Insert(markerString, paragraphStart + insertedLength);
							insertedLength += markerText.Length;
						}
					}
				}

				paragraphStart = i + 1;
			}

			return result ?? source;
		}

		static NSTextList? GetInnermostTextList(NSAttributedString source, int location)
		{
			if (source.GetAttribute(UIStringAttributeKey.ParagraphStyle, location, out _) is not NSParagraphStyle paragraphStyle)
				return null;

			var lists = paragraphStyle.TextLists;

			if (lists is null || lists.Length == 0)
				return null;

			return lists[lists.Length - 1];
		}

		internal static void UpdateTextPlainText(this UILabel platformLabel, IText label)
		{
			platformLabel.Text = label.Text;
		}
	}
}
