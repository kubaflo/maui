#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;

namespace Microsoft.Maui.Controls
{
	public partial class ContentPage
	{
		internal new static void RemapForControls()
		{
			PageHandler.Mapper.ReplaceMapping<ContentPage, IPageHandler>(nameof(ContentPage.HideSoftInputOnTapped), MapHideSoftInputOnTapped);
			PageHandler.Mapper.ReplaceMapping<ContentPage, IPageHandler>(nameof(PlatformConfiguration.iOSSpecific.Page.PrefersHomeIndicatorAutoHiddenProperty), MapPrefersHomeIndicatorAutoHiddenOnPropertyChanged);
		}

		static void MapHideSoftInputOnTapped(IPageHandler handler, ContentPage page)
		{
			page.UpdateHideSoftInputOnTapped();
		}

		static void MapPrefersHomeIndicatorAutoHiddenOnPropertyChanged(IPageHandler handler, ContentPage page)
		{
			page.UpdateHideSoftInputOnTapped();
		}

		void UpdateHideSoftInputOnTapped()
		{
			Handler
				?.GetService<HideSoftInputOnTappedChangedManager>()
				?.UpdatePage(this);
		}
	}
}
