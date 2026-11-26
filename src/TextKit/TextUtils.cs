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

	/// <summary>Counts the nonempty words separated by whitespace in the specified text.</summary>
	public static int WordCount(string text)
	{
		return text.Split(separator: (char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
	}
}
