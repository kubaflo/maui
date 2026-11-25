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
	/// Shortens text to the specified maximum length, adding an ellipsis when truncated.
	/// </summary>
	/// <param name="text">The text to truncate, or null.</param>
	/// <param name="maxLength">The maximum length of the result.</param>
	/// <returns>The truncated text, the original text if it fits, or an empty string for null.</returns>
	public static string Truncate(string? text, int maxLength)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		if (text.Length <= maxLength)
			return text;

		return text.Substring(0, maxLength - 3) + "...";
	}
}
