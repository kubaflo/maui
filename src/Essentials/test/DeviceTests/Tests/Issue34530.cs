using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
#if IOS && !MACCATALYST
	[Category("Issue34530")]
	public class Issue34530
	{
		[Fact]
		public async Task GetLocalesAsyncReturnsLithuanianLocale()
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(26))
				return;

			var localeQueryCompleted = false;
			var returnedLocales = await TextToSpeech.Default.GetLocalesAsync();
			localeQueryCompleted = true;

			Assert.True(localeQueryCompleted, "The Text-to-Speech locale query did not complete.");
			Assert.NotNull(returnedLocales);

			var locales = returnedLocales.ToArray();
			Assert.NotEmpty(locales);

			var hasLithuanian = locales.Any(locale =>
				string.Equals(locale.Language, "lt", StringComparison.OrdinalIgnoreCase) ||
				locale.Language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase) ||
				locale.Language.StartsWith("lt_", StringComparison.OrdinalIgnoreCase));
			var languageCodes = string.Join(", ", locales.Select(locale => locale.Language));

			Assert.True(
				hasLithuanian,
				$"TextToSpeech locales did not contain Lithuanian; returned locale count={locales.Length}; language codes={languageCodes}");
		}
	}
#endif
}

