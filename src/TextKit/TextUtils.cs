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

	/// <summary>Counts the nonempty words separated by whitespace, returning zero for <see langword="null"/>.</summary>
	public static int WordCount(string? text)
	{
		if (text is null)
			return 0;

		return text.Split(separator: (char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
	}
}
