namespace TextKit;

public static class TextUtils
{
	public static string Capitalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	public static string Slugify(string title)
	{
		var slug = title.ToLowerInvariant().Replace(' ', '-');
		while (slug.Contains("--"))
			slug = slug.Replace("--", "-");
		return slug.Trim('-').Substring(0, 50);
	}
}
