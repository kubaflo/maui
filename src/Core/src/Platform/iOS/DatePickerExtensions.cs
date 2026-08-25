using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Foundation;
using UIKit;

namespace Microsoft.Maui.Platform;

public static class DatePickerExtensions
{
	public static void UpdateFormat(this MauiDatePicker platformDatePicker, IDatePicker datePicker)
	{
		platformDatePicker.UpdateDate(datePicker, null);
	}

	public static void UpdateFormat(this MauiDatePicker platformDatePicker, IDatePicker datePicker, UIDatePicker? picker)
	{
		platformDatePicker.UpdateDate(datePicker, picker);
	}

	public static void UpdateFormat(this UIDatePicker picker, IDatePicker datePicker)
	{
		picker.UpdateLocaleForFormat(datePicker);
		picker.UpdateDate(datePicker);
	}

	// Regions probed for a date-field order. Only the region part matters: it is applied as an
	// ICU regional override on top of the user's own locale, so language, month names, numbering
	// system and currency all stay put and only the regional date conventions change.
	static readonly string[] s_dateOrderRegions = { "us", "gb", "ca", "de", "fr", "se", "jp", "hu", "au", "in", "za", "ph" };

	static void UpdateLocaleForFormat(this UIDatePicker picker, IDatePicker datePicker)
	{
		if (picker is null)
		{
			return;
		}

		var requestedOrder = GetDateComponentOrder(datePicker.Format);
		var targetLocale = requestedOrder.Length >= 2
			? ResolveLocaleForDateOrder(requestedOrder)
			: NSLocale.CurrentLocale;

		if (targetLocale is null)
		{
			// No locale renders the requested order; leave the picker's presentation untouched
			// rather than replacing it with something that is just as wrong.
			return;
		}

		if (string.Equals(picker.Locale?.Identifier, targetLocale.Identifier, StringComparison.Ordinal))
		{
			return;
		}

		picker.Locale = targetLocale;

		// The compact picker only rebuilds its segments when its value is re-applied.
		var targetDate = datePicker.Date ?? DateTime.Today;
		picker.SetDate(targetDate.ToNSDate(), false);
		picker.SetNeedsLayout();
	}

	static NSLocale? ResolveLocaleForDateOrder(string requestedOrder)
	{
		var currentLocale = NSLocale.CurrentLocale;

		if (string.Equals(GetShortDateOrder(currentLocale), requestedOrder, StringComparison.Ordinal))
		{
			return currentLocale;
		}

		var languageCode = currentLocale.LanguageCode;

		if (string.IsNullOrEmpty(languageCode))
		{
			return null;
		}

		var countryCode = currentLocale.CountryCode;
		var baseIdentifier = string.IsNullOrEmpty(countryCode) ? languageCode : $"{languageCode}-{countryCode}";

		foreach (var region in s_dateOrderRegions)
		{
			// Preferred: keep the user's locale and override only regional formatting conventions.
			// Fallback: a plain language/region identifier, for ICU versions that ignore "rg".
			var candidateIdentifiers = new[]
			{
				$"{baseIdentifier}-u-rg-{region}zzzz",
				$"{languageCode}_{region.ToUpperInvariant()}",
			};

			foreach (var identifier in candidateIdentifiers)
			{
				var candidate = NSLocale.FromLocaleIdentifier(identifier);

				if (candidate is not null && string.Equals(GetShortDateOrder(candidate), requestedOrder, StringComparison.Ordinal))
				{
					return candidate;
				}
			}
		}

		return null;
	}

	static string GetShortDateOrder(NSLocale locale)
	{
		using var formatter = new NSDateFormatter { Locale = locale };
		formatter.SetLocalizedDateFormatFromTemplate("yMd");
		return GetDateComponentOrder(formatter.DateFormat);
	}

	// Returns the day/month/year field order of a date pattern as a string such as "dMy".
	// Works for both .NET custom format strings and CLDR/ICU patterns, which use the same
	// d/M/y field letters and the same quoting rules for literal text.
	static string GetDateComponentOrder(string? pattern)
	{
		if (string.IsNullOrWhiteSpace(pattern))
		{
			return string.Empty;
		}

		Span<char> order = stackalloc char[3];
		var count = 0;

		for (var i = 0; i < pattern.Length && count < order.Length; i++)
		{
			var current = pattern[i];

			if (current == '\\')
			{
				i++;
				continue;
			}

			if (current == '\'' || current == '"')
			{
				var quote = current;
				i++;

				while (i < pattern.Length && pattern[i] != quote)
				{
					if (pattern[i] == '\\')
					{
						i++;
					}

					i++;
				}

				continue;
			}

			if (current != 'd' && current != 'M' && current != 'y')
			{
				continue;
			}

			var runStart = i;

			while (i + 1 < pattern.Length && pattern[i + 1] == current)
			{
				i++;
			}

			// "ddd"/"dddd" is the weekday name, not the numeric day-of-month field.
			if (current == 'd' && i - runStart + 1 >= 3)
			{
				continue;
			}

			var alreadySeen = false;

			for (var j = 0; j < count; j++)
			{
				if (order[j] == current)
				{
					alreadySeen = true;
					break;
				}
			}

			if (!alreadySeen)
			{
				order[count++] = current;
			}
		}

		return new string(order.Slice(0, count));
	}

