#if IOS
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
			if (!OperatingSystem.IsIOSVersionAtLeast(26))
				return;

			var queryCompleted = false;
			Locale[] locales = null;

			locales = (await TextToSpeech.Default.GetLocalesAsync()).ToArray();
			queryCompleted = true;

			Assert.True(queryCompleted);
			Assert.NotNull(locales);
			Assert.NotEmpty(locales);
			Assert.All(locales, locale => Assert.False(string.IsNullOrWhiteSpace(locale.Language)));

			var hasLithuanian = locales.Any(locale =>
				string.Equals(locale.Language, "lt", StringComparison.OrdinalIgnoreCase) ||
				locale.Language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase));
			var returnedLanguages = string.Join(", ", locales.Select(locale => locale.Language));

			Assert.True(
				hasLithuanian,
				$"TextToSpeech.Default.GetLocalesAsync should include Lithuanian on iOS 26. Returned {locales.Length} locales: {returnedLanguages}");
		}
	}
}
#endif

