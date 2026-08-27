using System.Globalization;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using WBrush = Microsoft.UI.Xaml.Media.Brush;

namespace Microsoft.Maui.Handlers
{
	public partial class TimePickerHandler : ViewHandler<ITimePicker, TimePicker>
	{
		CultureInfo? _appliedCulture;

		protected override TimePicker CreatePlatformView() => new TimePicker();

		protected override void ConnectHandler(TimePicker platformView)
		{
			platformView.SelectedTimeChanged += OnSelectedTimeChanged;
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
		}

		protected override void DisconnectHandler(TimePicker platformView)
		{
			platformView.SelectedTimeChanged -= OnSelectedTimeChanged;
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
			_appliedCulture = null;
		}

		public static void MapFormat(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView.UpdateTime(timePicker);
		}

		public static void MapTime(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView.UpdateTime(timePicker);
		}

		public static void MapCharacterSpacing(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView.UpdateCharacterSpacing(timePicker);
		}

		public static void MapFont(ITimePickerHandler handler, ITimePicker timePicker)
		{
			var fontManager = handler.GetRequiredService<IFontManager>();

			handler.PlatformView.UpdateFont(timePicker, fontManager);
		}

		public static void MapTextColor(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView.UpdateTextColor(timePicker);
		}

		public static void MapBackground(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView?.UpdateBackground(timePicker);
		}

		internal static void MapIsOpen(ITimePickerHandler handler, ITimePicker timePicker)
		{
			handler.PlatformView?.UpdateIsOpen(timePicker);
		}

		void OnSelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs e)
		{
			if (VirtualView is not null)
			{
				VirtualView.Time = e.NewTime;
				VirtualView.InvalidateMeasure();
			}
		}

		void OnRendering(object? sender, object e)
		{
			var culture = CultureInfo.CurrentCulture;
			if (ReferenceEquals(_appliedCulture, culture))
				return;

			_appliedCulture = culture;

			if (VirtualView is not null)
				PlatformView.UpdateClockIdentifier(VirtualView);
		}
	}
}