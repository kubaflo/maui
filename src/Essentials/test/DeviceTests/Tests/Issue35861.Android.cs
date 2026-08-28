using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.DeviceTests;
using Xunit;

namespace Microsoft.Maui.Essentials.DeviceTests
{
	using MauiPlatform = Microsoft.Maui.ApplicationModel.Platform;

	[Category("Issue35861")]
	public class Issue35861
	{
		[Fact]
		public async Task InvalidOffMainThreadRequestsDoNotPoisonValidRequest()
		{
			global::Android.App.Activity activity = null;
			global::Android.Views.Window activityWindow = null;
			var initiallyFocused = false;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				Assert.True(MainThread.IsMainThread);

				activity = MauiPlatform.CurrentActivity;
				Assert.NotNull(activity);

				activityWindow = activity.Window;
				Assert.NotNull(activityWindow);

				initiallyFocused = activityWindow.DecorView.HasWindowFocus;
			});

			Assert.True(initiallyFocused, "The Essentials device-test activity must have window focus before requesting permission.");

			var initialStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
			Assert.Equal(PermissionStatus.Denied, initialStatus);

			var observedFailureCount = -1;
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				var workerThreadReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				Assert.True(activityWindow.DecorView.Post(() => workerThreadReady.SetResult(true)));
				await workerThreadReady.Task.ConfigureAwait(false);

				Assert.False(MainThread.IsMainThread);

				var requests = new Task<PermissionStatus>[999];
				for (var i = 0; i < requests.Length; i++)
					requests[i] = Permissions.RequestAsync<Permissions.LocationWhenInUse>();

				var permissionFailures = 0;
				foreach (var request in requests)
				{
					try
					{
						await request.ConfigureAwait(false);
					}
					catch (PermissionException)
					{
						permissionFailures++;
					}
				}

				observedFailureCount = permissionFailures;
			});

			Assert.Equal(999, observedFailureCount);

			Task<PermissionStatus> finalRequest = null;
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				Assert.True(MainThread.IsMainThread);
				finalRequest = Permissions.RequestAsync<Permissions.LocationWhenInUse>();
			});

			Assert.NotNull(finalRequest);

			var requestCompletedWait = AssertHelpers.Wait(
				() => finalRequest.IsCompleted,
				timeout: 3000,
				interval: 50);
			var focusLostWait = AssertHelpers.Wait(
				() => !MainThread.InvokeOnMainThreadAsync(() => activityWindow.DecorView.HasWindowFocus).GetAwaiter().GetResult(),
				timeout: 3000,
				interval: 50);

			await Task.WhenAll(requestCompletedWait, focusLostWait);

			var requestCompleted = requestCompletedWait.Result;
			var focusLost = focusLostWait.Result;
			var finalOutcome = "<not observed>";
			Exception finalException = null;

			if (finalRequest.Status == TaskStatus.RanToCompletion)
				finalOutcome = $"PermissionStatus.{finalRequest.Result}";
			else if (focusLost)
				finalOutcome = "Android permission controller displayed";
			else if (finalRequest.IsFaulted)
			{
				finalException = finalRequest.Exception.GetBaseException();
				finalOutcome = $"{finalException.GetType().FullName}: {finalException.Message}";
			}
			else if (finalRequest.IsCanceled)
				finalOutcome = "request canceled";

			var validRequestReachedAndroid =
				finalRequest.Status == TaskStatus.RanToCompletion ||
				focusLost;

			Assert.True(
				requestCompleted || focusLost,
				$"The valid permission request produced no observable transition. Status={finalRequest.Status}; BackgroundFailures={observedFailureCount}; HasWindowFocus={!focusLost}.");
			Assert.True(
				validRequestReachedAndroid,
				$"Valid main-thread LocationWhenInUse request collided after 999 off-main-thread failures: Outcome={finalOutcome}; TaskStatus={finalRequest.Status}; BackgroundFailures={observedFailureCount}; HasWindowFocus={!focusLost}.");
		}
	}
}

