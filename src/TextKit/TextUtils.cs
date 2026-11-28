using System;
using System.Globalization;

namespace TextKit;

public static class TextUtils
{
	/// <summary>Returns <paramref name="text"/> with its first character in upper case.</summary>
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

	/// <summary>Reverses the order of the Unicode text elements in <paramref name="text"/>.</summary>
	/// <returns>The reversed text, or an empty string when <paramref name="text"/> is <see langword="null"/>.</returns>
	public static string ReverseText(string? text)
	{
		if (text is null)
			return string.Empty;

		return string.Create(text.Length, text, static (destination, source) =>
		{
			var remaining = source.AsSpan();
			var offset = destination.Length;
			while (!remaining.IsEmpty)
			{
				var length = StringInfo.GetNextTextElementLength(remaining);
				// Scan forward for boundaries, but place each intact element from right to left.
				offset -= length;
				remaining[..length].CopyTo(destination[offset..]);
				remaining = remaining[length..];
			}
		});
	}
}
