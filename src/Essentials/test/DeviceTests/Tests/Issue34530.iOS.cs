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
			if (!OperatingSystem.IsIOSVersionAtLeast(26))
				return;

			Locale[] completedLocales = null;

			completedLocales = (await TextToSpeech.Default.GetLocalesAsync()).ToArray();

			Assert.NotNull(completedLocales);
			Assert.NotEmpty(completedLocales);

			var observedLanguages = completedLocales
				.Select(locale => locale.Language)
				.OrderBy(language => language, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			var hasLithuanian = observedLanguages.Any(language =>
				string.Equals(language, "lt", StringComparison.OrdinalIgnoreCase) ||
				language.StartsWith("lt-", StringComparison.OrdinalIgnoreCase));

			Assert.True(
				hasLithuanian,
				$"TextToSpeech.Default.GetLocalesAsync omitted Lithuanian. Observed languages: {string.Join(", ", observedLanguages)}");
		}
	}
}
#endif

