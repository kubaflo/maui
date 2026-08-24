using System;
using Microsoft.Maui.Graphics;
using UIKit;

namespace Microsoft.Maui.Handlers
{
#if !MACCATALYST
	public partial class DatePickerHandler : ViewHandler<IDatePicker, MauiDatePicker>
	{
		protected override MauiDatePicker CreatePlatformView()
		{
			MauiDatePicker platformDatePicker = new MauiDatePicker();
			return platformDatePicker;
		}

		internal UIDatePicker? DatePickerDialog { get { return PlatformView?.InputView as UIDatePicker; } }

		internal bool UpdateImmediately { get; set; }

		// Snapshot of the platform view's background before MAUI applies any Background paint.
		// Restoring it is how a cleared Background returns the control to its UIKit default.
		UIColor? _defaultBackgroundColor;
		bool _hasDefaultBackgroundColor;

		protected override void ConnectHandler(MauiDatePicker platformView)
		{
			_defaultBackgroundColor = platformView.BackgroundColor;
			_hasDefaultBackgroundColor = true;

			platformView.MauiDatePickerDelegate = new DatePickerDelegate(this);

			if (DatePickerDialog is UIDatePicker picker)
			{
				var date = VirtualView?.Date;
				if (date is not null && date is DateTime dt)
				{
					picker.Date = dt.ToNSDate();
				}
			}

			base.ConnectHandler(platformView);
		}

		protected override void DisconnectHandler(MauiDatePicker platformView)
		{
			platformView.MauiDatePickerDelegate = null;

			_defaultBackgroundColor = null;
			_hasDefaultBackgroundColor = false;

			base.DisconnectHandler(platformView);
		}

		internal static void MapBackground(IDatePickerHandler handler, IDatePicker datePicker)
		{
			var platformView = handler.PlatformView;

			if (platformView is null)
				return;

			platformView.UpdateBackground(datePicker);

			// UpdateBackground leaves the last applied color in place for controls that are
			// neither LayoutView nor ContentView, so clearing the Background has to explicitly
			// put the platform default back.
			if (datePicker.Background.IsNullOrEmpty() &&
				handler is DatePickerHandler { _hasDefaultBackgroundColor: true } platformHandler)
			{
				platformView.BackgroundColor = platformHandler._defaultBackgroundColor;
			}
		}

		public static partial void MapFormat(IDatePickerHandler handler, IDatePicker datePicker)
		{
			var picker = (handler as DatePickerHandler)?.DatePickerDialog;
			handler.PlatformView?.UpdateFormat(datePicker, picker);
		}

		public static partial void MapDate(IDatePickerHandler handler, IDatePicker datePicker)
		{
			var picker = (handler as DatePickerHandler)?.DatePickerDialog;
			handler.PlatformView?.UpdateDate(datePicker, picker);
		}

		public static partial void MapMinimumDate(IDatePickerHandler handler, IDatePicker datePicker)
		{
			if (handler is DatePickerHandler platformHandler)
				handler.PlatformView?.UpdateMinimumDate(datePicker, platformHandler.DatePickerDialog);
		}

		public static partial void MapMaximumDate(IDatePickerHandler handler, IDatePicker datePicker)
		{
			if (handler is DatePickerHandler platformHandler)
				handler.PlatformView?.UpdateMaximumDate(datePicker, platformHandler.DatePickerDialog);
		}

		public static partial void MapCharacterSpacing(IDatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateCharacterSpacing(datePicker);
		}

		public static partial void MapFont(IDatePickerHandler handler, IDatePicker datePicker)
		{
			var fontManager = handler.GetRequiredService<IFontManager>();

			handler.PlatformView?.UpdateFont(datePicker, fontManager);
		}

		public static partial void MapTextColor(IDatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateTextColor(datePicker);
		}

		public static partial void MapFlowDirection(IDatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateFlowDirection(datePicker);
			handler.PlatformView?.UpdateTextAlignment(datePicker);
		}

		internal static partial void MapIsOpen(IDatePickerHandler handler, IDatePicker datePicker)
		{
			handler.PlatformView?.UpdateIsOpen(datePicker);
		}

		static void OnValueChanged(object? sender)
		{
			if (sender is DatePickerHandler datePickerHandler)
			{
				if (datePickerHandler.UpdateImmediately)  // Platform Specific
					datePickerHandler.SetVirtualViewDate();

				if (datePickerHandler.VirtualView != null)
					datePickerHandler.VirtualView.IsFocused = true;
			}
		}

		static void OnStarted(object? sender)
		{
			if (sender is IDatePickerHandler datePickerHandler && datePickerHandler.VirtualView != null)
			{
				datePickerHandler.VirtualView.IsFocused = datePickerHandler.VirtualView.IsOpen = true;

				// Notify VoiceOver that the date picker popup has appeared
				if (datePickerHandler.PlatformView?.InputView is not null)
				{
					datePickerHandler.PlatformView.PostAccessibilityFocusNotification(datePickerHandler.PlatformView.InputView);
				}
			}
		}

		static void OnEnded(object? sender)
		{
			if (sender is IDatePickerHandler datePickerHandler && datePickerHandler.VirtualView != null)
			{
				datePickerHandler.VirtualView.IsFocused = datePickerHandler.VirtualView.IsOpen = false;

				// Restore VoiceOver focus to the date picker field when the popup closes
				datePickerHandler.PlatformView?.PostAccessibilityFocusNotification();
			}
		}

		static void OnDoneClicked(object? sender)
		{
			if (sender is DatePickerHandler handler)
			{
				handler.SetVirtualViewDate();
				handler.PlatformView.ResignFirstResponder();
			}
		}

		void SetVirtualViewDate()
		{
			if (VirtualView is null || DatePickerDialog is null)
			{
				return;
			}

			VirtualView.Date = DatePickerDialog.Date.ToDateTime();
		}

		class DatePickerDelegate : MauiDatePickerDelegate
		{
			readonly WeakReference<IDatePickerHandler> _handler;

			public DatePickerDelegate(IDatePickerHandler handler) =>
				_handler = new WeakReference<IDatePickerHandler>(handler);

			IDatePickerHandler? Handler
			{
				get
				{
					if (_handler?.TryGetTarget(out IDatePickerHandler? target) == true)
						return target;

					return null;
				}
			}

			public override void DatePickerEditingDidBegin()
			{
				DatePickerHandler.OnStarted(Handler);
			}

			public override void DatePickerEditingDidEnd()
			{
				DatePickerHandler.OnEnded(Handler);
			}

			public override void DatePickerValueChanged()
			{
				DatePickerHandler.OnValueChanged(Handler);
			}

			public override void DoneClicked()
			{
				DatePickerHandler.OnDoneClicked(Handler);
			}
		}
	}
#endif
}
