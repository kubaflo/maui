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

	/// <summary>Replaces all but the last <paramref name="visible"/> characters with asterisks.</summary>
	/// <remarks>If the requested visibility exceeds the length, the value is returned unchanged.</remarks>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="visible"/> is negative.</exception>
	public static string Mask(string value, int visible)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(visible);

		int maskedLength = value.Length - Math.Min(visible, value.Length);
		return new string('*', maskedLength) + value.Substring(maskedLength);
	}
}
