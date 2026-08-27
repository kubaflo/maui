#if IOS && !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Xunit;

namespace Microsoft.Maui.DeviceTests;

[Category("Issue33556")]
public class Issue33556 : HybridWebViewTestsBase
{
	[Fact]
	public Task RoutedDocumentCanInvokeDotNet() =>
		RunTest("invokedotnettests.html", async view =>
		{
			var invocationTarget = new InvocationTarget();
			view.SetInvokeJavaScriptTarget(invocationTarget);

			var initialPath = await view.EvaluateJavaScriptAsync("window.location.pathname");
			Assert.Equal("/", initialPath);

			await view.EvaluateJavaScriptAsync("history.pushState({}, '', '/home')");

			var routedPath = await view.EvaluateJavaScriptAsync("window.location.pathname");
			Assert.Equal("/home", routedPath);

			var observedState = await view.EvaluateJavaScriptAsync("window.issue33556State = 'not-started'");
			Assert.Equal("not-started", observedState);

			var triggerState = await view.EvaluateJavaScriptAsync(
				"window.issue33556State = 'pending'; window['Hybrid' + 'WebView'].InvokeDotNet('MarkInvoked').then(() => window.issue33556State = 'completed').catch(error => window.issue33556State = `rejected: ${error}`); 'started'");
			Assert.Equal("started", triggerState);

			System.Func<Task<bool>> invocationCompleted = async () =>
			{
				observedState = await view.EvaluateJavaScriptAsync("window.issue33556State");
				return observedState == "completed" ||
					(observedState != null && observedState.StartsWith("rejected: ", System.StringComparison.Ordinal));
			};

			await invocationCompleted.AssertEventuallyAsync(
				timeout: 5000,
				message: "The routed document invocation did not complete.");

			Assert.True(
				invocationTarget.WasCalled,
				$"Routed document invocation should reach .NET; observed state: {observedState}");
		});

	sealed class InvocationTarget
	{
		public bool WasCalled { get; private set; }

		public void MarkInvoked()
		{
			WasCalled = true;
		}
	}
}
#endif

