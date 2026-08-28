#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.Maui.Platform
{
	public static class SliderExtensions
	{
		static void UpdateIncrement(this Slider nativeSlider, ISlider slider)
		{
			var difference = slider.Maximum - slider.Minimum;

			double stepping = 1;

			// Setting the Slider SmallChange property to 0 would throw an System.ArgumentException.
			if (difference != 0)
			{
				stepping = Math.Min((difference) / 1000, 1);
			}

			nativeSlider.StepFrequency = stepping;
		}

		public static void UpdateMinimum(this Slider nativeSlider, ISlider slider)
		{
			nativeSlider.Minimum = slider.Minimum;
			nativeSlider.UpdateIncrement(slider);
		}

		public static void UpdateMaximum(this Slider nativeSlider, ISlider slider)
		{
			nativeSlider.Maximum = slider.Maximum;
			nativeSlider.UpdateIncrement(slider);
		}

		public static void UpdateValue(this Slider nativeSlider, ISlider slider)
		{
			if (nativeSlider.Value != slider.Value)
			{
				nativeSlider.Value = slider.Value;
			}
		}

		public static void UpdateMinimumTrackColor(this Slider platformSlider, ISlider slider)
		{
			UpdateColor(platformSlider, MinimumTrackColorResourceKeys, slider.MinimumTrackColor?.ToPlatform());
		}

		static readonly string[] MinimumTrackColorResourceKeys =
		{
			"SliderTrackValueFill",
			"SliderTrackValueFillPointerOver",
			"SliderTrackValueFillPressed",
			"SliderTrackValueFillDisabled",
		};

		public static void UpdateMaximumTrackColor(this Slider platformSlider, ISlider slider)
		{
			UpdateColor(platformSlider, MaximumTrackColorResourceKeys, slider.MaximumTrackColor?.ToPlatform());
		}

		static readonly string[] MaximumTrackColorResourceKeys =
		{
			"SliderTrackFill",
			"SliderTrackFillPointerOver",
			"SliderTrackFillPressed",
			"SliderTrackFillDisabled",
		};

		public static void UpdateThumbColor(this Slider platformSlider, ISlider slider)
		{
			UpdateColor(platformSlider, ThumbColorResourceKeys, slider.ThumbColor?.ToPlatform());
		}

		static readonly string[] ThumbColorResourceKeys =
		{
			"SliderThumbBackground",
			"SliderThumbBackgroundPointerOver",
			"SliderThumbBackgroundPressed",
			"SliderThumbBackgroundDisabled",
		};

		internal static async Task UpdateThumbImageSourceAsync(this MauiSlider nativeSlider, ISlider slider, IImageSourceServiceProvider? provider, Size? defaultThumbSize)
		{
			var thumbImageSource = slider.ThumbImageSource;

			if (thumbImageSource == null)
			{
				nativeSlider.ThumbImageSource = null;

				var thumb = nativeSlider.GetFirstDescendant<Thumb>();

				if (thumb is not null)
				{
					ApplyDefaultThumbSize(thumb, defaultThumbSize);
				}

				return;
			}

			if (provider != null && thumbImageSource != null)
			{
				var service = provider.GetRequiredImageSourceService(thumbImageSource);
				var nativeThumbImageSource = await service.GetImageSourceAsync(thumbImageSource);
				var nativeThumbImage = nativeThumbImageSource?.Value;

				// BitmapImage is a special case that has an event when the image is loaded
				// when this happens, we want to resize the thumb
				if (nativeThumbImage is BitmapImage bitmapImage)
				{
					bitmapImage.ImageOpened += OnImageOpened;

					void OnImageOpened(object sender, RoutedEventArgs e)
					{
						bitmapImage.ImageOpened -= OnImageOpened;

						// The thumb keeps the size it had before the image was applied; the image
						// is scaled uniformly into it. Sizing the thumb to the bitmap's pixel
						// dimensions would make the thumb as large as the source image.
						if (nativeSlider.TryGetFirstDescendant<Thumb>(out var thumb))
						{
							ApplyDefaultThumbSize(thumb, defaultThumbSize);
						}

						if (nativeSlider.Parent is FrameworkElement frameworkElement)
						{
							frameworkElement.InvalidateMeasure();
						}
					}
				}

				nativeSlider.ThumbImageSource = nativeThumbImageSource?.Value;
			}
		}

		// Restores the thumb to the size captured from the default template, so a thumb image
		// never changes the slider's thumb footprint.
		static void ApplyDefaultThumbSize(Thumb thumb, Size? defaultThumbSize)
		{
			if (!defaultThumbSize.HasValue)
			{
				return;
			}

			var width = defaultThumbSize.Value.Width;
			var height = defaultThumbSize.Value.Height;

			if (width >= 0 && !double.IsNaN(width) && !double.IsInfinity(width))
			{
				thumb.Width = width;
			}

			if (height >= 0 && !double.IsNaN(height) && !double.IsInfinity(height))
			{
				thumb.Height = height;
			}
		}

		static readonly string[] BackgroundColorResourceKeys =
		{
			"SliderContainerBackground",
			"SliderContainerBackgroundPointerOver",
			"SliderContainerBackgroundPressed",
			"SliderContainerBackgroundDisabled",
		};

		internal static void UpdateBackgroundColor(this MauiSlider platformSlider, ISlider slider)
		{
			UpdateColor(platformSlider, BackgroundColorResourceKeys, slider.Background?.ToPlatform());
		}

		static void UpdateColor(Slider platformSlider, string[] keys, Brush? brush)
		{
			ResourceDictionary resource = platformSlider.Resources;

			if (brush is null)
			{
				resource.RemoveKeys(keys);
			}
			else
			{
				resource.SetValueForAllKey(keys, brush);
			}

			platformSlider.RefreshThemeResources();
		}
	}
}