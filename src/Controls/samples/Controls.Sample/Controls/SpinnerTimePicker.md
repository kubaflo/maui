# Android spinner TimePicker

This app-level example demonstrates the spinner dialog requested in
[dotnet/maui#13568](https://github.com/dotnet/maui/issues/13568). It does not add
an `android:TimePicker.Mode` framework API.

Open **TimePicker** in the Controls sample gallery to try the 12-hour and
24-hour examples. Confirm a time with **OK**, or use **Cancel** or Android's
Back button to keep the previous value.

## Use in another MAUI app

1. Copy `SpinnerTimePicker.cs` into your app.
2. Copy `Platforms/Android/SpinnerTimePickerHandler.cs` into your app's
   `Platforms/Android` folder.
3. Copy both `spinner_time_picker.xml` files, preserving their `values` and
   `values-night` folders under `Platforms/Android/Resources`.
4. Change the C# namespaces and the XAML namespace below to match your app.
   `Resource.Style.SpinnerTimePickerDialog` must resolve to your app's generated
   Android `Resource` class.
5. Register the handler once in `MauiProgram.CreateMauiApp`, after `UseMauiApp`:

```csharp
#if ANDROID
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler<SpinnerTimePicker, SpinnerTimePickerHandler>();
});
#endif
```

Import your control and Android handler namespaces, plus
`Microsoft.Maui.Hosting`, in `MauiProgram.cs`. Guard Android-only imports with
`#if ANDROID`.

Use the custom control from XAML:

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:controls="clr-namespace:Maui.Controls.Sample.Controls">
    <controls:SpinnerTimePicker Time="17:45:00" Format="HH:mm" />
</ContentPage>
```

Use `Format="hh:mm tt"` for a 12-hour dialog, `Format="HH:mm"` for a 24-hour
dialog, or `Format="t"` to follow the Android device's 12/24-hour preference.
The selected value remains the standard MAUI `TimePicker.Time` property, so
normal two-way bindings and `TimeSelected` events continue to work.

The handler inherits the existing `TimePickerHandler` property and command
mappers. Its scoped `AppendToMapping` only releases the custom dialog when
`IsOpen` becomes false. The native dialog theme sets `android:timePickerMode`
to `spinner` without reflection or replacing click/focus listeners. The
`values-night` resource provides the corresponding dark dialog theme.
Adjust `android:colorAccent` in those themes to match your app's palette.

Only `SpinnerTimePicker` uses this Android handler; ordinary `TimePicker`
controls keep their normal dialog. On other platforms, the subclass uses the
standard MAUI handler. The example targets .NET MAUI 10 or later, including
the `IsOpen` dialog lifecycle API.
