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

		// The accessory is a translucent bar stretched across the full width of the screen and docked
		// directly on top of application content. Its default hit testing claims every point in that
		// band, including the large empty run between the leading edge and the Done item, so a tap on
		// a control the user can plainly see behind the accessory never reaches that control. Claim a
		// touch only when it actually lands on one of the bar's interactive items and let every other
		// touch fall through to the content behind the accessory.
		public override UIView? HitTest(CGPoint point, UIEvent? uievent)
		{
			var hit = base.HitTest(point, uievent);

			if (hit is null)
				return null;

			for (var view = hit; view is not null; view = view.Superview)
			{
				if (view is UIControl)
					return hit;

				if (view.Handle == Handle)
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