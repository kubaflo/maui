namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	public static string Initials(string fullName)
	{
		var parts = fullName.Split(' ');
		var result = "";
		foreach (var part in parts)
			result += part[0];
		return result.ToUpper();
	}
}
