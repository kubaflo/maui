#if IOS && !MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
	[Category("Issue34530")]
	public class Issue34530
	{
		[Fact]
		public async Task GetLocalesAsyncIncludesLithuanian()
		{
			var locales = await TextToSpeech.Default.GetLocalesAsync();

			Assert.NotNull(locales);

			var languages = locales.Select(locale => locale.Language).ToArray();

			Assert.True(
				languages.Any(IsLithuanian),
				$"TextToSpeech.Default.GetLocalesAsync returned no Lithuanian locale; observed languages: {string.Join(", ", languages)}");
		}

		static bool IsLithuanian(string language) =>
			!string.IsNullOrEmpty(language) &&
			(string.Equals(language, "lt", StringComparison.OrdinalIgnoreCase) ||
			language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase) ||
			language.StartsWith("lt_", StringComparison.OrdinalIgnoreCase));
	}
}
#endif

