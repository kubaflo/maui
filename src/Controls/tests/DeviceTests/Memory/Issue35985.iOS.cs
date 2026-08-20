using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

#if !MACCATALYST
[Category(TestCategory.Memory)]
[Category("Issue35985")]
public class Issue35985 : ControlsHandlerTestBase
{
	[Fact]
	public async Task DetachedDefaultStepperAndHandlerAreCollected()
	{
		if (!OperatingSystem.IsIOSVersionAtLeast(26))
			return;

		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<IScrollView, ScrollViewHandler>();
				handlers.AddHandler<IContentView, ContentViewHandler>();
				handlers.AddHandler<Stepper, StepperHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var scenario = CreateScenario();

		await CreateHandlerAndAddToWindow(scenario.RootPage, async () =>
		{
			await OnLoadedAsync(scenario.AffectedStepper);

			var unloadedObservation = new UnloadedObservation();
			var unloadedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var references = ReleaseStepper(scenario, unloadedObservation, unloadedSource);

			await unloadedSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
			Assert.Equal(1, unloadedObservation.Count);

			await AssertionExtensions.WaitForGC(
				references.StepperReference,
				references.HandlerReference,
				references.PlatformViewReference);
		});
	}

	static StepperScenario CreateScenario()
	{
		var stepper = new Stepper();
		var scenarioGrid = new Grid
		{
			Children = { stepper }
		};
		var scenarioHost = new ContentView
		{
			HeightRequest = 80,
			Content = scenarioGrid
		};
		var stack = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label { FontAttributes = FontAttributes.Bold, FontSize = 20, Text = "iOS default Stepper handler lifetime" },
				new Label { Text = "Preparing default Stepper handler..." },
				scenarioHost,
				new Button { Text = "Release Stepper hierarchy" },
				new Button { Text = "Check weak references" },
				new Label { Text = "Lifecycle pending" },
				new Label { Text = "Alive references: not checked" },
				new Label { FontAttributes = FontAttributes.Bold, Text = "NO BUG:" }
			}
		};
		var rootPage = new ContentPage
		{
			Content = new ScrollView { Content = stack }
		};

		return new StepperScenario
		{
			RootPage = rootPage,
			ScenarioHost = scenarioHost,
			ScenarioGrid = scenarioGrid,
			AffectedStepper = stepper
		};
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static LeakReferences ReleaseStepper(
		StepperScenario scenario,
		UnloadedObservation unloadedObservation,
		TaskCompletionSource unloadedSource)
	{
		var stepper = scenario.AffectedStepper;

		Assert.Same(stepper, Assert.Single(scenario.ScenarioGrid.Children));
		Assert.Equal(0, stepper.Value);
		Assert.Equal(0, stepper.Minimum);
		Assert.Equal(100, stepper.Maximum);
		Assert.Equal(1, stepper.Increment);

		var handler = Assert.IsType<StepperHandler>(stepper.Handler);
		var platformView = Assert.IsType<UIStepper>(handler.PlatformView);
		Assert.NotNull(platformView.Window);

		var references = new LeakReferences
		{
			StepperReference = new WeakReference(stepper),
			HandlerReference = new WeakReference(handler),
			PlatformViewReference = new WeakReference(platformView)
		};

		stepper.Unloaded += (_, _) =>
		{
			unloadedObservation.Count = 1;
			unloadedSource.TrySetResult();
		};

		scenario.ScenarioHost.Content = null;
		scenario.ScenarioGrid = null;
		scenario.AffectedStepper = null;

		return references;
	}

	sealed class StepperScenario
	{
		public ContentPage RootPage { get; set; }
		public ContentView ScenarioHost { get; set; }
		public Grid ScenarioGrid { get; set; }
		public Stepper AffectedStepper { get; set; }
	}

	sealed class LeakReferences
	{
		public WeakReference StepperReference { get; set; }
		public WeakReference HandlerReference { get; set; }
		public WeakReference PlatformViewReference { get; set; }
	}

	sealed class UnloadedObservation
	{
		public int Count { get; set; } = -1;
	}
}
#endif

