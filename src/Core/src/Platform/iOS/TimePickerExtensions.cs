using System;
using System.Globalization;
using Foundation;
using Microsoft.Maui.Storage;
using UIKit;

namespace Microsoft.Maui.Platform;

public static class TimePickerExtensions
{
	public static void UpdateFormat(this MauiTimePicker mauiTimePicker, ITimePicker timePicker)
	{
		mauiTimePicker.UpdateTime(timePicker, null);
	}

	public static void UpdateFormat(this UIDatePicker picker, ITimePicker timePicker)
	{
		picker.UpdateTime(timePicker);
	}

	public static void UpdateFormat(this MauiTimePicker mauiTimePicker, ITimePicker timePicker, UIDatePicker? picker)
	{
		mauiTimePicker.UpdateTime(timePicker, picker);
	}

	public static void UpdateTime(this MauiTimePicker mauiTimePicker, ITimePicker timePicker)
	{
		mauiTimePicker.UpdateTime(timePicker, null);
	}

	public static void UpdateTime(this UIDatePicker picker, ITimePicker timePicker)
	{
		if (picker is not null)
		{
			picker.Date = new DateTime(1, 1, 1).Add(timePicker?.Time ?? TimeSpan.Zero).ToNSDate();
		}
	}

	public static void UpdateTime(this MauiTimePicker mauiTimePicker, ITimePicker timePicker, UIDatePicker? picker)
	{
		picker?.UpdateTime(timePicker);

		var cultureInfo = Culture.CurrentCulture;

		if (string.IsNullOrEmpty(timePicker.Format))
		{
			NSLocale locale = new NSLocale(cultureInfo.TwoLetterISOLanguageName);

			if (picker is not null)
			{
				picker.Locale = locale;
			}
		}

		var time = timePicker.Time;
		var format = timePicker.Format;

		// Determine which culture to use for consistent formatting
		CultureInfo formattingCulture;
		if (format != null)
		{
			if (format.Contains('t', StringComparison.Ordinal) || format.Contains('h', StringComparison.Ordinal))
			{
				formattingCulture = new CultureInfo("en-US");
			}	
			else if (format.Contains('H', StringComparison.Ordinal))
			{
				formattingCulture = new CultureInfo("de-DE");
			}
			else
			{
				formattingCulture = cultureInfo;
			}
				
		}
		else
		{
			formattingCulture = cultureInfo;
		}

		// Apply the same culture to both the text display and the picker
		mauiTimePicker.Text = time?.ToFormattedString(format ?? string.Empty, formattingCulture);

		if (picker != null && format != null)
		{
			picker.Locale = new NSLocale(formattingCulture.TwoLetterISOLanguageName);
		}

		mauiTimePicker.UpdateCharacterSpacing(timePicker);
	}

	/// <summary>
	/// Calculates the additional width the rendered time text needs so that the requested
	/// <see cref="ITextStyle.CharacterSpacing"/> can be honored by a native <see cref="UIDatePicker"/>.
	/// </summary>
	/// <remarks>
	/// Mac Catalyst renders the TimePicker with a plain <see cref="UIDatePicker"/>, which owns its
	/// text rendering and exposes no text or attributed-text API. The tracking therefore cannot be
	/// pushed into the control; instead the handler reserves the width the tracking asks for so the
	/// control's layout reflects the requested text style.
	/// </remarks>
	internal static double GetCharacterSpacingWidth(this UIDatePicker? picker, ITimePicker? timePicker)
	{
		if (picker is null || timePicker is null)
			return 0;

		var characterSpacing = timePicker.CharacterSpacing;

		if (double.IsNaN(characterSpacing) || characterSpacing <= 0)
			return 0;

		var glyphCount = GetRenderedTimeText(timePicker).Length;

		// Tracking sits between glyphs, so a run of n glyphs grows by (n - 1) tracking steps.
		if (glyphCount < 2)
			return 0;

		return characterSpacing * (glyphCount - 1);
	}

	static string GetRenderedTimeText(ITimePicker timePicker)
	{
		var displayTime = new DateTime(1, 1, 1).Add(timePicker.Time ?? TimeSpan.Zero);
		var cultureInfo = Culture.CurrentCulture;
		var format = timePicker.Format;

		if (!string.IsNullOrEmpty(format))
		{
			try
			{
				return displayTime.ToString(format, cultureInfo);
			}
			catch (FormatException)
			{
				// A custom format the platform cannot render falls back to the culture's short time.
			}
		}

		return displayTime.ToString("t", cultureInfo);
	}

	public static void UpdateTextAlignment(this MauiTimePicker textField, ITimePicker timePicker)
	{
		UISemanticContentAttribute updateValue = textField.SemanticContentAttribute;

		textField.TextAlignment = (updateValue == UISemanticContentAttribute.ForceRightToLeft) ? UITextAlignment.Right : UITextAlignment.Left;
	}

	internal static void UpdateIsOpen(this UIDatePicker picker, ITimePicker timePicker)
	{
		if (timePicker.IsOpen)
			picker.BecomeFirstResponder();
		else
			picker.ResignFirstResponder();
	}

	internal static void UpdateIsOpen(this MauiTimePicker mauiTimePicker, ITimePicker timePicker)
	{
		if (timePicker.IsOpen)
			mauiTimePicker.BecomeFirstResponder();
		else
			mauiTimePicker.ResignFirstResponder();
	}
}