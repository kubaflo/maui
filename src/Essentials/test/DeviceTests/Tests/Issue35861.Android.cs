using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Xunit;
using MauiPlatform = Microsoft.Maui.ApplicationModel.Platform;

namespace Microsoft.Maui.Essentials.DeviceTests;

[Category("Issue35861")]
public class Issue35861
{
	const int FailedRequestCount = 999;

	[Fact]
	public async Task FailedOffMainThreadRequestsDoNotPoisonValidRequest()
	{
		Assert.NotNull(MauiPlatform.CurrentActivity);

		var initialStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
		Assert.Equal(PermissionStatus.Denied, initialStatus);

		var observedFailureCount = -1;
		var caughtFailureCount = 0;
		var callbackRan = false;
		var callbackOnMainThread = true;

		var source = ImageSource.FromStream(() =>
		{
			callbackRan = true;
			callbackOnMainThread = MainThread.IsMainThread;

			for (var i = 0; i < FailedRequestCount; i++)
			{
				try
				{
					Permissions.RequestAsync<Permissions.LocationWhenInUse>().GetAwaiter().GetResult();
				}
				catch (PermissionException)
				{
					caughtFailureCount++;
				}
			}

			observedFailureCount = caughtFailureCount;
			return null;
		});

		using var stream = await ((IStreamImageSource)source).GetStreamAsync();

		Assert.True(callbackRan);
		Assert.False(callbackOnMainThread);
		Assert.Equal(FailedRequestCount, observedFailureCount);

		var invokedOnMainThread = false;
		var validRequest = Task.FromCanceled<PermissionStatus>(new CancellationToken(true));

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			invokedOnMainThread = MainThread.IsMainThread;
			validRequest = Permissions.RequestAsync<Permissions.LocationWhenInUse>();
		});

		Assert.True(invokedOnMainThread);

		string requestState;
		if (validRequest.IsFaulted)
			requestState = $"faulted with {validRequest.Exception.GetBaseException()}";
		else if (validRequest.IsCanceled)
			requestState = "canceled";
		else if (!validRequest.IsCompleted)
			requestState = "pending for the Android permission prompt";
		else
			requestState = $"returned {validRequest.Result}";

		Assert.True(
			!validRequest.IsFaulted && !validRequest.IsCanceled,
			$"Valid main-thread location request was poisoned after 999 off-main-thread failures; request {requestState}.");
	}
}

