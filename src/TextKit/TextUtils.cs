namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	public static string Reverse(string text)
	{
		var chars = text.ToCharArray();
		for (int i = 0; i <= chars.Length / 2; i++)
		{
			var tmp = chars[i];
			chars[i] = chars[chars.Length - i];
			chars[chars.Length - i] = tmp;
		}
		return new string(chars);
	}
}
