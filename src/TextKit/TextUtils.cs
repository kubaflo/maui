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
	/// Creates a lowercase URL path slug of at most 50 UTF-16 code units.
	/// </summary>
	public static string Slugify(string title)
	{
		var characters = title.ToLowerInvariant().ToCharArray();
		for (var i = 0; i < characters.Length; i++)
		{
			var character = characters[i];
			if (char.IsAscii(character)
				? !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '.' or '_' or '~')
				: char.IsWhiteSpace(character) || char.IsControl(character))
			{
				characters[i] = '-';
			}
		}

		var slug = new string(characters);
		while (slug.Contains("--"))
			slug = slug.Replace("--", "-");

		slug = slug.Trim('-');
		if (slug is "." or "..")
			return "";

		const int MaxSlugLength = 50;
		if (slug.Length <= MaxSlugLength)
			return slug;

		var length = MaxSlugLength;
		if (char.IsSurrogatePair(slug[length - 1], slug[length]))
			length--;
		return slug.Substring(0, length);
	}

	/// <summary>Shortens <paramref name="text"/> to at most <paramref name="maxLength"/> characters, ending with "…".</summary>
	public static string Truncate(string text, int maxLength)
	{
		if (text.Length <= maxLength)
			return text;
		return text.Substring(0, maxLength) + "…";
	}
}
