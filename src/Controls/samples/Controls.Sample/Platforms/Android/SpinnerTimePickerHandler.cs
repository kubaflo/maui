using System;
using Android.App;
using Android.Text.Format;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Controls.Sample.Platform;

public class SpinnerTimePickerHandler : TimePickerHandler
{
	static readonly IPropertyMapper<ITimePicker, SpinnerTimePickerHandler> SpinnerMapper =
		new PropertyMapper<ITimePicker, SpinnerTimePickerHandler>(TimePickerHandler.Mapper);

	TimePickerDialog? _spinnerDialog;

	static SpinnerTimePickerHandler()
	{
		SpinnerMapper.AppendToMapping(nameof(ITimePicker.IsOpen), (handler, picker) =>
		{
			if (!picker.IsOpen)
			{
				handler.ReleaseDialog();
			}
		});
	}

	public SpinnerTimePickerHandler() : base(SpinnerMapper)
	{
	}

	protected override TimePickerDialog CreateTimePickerDialog(int hour, int minute)
	{
		ReleaseDialog();

		var format = VirtualView.Format;
		var use24HourView = format == "t"
			? DateFormat.Is24HourFormat(Context)
			: format?.Contains("HH", StringComparison.Ordinal) == true;

		_spinnerDialog = new TimePickerDialog(
			Context,
			Resource.Style.SpinnerTimePickerDialog,
			OnTimeSet,
			hour,
			minute,
			use24HourView);
		_spinnerDialog.DismissEvent += OnDialogDismissed;

		return _spinnerDialog;
	}

	void OnTimeSet(object? sender, TimePickerDialog.TimeSetEventArgs e)
	{
		if (VirtualView is not null)
		{
			VirtualView.Time = new TimeSpan(e.HourOfDay, e.Minute, 0);
			VirtualView.IsFocused = false;
		}
	}

	void OnDialogDismissed(object? sender, EventArgs e)
	{
		if (ReferenceEquals(sender, _spinnerDialog) && VirtualView is not null)
		{
			VirtualView.IsFocused = false;
			VirtualView.IsOpen = false;
		}
	}

	protected override void DisconnectHandler(MauiTimePicker platformView)
	{
		// Dispose after the base handler has dropped its reference to the dialog.
		using var dialog = _spinnerDialog;
		_spinnerDialog = null;

		if (dialog is not null)
		{
			dialog.DismissEvent -= OnDialogDismissed;
			dialog.Dismiss();
		}

		base.DisconnectHandler(platformView);
	}

	void ReleaseDialog()
	{
		var dialog = _spinnerDialog;
		_spinnerDialog = null;

		if (dialog is not null)
		{
			dialog.DismissEvent -= OnDialogDismissed;
			dialog.Dismiss();
			dialog.Dispose();
		}
	}
}
