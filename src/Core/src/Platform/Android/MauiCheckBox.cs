using Android.Content;
using Android.Runtime;
using Android.Util;
using Google.Android.Material.CheckBox;

namespace Microsoft.Maui.Platform
{
	/// <summary>
	/// Custom MaterialCheckBox that properly handles layout alignment
	/// without being affected by Material Design's minimum touch target enforcement
	/// </summary>
	public class MauiCheckBox : MaterialCheckBox
	{
		public MauiCheckBox(Context context) : base(context)
		{
			Initialize();
		}

		public MauiCheckBox(Context context, IAttributeSet? attrs) : base(context, attrs)
		{
			Initialize();
		}

		public MauiCheckBox(Context context, IAttributeSet? attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
		{
			Initialize();
		}

		protected MauiCheckBox(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		void Initialize()
		{
			// MaterialCheckBox enforces a 48dp minimum touch target which can cause
			// alignment issues. We need to ensure the CheckBox measures correctly
			// for layout purposes while still maintaining proper touch targets.
			// Setting MinimumWidth and MinimumHeight to 0 allows the CheckBox
			// to be measured based on its actual content size.
			MinimumWidth = 0;
			MinimumHeight = 0;
		}
	}
}
