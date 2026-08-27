#if ANDROID
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Microsoft.Maui.DeviceTests.Memory;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category("Issue36216")]
public class Issue36216 : ControlsHandlerTestBase
{
	const int CycleCount = 3;

	[Fact]
	public async Task StopOnlyAccelerometerSubscribersAreCollectibleAfterNavigation()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandlerStub>();
				handlers.AddHandler<NavigationPage, NavigationViewHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Toolbar, ToolbarHandler>();
				handlers.AddHandler<ScrollView, ScrollViewHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var rootLayout = new VerticalStackLayout
		{
			Padding = 24,
			Children =
			{
				new Label { Text = "Accelerometer subscriber retention" },
				new Label { Text = "Running navigation cycles" }
			}
		};
		var scrollView = new ScrollView { Content = rootLayout };
		var rootPage = new ContentPage { Content = scrollView };
		var navigationPage = new NavigationPage(rootPage);
		var window = new Window(navigationPage);
		var controlReferences = new List<WeakReference>();
		var stopOnlyReferences = new List<WeakReference>();
		var unsubscribedReferences = new List<WeakReference>();
		var lifecycleStates = new List<LifecycleState>();
		Func<byte[]> createPayload = static () =>
		{
			var payload = new byte[3 * 1024 * 1024];
			payload[0] = 1;
			payload[^1] = 1;
			return payload;
		};

		try
		{
			await CreateHandlerAndAddToWindow<WindowHandlerStub>(window, async _ =>
			{
				Assert.NotNull(window.Handler);
				Assert.NotNull(window.Handler.PlatformView);
				Assert.NotNull(navigationPage.Handler);
				Assert.NotNull(navigationPage.Handler.PlatformView);
				Assert.NotNull(rootPage.Handler);
				Assert.NotNull(rootPage.Handler.PlatformView);
				Assert.NotNull(scrollView.Handler);
				Assert.NotNull(scrollView.Handler.PlatformView);
				Assert.NotNull(rootLayout.Handler);
				Assert.NotNull(rootLayout.Handler.PlatformView);

				await RunScenario(navigationPage, SubscriptionMode.Control, controlReferences, lifecycleStates, createPayload);
				await RunScenario(navigationPage, SubscriptionMode.StopOnly, stopOnlyReferences, lifecycleStates, createPayload);
				await RunScenario(navigationPage, SubscriptionMode.Unsubscribe, unsubscribedReferences, lifecycleStates, createPayload);
			});

			Assert.Equal(CycleCount * 3, lifecycleStates.Count);
			Assert.All(lifecycleStates, state =>
			{
				Assert.Equal(0, state.AppearingCount);
				Assert.Equal(0, state.DisappearingCount);
				Assert.True(state.PopCompleted);
			});

			await AssertionExtensions.WaitForGC(controlReferences.ToArray());
			await AssertionExtensions.WaitForGC(unsubscribedReferences.ToArray());
			await AssertionExtensions.WaitForGC(stopOnlyReferences.ToArray());
		}
		finally
		{
			foreach (var reference in stopOnlyReferences)
			{
				if (reference.Target is SensorSubscriberPage page)
					page.RemoveSubscription();
			}
		}
	}

	static async Task RunScenario(
		NavigationPage navigationPage,
		SubscriptionMode mode,
		List<WeakReference> references,
		List<LifecycleState> lifecycleStates,
		Func<byte[]> createPayload)
	{
		for (var i = 0; i < CycleCount; i++)
		{
			var lifecycleState = new LifecycleState
			{
				AppearingCount = -1,
				DisappearingCount = -1
			};
			lifecycleStates.Add(lifecycleState);
			references.Add(await NavigateOnce(navigationPage, mode, lifecycleState, createPayload));
			Assert.Single(navigationPage.Navigation.NavigationStack);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static async Task<WeakReference> NavigateOnce(
		NavigationPage navigationPage,
		SubscriptionMode mode,
		LifecycleState lifecycleState,
		Func<byte[]> createPayload)
	{
		SensorSubscriberPage page = new();
		page.Initialize(mode, lifecycleState, createPayload());
		var reference = new WeakReference(page);

		await navigationPage.PushAsync(page, false);
		page.AssertHandlersCreated();
		await navigationPage.PopAsync(false);
		lifecycleState.PopCompleted = true;
		page = null;

		return reference;
	}

	enum SubscriptionMode
	{
		Control,
		StopOnly,
		Unsubscribe
	}

	sealed class LifecycleState
	{
		public int AppearingCount { get; set; }

		public int DisappearingCount { get; set; }

		public bool PopCompleted { get; set; }
	}

	sealed class SensorSubscriberPage : ContentPage
	{
		SubscriptionMode _mode;
		LifecycleState _lifecycleState;
		byte[] _payload;
		VerticalStackLayout _contentLayout;

		public void Initialize(SubscriptionMode mode, LifecycleState lifecycleState, byte[] payload)
		{
			_mode = mode;
			_lifecycleState = lifecycleState;
			_payload = payload;
			_contentLayout = new VerticalStackLayout
			{
				Padding = 24,
				Children =
				{
					new Label { Text = "Accelerometer subscriber retention" },
					new Label { Text = "Running navigation cycle" }
				}
			};
			Content = _contentLayout;
		}

		public void AssertHandlersCreated()
		{
			Assert.NotNull(Handler);
			Assert.NotNull(Handler.PlatformView);
			Assert.NotNull(_contentLayout.Handler);
			Assert.NotNull(_contentLayout.Handler.PlatformView);
			Assert.All(_contentLayout.Children, child =>
			{
				Assert.NotNull(child.Handler);
				Assert.NotNull(child.Handler.PlatformView);
			});
		}

		public void RemoveSubscription() =>
			Accelerometer.ReadingChanged -= OnReadingChanged;

		protected override void OnAppearing()
		{
			base.OnAppearing();
			_lifecycleState.AppearingCount++;

			if (_mode != SubscriptionMode.Control)
				Accelerometer.ReadingChanged += OnReadingChanged;
		}

		protected override void OnDisappearing()
		{
			if (_mode != SubscriptionMode.Control)
			{
				Accelerometer.Stop();

				if (_mode == SubscriptionMode.Unsubscribe)
					RemoveSubscription();
			}

			_lifecycleState.DisappearingCount++;
			base.OnDisappearing();
		}

		void OnReadingChanged(object sender, AccelerometerChangedEventArgs e) =>
			_ = _payload[0];
	}
}
#endif

