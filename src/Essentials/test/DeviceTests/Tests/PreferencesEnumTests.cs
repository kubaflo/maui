using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
	[Category("Preferences")]
	[Collection("UsesPreferences")]
	public class PreferencesEnumTests
	{
		enum UnsignedOptions : ulong
		{
			None = 0,
			HighBit = 1UL << 63,
			All = ulong.MaxValue,
		}

		[Fact]
		public void AppThemeAndPrimitiveRoundTrip()
		{
			var preferences = Preferences.Default;
			var themeKey = Guid.NewGuid().ToString();
			var intKey = Guid.NewGuid().ToString();

			try
			{
				Assert.Equal(AppTheme.Light, preferences.Get(themeKey, AppTheme.Light));

				preferences.Set(themeKey, AppTheme.Dark);
				Assert.Equal(AppTheme.Dark, preferences.Get(themeKey, AppTheme.Unspecified));

				preferences.Set(intKey, 42);
				Assert.Equal(42, preferences.Get(intKey, 0));
			}
			finally
			{
				preferences.Remove(themeKey);
				preferences.Remove(intKey);
			}
		}

		[Theory]
		[InlineData((ulong)UnsignedOptions.HighBit)]
		[InlineData((ulong)UnsignedOptions.All)]
		public void UnsignedEnumPreservesAllBits(ulong rawValue)
		{
			var value = (UnsignedOptions)rawValue;
			var preferences = Preferences.Default;
			var key = Guid.NewGuid().ToString();

			try
			{
				Assert.Equal(value, preferences.Get(key, value));

				preferences.Set(key, value);
				Assert.Equal(value, preferences.Get(key, UnsignedOptions.None));
			}
			finally
			{
				preferences.Remove(key);
			}
		}
	}
}
