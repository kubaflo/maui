#if IOS && !MACCATALYST
using System;
using CoreGraphics;
using UIKit;

namespace Microsoft.Maui.Platform
{
	internal class MauiDoneAccessoryView : UIToolbar
	{
		readonly BarButtonItemProxy _proxy;

		public MauiDoneAccessoryView() : base(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, 44))
		{
			_proxy = new BarButtonItemProxy();
			BarStyle = UIBarStyle.Default;
			Translucent = true;
			var spacer = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);
			var doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done, _proxy.OnDataClicked);

			SetItems(new[] { spacer, doneButton }, false);
		}

		// The accessory spans the full width of the keyboard but is transparent, so any
		// content it overlaps (typically the text field sitting just above the keyboard)
		// stays visible. Swallowing touches over that empty area would make the visible
		// content untappable, so only real controls (the Done button) accept touches and
		// everything else falls through to whatever is rendered behind the accessory.
		public override UIView? HitTest(CGPoint point, UIEvent? uievent)
		{
			var hit = base.HitTest(point, uievent);

			if (hit is null)
				return null;

			for (UIView? view = hit; view is not null; view = view.Superview)
			{
				if (view is UIControl)
					return hit;

				if (view == this)
					break;
			}

			return null;
		}

		internal void SetDoneClicked(Action<object>? value) => _proxy.SetDoneClicked(value);


		internal void SetDataContext(object? dataContext) => _proxy.SetDataContext(dataContext);

		public MauiDoneAccessoryView(Action doneClicked) : base(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, 44))
		{
			_proxy = new BarButtonItemProxy(doneClicked);
			BarStyle = UIBarStyle.Default;
			Translucent = true;

			var spacer = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);
			var doneButton = new UIBarButtonItem(UIBarButtonSystemItem.Done, _proxy.OnClicked);
			SetItems(new[] { spacer, doneButton }, false);
		}

		class BarButtonItemProxy
		{
			readonly Action? _doneClicked;
			Action<object>? _doneWithDataClicked;
			WeakReference<object>? _data;

			public BarButtonItemProxy() { }

			public BarButtonItemProxy(Action doneClicked)
			{
				_doneClicked = doneClicked;
			}

			public void SetDoneClicked(Action<object>? value) => _doneWithDataClicked = value;

			public void SetDataContext(object? dataContext) => _data = dataContext is null ? null : new(dataContext);

			public void OnDataClicked(object? sender, EventArgs e)
			{
				if (_data is not null && _data.TryGetTarget(out var data))
					_doneWithDataClicked?.Invoke(data);
			}

			public void OnClicked(object? sender, EventArgs e) => _doneClicked?.Invoke();
		}
	}
}
#endif