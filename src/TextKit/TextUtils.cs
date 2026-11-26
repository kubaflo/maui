namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	/// <summary>Returns <paramref name="text"/> repeated <paramref name="count"/> times.</summary>
	public static string Repeat(string text, int count)
	{
		var result = "";
		for (int i = 0; i < count; i++)
			result += text;
		return result;
	}
}
