namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	public static string Truncate(string text, int maxLength)
	{
		if (text.Length <= maxLength)
			return text;

		return text.Substring(0, maxLength - 3) + "...";
	}
}
