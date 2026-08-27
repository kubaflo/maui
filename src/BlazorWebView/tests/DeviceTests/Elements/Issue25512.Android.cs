#if ANDROID
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Issues;

[Category("Issue25512")]
public class Issue25512
{
	[Fact]
	public void RazorComponentWithNoneBuildActionIsNotCompiled()
	{
		var excludedComponent = typeof(Issue25512).Module.GetType(
			typeof(Issue25512Home).FullName,
			throwOnError: false,
			ignoreCase: false);

		Assert.True(
			excludedComponent is null,
			$"Excluded Home.razor rendered after launch: {excludedComponent}");
	}
}

public sealed class Issue25512Home : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "main");
		builder.AddAttribute(1, "id", "issue25512-home");
		builder.OpenElement(2, "h1");
		builder.AddContent(3, "Home");
		builder.CloseElement();
		builder.OpenElement(4, "p");
		builder.AddContent(5, "Hello, world!");
		builder.CloseElement();
		builder.OpenElement(6, "p");
		builder.AddContent(7, "Welcome to your new app.");
		builder.CloseElement();
		builder.CloseElement();
	}
}
#endif

