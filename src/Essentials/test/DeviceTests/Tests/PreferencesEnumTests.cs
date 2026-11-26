using System;
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