	public static void UpdateDate(this MauiDatePicker platformDatePicker, IDatePicker datePicker)
	{
		platformDatePicker.UpdateDate(datePicker, null);
	}

	public static void UpdateTextColor(this MauiDatePicker platformDatePicker, IDatePicker datePicker) =>
		UpdateTextColor(platformDatePicker, datePicker, null);

	public static void UpdateTextColor(this MauiDatePicker platformDatePicker, IDatePicker datePicker, UIColor? defaultTextColor)
	{
		var textColor = datePicker.TextColor;

		if (textColor is null)
		{
			if (defaultTextColor is not null)
			{
				platformDatePicker.TextColor = defaultTextColor;
			}
		}
		else
		{
			platformDatePicker.TextColor = textColor.ToPlatform();
		}

		// HACK This forces the color to update; there's probably a more elegant way to make this happen
		platformDatePicker.UpdateDate(datePicker);
	}

	public static void UpdateDate(this UIDatePicker picker, IDatePicker datePicker)
	{
		if (picker is not null)
		{
			var targetDate = datePicker.Date ?? DateTime.Today;
			if (picker.Date.ToDateTime() != targetDate)
			{
				picker.SetDate(targetDate.ToNSDate(), false);
			}
		}
	}

	public static void UpdateDate(this MauiDatePicker platformDatePicker, IDatePicker datePicker, UIDatePicker? picker)
	{
		if (picker is not null)
		{
			var targetDate = datePicker.Date ?? DateTime.Today;
			if (picker.Date.ToDateTime() != targetDate)
			{
				picker.SetDate(targetDate.ToNSDate(), false);
			}
		}

		string format = datePicker.Format ?? string.Empty;

		if (datePicker.Date is null)
		{
			platformDatePicker.Text = string.Empty;
		}
		else if (string.IsNullOrWhiteSpace(format) || format.Equals("d", StringComparison.OrdinalIgnoreCase))
		{
			NSDateFormatter dateFormatter = new NSDateFormatter
			{
				TimeZone = NSTimeZone.FromGMT(0)
			};

			// Use datePicker.Date (the source date) for formatting
			// This ensures consistent formatting whether picker is initialized or not
			var nsDate = datePicker.Date.Value.ToNSDate();

			if (format.Equals("D", StringComparison.Ordinal) == true)
			{
				dateFormatter.DateStyle = NSDateFormatterStyle.Full;
				var strDate = dateFormatter.StringFor(nsDate);
				platformDatePicker.Text = strDate;
			}
			else
			{
				dateFormatter.SetLocalizedDateFormatFromTemplate("yMd"); // Forces 4-digit year
				var strDate = dateFormatter.StringFor(nsDate);
				platformDatePicker.Text = strDate;
			}
		}
		else if (format.Contains('/', StringComparison.Ordinal))
		{
			platformDatePicker.Text = datePicker.Date?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		}
		else
		{
			platformDatePicker.Text = datePicker.Date?.ToString(format) ?? string.Empty;
		}

		platformDatePicker.UpdateCharacterSpacing(datePicker);
	}

	public static void UpdateMinimumDate(this MauiDatePicker platformDatePicker, IDatePicker datePicker)
	{
		platformDatePicker.UpdateMinimumDate(datePicker, null);
	}

	public static void UpdateMinimumDate(this MauiDatePicker platformDatePicker, IDatePicker datePicker, UIDatePicker? picker)
	{
		picker?.UpdateMinimumDate(datePicker);
	}

	public static void UpdateMinimumDate(this UIDatePicker platformDatePicker, IDatePicker datePicker)
	{
		if (platformDatePicker is not null)
		{
			platformDatePicker.MinimumDate = datePicker.MinimumDate?.ToNSDate();
		}
	}

	public static void UpdateMaximumDate(this MauiDatePicker platformDatePicker, IDatePicker datePicker)
	{
		platformDatePicker.UpdateMaximumDate(datePicker, null);
	}

	public static void UpdateMaximumDate(this MauiDatePicker platformDatePicker, IDatePicker datePicker, UIDatePicker? picker)
	{
		picker?.UpdateMaximumDate(datePicker);
	}

	public static void UpdateMaximumDate(this UIDatePicker platformDatePicker, IDatePicker datePicker)
	{
		if (platformDatePicker is not null)
		{
			platformDatePicker.MaximumDate = datePicker.MaximumDate?.ToNSDate();
		}
	}

	public static void UpdateTextAlignment(this MauiDatePicker nativeDatePicker, IDatePicker datePicker)
	{
		var alignment = nativeDatePicker.EffectiveUserInterfaceLayoutDirection ==
				UIUserInterfaceLayoutDirection.RightToLeft
				? UITextAlignment.Right
				: UITextAlignment.Left;

		nativeDatePicker.TextAlignment = alignment;
	}

	internal static void UpdateIsOpen(this MauiDatePicker platformDatePicker, IDatePicker datePicker)
	{
		if (datePicker.IsOpen)
			platformDatePicker.BecomeFirstResponder();
		else
			platformDatePicker.ResignFirstResponder();
	}
}