using System;
using System.Collections.Generic;
using CoreFoundation;
using Foundation;
using Microsoft.Maui.Graphics;
using UIKit;

namespace Microsoft.Maui.Handlers
{
	public partial class TimePickerHandler : ViewHandler<ITimePicker, UIDatePicker>
	{
		readonly UIDatePickerProxy _proxy = new();

		bool _characterSpacingRefreshQueued;

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

		public override void PlatformArrange(Rect frame)
		{
			base.PlatformArrange(frame);

			// UIDatePicker builds and refreshes its segment text fields during its own layout
			// pass, discarding attributes that were applied before it, so the spacing has to be
			// re-applied for every arrange and again once that layout pass has completed.
			UpdateSegmentCharacterSpacing();
		}

		public static void MapFormat(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView?.UpdateFormat(timePicker);

			// Changing the format rebuilds the segments, so restyle them.
			MapCharacterSpacing(handler, timePicker);
		}

		public static void MapTime(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView?.UpdateTime(timePicker);

			// The segment text is regenerated for the new time, so restyle it.
			MapCharacterSpacing(handler, timePicker);
		}

		public static void MapCharacterSpacing(ITimePickerHandler handler, ITimePicker timePicker)
		{
			if (handler is TimePickerHandler timePickerHandler)
			{
				timePickerHandler.UpdateSegmentCharacterSpacing();
				return;
			}

			ApplyCharacterSpacing(handler.PlatformView, timePicker);
		}

		void UpdateSegmentCharacterSpacing()
		{
			var platformView = ((IElementHandler)this).PlatformView as UIDatePicker;
			var virtualView = ((IElementHandler)this).VirtualView as ITimePicker;

			if (platformView is null || virtualView is null)
				return;

			ApplyCharacterSpacing(platformView, virtualView);

			if (_characterSpacingRefreshQueued)
				return;

			// The segments the picker lays out after this point would otherwise keep UIKit's
			// unspaced text, so schedule a single follow-up for when the layout pass is done.
			_characterSpacingRefreshQueued = true;

			DispatchQueue.MainQueue.DispatchAsync(() =>
			{
				_characterSpacingRefreshQueued = false;
				ApplyCharacterSpacing(platformView, virtualView);
			});
		}

		static bool ApplyCharacterSpacing(UIDatePicker? platformView, ITimePicker? timePicker)
		{
			if (platformView is null || timePicker is null)
				return false;

			var characterSpacing = NSNumber.FromDouble(timePicker.CharacterSpacing);
			var applied = false;

			foreach (var textField in GetSegmentTextFields(platformView))
			{
				// Storing the kerning in the segment's default attributes keeps the spacing when
				// UIKit regenerates the segment text (time edits, format changes, re-layout).
				var attributes = textField.WeakDefaultTextAttributes is NSDictionary existing && existing.Count > 0
					? new NSMutableDictionary(existing)
					: new NSMutableDictionary();

				attributes[UIStringAttributeKey.KerningAdjustment] = characterSpacing;
				textField.WeakDefaultTextAttributes = attributes;

				// Restyle the text that is already displayed. Skipped while the segment is being
				// edited so the caret and selection are left alone.
				var attributedText = textField.AttributedText;
				if (attributedText is not null && attributedText.Length > 0)
				{
					if (!textField.IsEditing)
					{
						var spacedText = new NSMutableAttributedString(attributedText);
						spacedText.AddAttribute(
							UIStringAttributeKey.KerningAdjustment,
							characterSpacing,
							new NSRange(0, spacedText.Length));

						textField.AttributedText = spacedText;
					}

					applied = true;
				}
			}

			return applied;
		}

		// The compact UIDatePicker on MacCatalyst renders its time as internal UITextField
		// segments; they are the only text-bearing views the picker exposes.
		static IEnumerable<UITextField> GetSegmentTextFields(UIView view)
		{
			foreach (var subview in view.Subviews)
			{
				if (subview is UITextField textField)
				{
					yield return textField;
				}
				else
				{
					foreach (var nested in GetSegmentTextFields(subview))
					{
						yield return nested;
					}
				}
			}
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