#if ANDROID
using System;
using System.Threading.Tasks;
using Android.Views.Accessibility;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.TestUtils.DeviceTests.Runners.HeadlessRunner;
using Xunit;
using Xunit.Sdk;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.Essentials.DeviceTests;

[Category("Issue35861")]
public class Issue35861
{
	const int OffMainRequestCount = 999;
	const string MainThreadExceptionMessage = "Permission request must be invoked on main thread.";

	[Fact]
	public async Task FailedOffMainRequestsDoNotPoisonValidRequest()
	{
		Assert.Equal(
			PermissionStatus.Denied,
			await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>());
		Assert.False(IsPermissionDialogVisible());

		var completedOffMainRequests = -1;
		var expectedFailures = 0;

		await Screenshot.Default.CaptureAsync().ConfigureAwait(false);
		Assert.False(MainThread.IsMainThread);

		for (var i = 0; i < OffMainRequestCount; i++)
		{
			var exception = await Assert.ThrowsAsync<PermissionException>(
				() => Permissions.RequestAsync<Permissions.LocationWhenInUse>());

			Assert.Equal(MainThreadExceptionMessage, exception.Message);
			expectedFailures++;
		}

		completedOffMainRequests = expectedFailures;
		Assert.Equal(OffMainRequestCount, completedOffMainRequests);

		var finalStatus = (PermissionStatus)(-1);
		var validRequest = MainThread.InvokeOnMainThreadAsync(
			Permissions.RequestAsync<Permissions.LocationWhenInUse>);

		await AssertEventually(
			() => validRequest.IsCompleted || IsPermissionDialogVisible(),
			timeout: 10000,
			message: "The valid location request neither completed nor displayed the permission dialog.");

		if (validRequest.IsFaulted)
		{
			var aggregateException = validRequest.Exception;
			Assert.NotNull(aggregateException);
			var requestException = aggregateException.GetBaseException();

			if (requestException is ArgumentException argumentException)
			{
				throw new XunitException(
					$"Valid main-thread location request collided after {completedOffMainRequests} expected off-main-thread failures: {argumentException.Message}");
			}

			await validRequest;
		}

		Assert.False(validRequest.IsCompleted);
		Assert.True(IsPermissionDialogVisible());
		Assert.True(DenyPermissionDialog(), "The Android permission dialog did not expose a deny action.");

		await AssertEventually(
			() => validRequest.IsCompleted,
			timeout: 10000,
			message: "The location request did not complete after the permission dialog was denied.");

		finalStatus = await validRequest;
		Assert.Equal(PermissionStatus.Denied, finalStatus);
	}

	static bool IsPermissionDialogVisible() => FindPermissionDenyButton(performClick: false);

	static bool DenyPermissionDialog() => FindPermissionDenyButton(performClick: true);

	static bool FindPermissionDenyButton(bool performClick)
	{
		using var root = MauiTestInstrumentation.Current.UiAutomation.RootInActiveWindow;
		return root != null && FindPermissionDenyButton(root, performClick);
	}

	static bool FindPermissionDenyButton(AccessibilityNodeInfo node, bool performClick)
	{
		var resourceId = node.ViewIdResourceName;
		if (!string.IsNullOrEmpty(resourceId)
			&& (resourceId.EndsWith(":id/permission_deny_button", StringComparison.Ordinal)
				|| resourceId.EndsWith(":id/permission_deny_and_dont_ask_again_button", StringComparison.Ordinal)))
		{
			return !performClick || node.PerformAction(global::Android.Views.Accessibility.Action.Click);
		}

		for (var i = 0; i < node.ChildCount; i++)
		{
			using var child = node.GetChild(i);
			if (child != null && FindPermissionDenyButton(child, performClick))
				return true;
		}

		return false;
	}
}
#endif

