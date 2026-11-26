namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	public static bool IsPalindrome(string text)
	{
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] != text[text.Length - 1 - i])
				return false;
		}
		return true;
	}
}
