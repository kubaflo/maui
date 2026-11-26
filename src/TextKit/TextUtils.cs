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
		var slug = title.ToLowerInvariant().Replace(' ', '-');
		while (slug.Contains("--"))
			slug = slug.Replace("--", "-");

		slug = slug.Trim('-');
		const int MaxSlugLength = 50;
		if (slug.Length <= MaxSlugLength)
			return slug;

		var length = MaxSlugLength;
		if (char.IsSurrogatePair(slug[length - 1], slug[length]))
			length--;
		return slug.Substring(0, length);
	}
}
