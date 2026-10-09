using System;
using System.Globalization;
using System.Runtime.InteropServices;
using CoreFoundation;
using Foundation;
using UIKit;

namespace Microsoft.Maui.Platform
{
	public class MauiDatePicker : NoCaretField
	{
#if !MACCATALYST
		readonly UIDatePickerProxy _proxy = new();
		readonly RunLoopObserverCallback _runLoopObserverCallback;
		IntPtr _runLoopObserver;
		string _renderedCultureName = CultureInfo.CurrentCulture.Name;

		public MauiDatePicker()
		{
			_runLoopObserverCallback = OnRunLoopActivity;
			BorderStyle = UITextBorderStyle.RoundedRect;
			var picker = new UIDatePicker { Mode = UIDatePickerMode.Date, TimeZone = new NSTimeZone("UTC") };

			if (OperatingSystem.IsIOSVersionAtLeast(13, 4))
			{
				picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;
			}

			this.InputView = picker;
			var accessoryView = new MauiDoneAccessoryView();
			this.InputAccessoryView = accessoryView;

			accessoryView.SetDataContext(this);
			accessoryView.SetDoneClicked(OnDoneClicked);

			this.InputView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight;
			this.InputAccessoryView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight;

			this.InputAssistantItem.LeadingBarButtonGroups = null;
			this.InputAssistantItem.TrailingBarButtonGroups = null;

			this.AccessibilityTraits = UIAccessibilityTrait.Button;

			this.EditingDidBegin += OnStarted;
			this.EditingDidEnd += OnEnded;
			picker.ValueChanged += _proxy.OnValueChanged;
		}

		internal event EventHandler? CultureChanged;

		protected override void Dispose(bool disposing)
		{
			StopCultureObservation();
			base.Dispose(disposing);
		}

		internal void StartCultureObservation()
		{
			if (_runLoopObserver != IntPtr.Zero)
				return;

			_renderedCultureName = CultureInfo.CurrentCulture.Name;
			_runLoopObserver = CFRunLoopObserverCreate(
				IntPtr.Zero,
				(uint)CFRunLoopActivity.BeforeWaiting,
				true,
				0,
				_runLoopObserverCallback,
				IntPtr.Zero);
			CFRunLoopAddObserver(CFRunLoopGetMain(), _runLoopObserver, CFRunLoop.ModeCommon.Handle);
		}

		internal void StopCultureObservation()
		{
			if (_runLoopObserver == IntPtr.Zero)
				return;

			CFRunLoopRemoveObserver(CFRunLoopGetMain(), _runLoopObserver, CFRunLoop.ModeCommon.Handle);
			CFRelease(_runLoopObserver);
			_runLoopObserver = IntPtr.Zero;
		}

		void OnRunLoopActivity(IntPtr observer, CFRunLoopActivity activity, IntPtr info)
		{
			var cultureName = CultureInfo.CurrentCulture.Name;
			if (string.Equals(_renderedCultureName, cultureName, StringComparison.Ordinal))
				return;

			_renderedCultureName = cultureName;
			CultureChanged?.Invoke(this, EventArgs.Empty);
		}

		static void OnDoneClicked(object obj)
		{
			if (obj is MauiDatePicker mdp)
				mdp.MauiDatePickerDelegate?.DoneClicked();
		}

		void OnEnded(object? sender, EventArgs e) =>
			MauiDatePickerDelegate?.DatePickerEditingDidEnd();

		void OnStarted(object? sender, EventArgs e) =>
			MauiDatePickerDelegate?.DatePickerEditingDidBegin();

		internal MauiDatePickerDelegate? MauiDatePickerDelegate
		{
			get => _proxy.MauiDatePickerDelegate;
			set => _proxy.MauiDatePickerDelegate = value;
		}

		internal UIDatePicker? DatePickerDialog { get { return InputView as UIDatePicker; } }

		[Flags]
		enum CFRunLoopActivity : uint
		{
			BeforeWaiting = 1 << 5,
		}

		delegate void RunLoopObserverCallback(IntPtr observer, CFRunLoopActivity activity, IntPtr info);

		const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

		[DllImport(CoreFoundationLibrary)]
		static extern IntPtr CFRunLoopGetMain();

		[DllImport(CoreFoundationLibrary)]
		static extern IntPtr CFRunLoopObserverCreate(
			IntPtr allocator,
			uint activities,
			[MarshalAs(UnmanagedType.I1)] bool repeats,
			nint order,
			RunLoopObserverCallback callback,
			IntPtr context);

		[DllImport(CoreFoundationLibrary)]
		static extern void CFRunLoopAddObserver(IntPtr runLoop, IntPtr observer, IntPtr mode);

		[DllImport(CoreFoundationLibrary)]
		static extern void CFRunLoopRemoveObserver(IntPtr runLoop, IntPtr observer, IntPtr mode);

		[DllImport(CoreFoundationLibrary)]
		static extern void CFRelease(IntPtr value);

		class UIDatePickerProxy
		{
			internal MauiDatePickerDelegate? MauiDatePickerDelegate { get; set; }

			public void OnValueChanged(object? sender, EventArgs e) =>
				MauiDatePickerDelegate?.DatePickerValueChanged();
		}
#else
		public MauiDatePicker()
		{
			BorderStyle = UITextBorderStyle.RoundedRect;
		}
#endif
	}
}