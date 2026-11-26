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
	/// Determines whether <paramref name="text"/> is a palindrome using ordinal, case-sensitive comparison.
	/// For example, <c>Aba</c> is not a palindrome.
	/// </summary>
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
