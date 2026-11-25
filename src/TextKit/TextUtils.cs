using System;

namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	/// <summary>
	/// Shortens text to the specified maximum length, adding an ellipsis when truncated
	/// if the limit is at least three characters.
	/// </summary>
	/// <param name="text">The text to truncate, or null.</param>
	/// <param name="maxLength">The nonnegative maximum length of the result.</param>
	/// <returns>The truncated text, the original text if it fits, or an empty string for null.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The maximum length is negative.</exception>
	public static string Truncate(string? text, int maxLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

		if (string.IsNullOrEmpty(text))
			return string.Empty;

		if (text.Length <= maxLength)
			return text;

		if (maxLength < 3)
			return text.Substring(0, maxLength);

		return text.Substring(0, maxLength - 3) + "...";
	}
}
