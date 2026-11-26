namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	public static string Mask(string value, int visible)
	{
		return new string('*', value.Length - visible) + value.Substring(value.Length - visible);
	}
}
