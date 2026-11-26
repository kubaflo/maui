namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	/// <summary>Returns uppercase initials from the first Unicode character of each nonempty whitespace-separated part.</summary>
	public static string Initials(string fullName)
	{
		var parts = fullName.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries);
		var result = "";
		foreach (var part in parts)
		{
			if (char.IsSurrogatePair(part, 0))
				result += part[..2];
			else
				result += part[0];
		}
		return result.ToUpper();
	}
}
