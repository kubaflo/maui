#if ANDROID
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
	using MauiPlatform = Microsoft.Maui.ApplicationModel.Platform;

	[Category("Screenshot")]
	[Category("Issue37638")]
	public class Issue37638
	{
		[Fact]
		public async Task CaptureAsyncCompletesSynchronouslyOnMainThread()
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(26))
				return;

			Assert.True(Screenshot.IsCaptureSupported);

			bool callbackInvoked = false;
			bool callbackRanOnMainThread = false;
			TaskStatus captureStatus = (TaskStatus)(-1);
			Task<IScreenshotResult> captureTask = null;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				callbackInvoked = true;
				callbackRanOnMainThread = MainThread.IsMainThread;

				var activity = MauiPlatform.CurrentActivity;
				Assert.NotNull(activity);

				var window = activity.Window;
				Assert.NotNull(window);

				var rootView = window.DecorView.RootView;
				Assert.NotNull(rootView);
				Assert.True(rootView.IsAttachedToWindow);
				Assert.True(rootView.Width > 0);
				Assert.True(rootView.Height > 0);

				captureTask = Screenshot.Default.CaptureAsync();
				captureStatus = captureTask.Status;
			});

			Assert.True(callbackInvoked);
			Assert.True(callbackRanOnMainThread);
			Assert.NotNull(captureTask);

			await captureTask.ConfigureAwait(false);

			Assert.True(
				captureStatus == TaskStatus.RanToCompletion,
				$"Screenshot capture did not complete synchronously on the Android UI thread. Expected: {TaskStatus.RanToCompletion}; Actual: {captureStatus}.");
		}
	}
}
#endif

