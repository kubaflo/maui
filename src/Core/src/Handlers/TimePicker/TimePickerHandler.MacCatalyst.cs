using System;
using Foundation;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui.Handlers
{
	public partial class TimePickerHandler : ViewHandler<ITimePicker, UIDatePicker>
	{
		readonly UIDatePickerProxy _proxy = new();

		protected override UIDatePicker CreatePlatformView()
		{
			return new UIDatePicker { Mode = UIDatePickerMode.Time, TimeZone = new NSTimeZone("UTC") };
		}

		internal bool UpdateImmediately { get; set; } = true;

		protected override void ConnectHandler(UIDatePicker platformView)
		{
			base.ConnectHandler(platformView);

			_proxy.Connect(this, VirtualView, platformView);
		}

		protected override void DisconnectHandler(UIDatePicker platformView)
		{
			base.DisconnectHandler(platformView);

			_proxy.Disconnect(platformView);
		}

		public static void MapFormat(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView?.UpdateFormat(timePicker);
		}

		public static void MapTime(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView?.UpdateTime(timePicker);
		}

		public static void MapCharacterSpacing(ITimePickerHandler handler, ITimePicker timePicker)
		{
			// Mac Catalyst renders the TimePicker with a plain UIDatePicker, which owns its own text
			// rendering and exposes no text API, so the tracking cannot be pushed into the control.
			// The handler honors CharacterSpacing in its size contract instead, so re-measure here.
			if (handler.PlatformView is not null)
			{
				timePicker.InvalidateMeasure();
			}
		}

		Size IViewHandler.GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			var size = base.GetDesiredSize(widthConstraint, heightConstraint);

			if (((IViewHandler)this).PlatformView is UIDatePicker picker &&
				VirtualView is ITimePicker timePicker)
			{
				var extraWidth = picker.GetCharacterSpacingWidth(timePicker);

				if (extraWidth > 0)
				{
					size = new Size(size.Width + extraWidth, size.Height);
				}
			}

			return size;
		}

		public static void MapFont(ITimePickerHandler handler, ITimePicker timePicker)
		{
			var fontManager = handler.GetRequiredService<IFontManager>();

			//handler.PlatformView?.UpdateFont(timePicker, fontManager);
		}

		public static void MapTextColor(ITimePickerHandler handler, ITimePicker timePicker)
		{
			//handler.PlatformView?.UpdateTextColor(timePicker, DefaultTextColor);
		}

		public static void MapFlowDirection(TimePickerHandler handler, ITimePicker timePicker)
		{
			// handler.PlatformView?.UpdateFlowDirection(timePicker);
			// handler.PlatformView?.UpdateTextAlignment(timePicker);
		}

		internal static void MapIsOpen(ITimePickerHandler handler, ITimePicker timePicker)
		{

		}

		void SetVirtualViewTime()
		{
			if (VirtualView == null || PlatformView == null)
				return;

			var datetime = PlatformView.Date.ToDateTime();
			VirtualView.Time = new TimeSpan(datetime.Hour, datetime.Minute, 0);
		}

		class UIDatePickerProxy
		{
			WeakReference<TimePickerHandler>? _handler;
			WeakReference<ITimePicker>? _virtualView;

			ITimePicker? VirtualView => _virtualView is not null && _virtualView.TryGetTarget(out var v) ? v : null;

			public void Connect(TimePickerHandler handler, ITimePicker virtualView, UIDatePicker platformView)
			{
				_handler = new(handler);
				_virtualView = new(virtualView);

				platformView.EditingDidBegin += OnStarted;
				platformView.EditingDidEnd += OnEnded;
				platformView.ValueChanged += OnValueChanged;
			}

			public void Disconnect(UIDatePicker platformView)
			{
				_virtualView = null;

				platformView.EditingDidBegin -= OnStarted;
				platformView.EditingDidEnd -= OnEnded;
				platformView.ValueChanged -= OnValueChanged;
				platformView.RemoveFromSuperview();
			}

			void OnStarted(object? sender, EventArgs eventArgs)
			{
				if (VirtualView is ITimePicker virtualView)
					virtualView.IsFocused = true;
			}

			void OnEnded(object? sender, EventArgs eventArgs)
			{
				if (VirtualView is ITimePicker virtualView)
					virtualView.IsFocused = false;
			}

			void OnValueChanged(object? sender, EventArgs e)
			{
				if (_handler is not null && _handler.TryGetTarget(out var handler) && handler.UpdateImmediately)
				{
					handler.SetVirtualViewTime();
				}
			}
		}
	}
}