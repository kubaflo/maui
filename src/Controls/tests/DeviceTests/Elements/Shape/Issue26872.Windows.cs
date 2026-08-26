using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue26872")]
	public class Issue26872 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task RectangleRetainsRealParentAfterRepeatedPopupRemoval()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ContentView, ContentViewHandler>();
					handlers.AddHandler<Rectangle, RectangleHandler>();
				});
			});

			var popupState = new PopupState();
			var page = popupState.CreatePage();

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				InvokeButton(popupState.OpenPopupButton);
				await AssertEventually(() => popupState.OpenCallbackCount == 1);

				var firstRectangle = popupState.CurrentRectangle;
				await AssertAttachedAndRendered(popupState, firstRectangle);

				CollectClosedPopup();
				AssertParentOwnership(firstRectangle, "while popup cycle 1 was attached");

				var firstPopupRoot = popupState.PopupRoot;
				var firstPopupUnloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				void OnFirstPopupUnloaded(object sender, EventArgs args) => firstPopupUnloaded.TrySetResult();
				firstPopupRoot.Unloaded += OnFirstPopupUnloaded;
				InvokeButton(popupState.CurrentCloseButton);
				await AssertEventually(() => popupState.CloseCallbackCycle == 1);
				await firstPopupUnloaded.Task;
				firstPopupRoot.Unloaded -= OnFirstPopupUnloaded;
				firstPopupRoot = null;
				Assert.Null(popupState.PopupHost.Content);
				Assert.False(popupState.PopupHost.IsVisible);

				InvokeButton(popupState.OpenPopupButton);
				await AssertEventually(() => popupState.OpenCallbackCount == 2);

				var secondRectangle = popupState.CurrentRectangle;
				Assert.NotSame(firstRectangle, secondRectangle);
				await AssertAttachedAndRendered(popupState, secondRectangle);

				InvokeButton(popupState.CurrentCloseButton);
				await AssertEventually(() => popupState.CloseCallbackCycle == 2);
				Assert.Null(popupState.PopupHost.Content);
				Assert.False(popupState.PopupHost.IsVisible);

				Assert.NotNull(firstRectangle.RealParent);
				var retainedParent = Assert.IsType<VerticalStackLayout>(firstRectangle.RealParent);
				Assert.True(retainedParent.Children.Any(child => ReferenceEquals(child, firstRectangle)));
			});
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static async Task AssertAttachedAndRendered(PopupState popupState, Rectangle rectangle)
		{
			Assert.Same(rectangle, popupState.CurrentRectangle);
			Assert.Same(popupState.PopupRoot, popupState.PopupHost.Content);
			Assert.Same(popupState.PopupStack, rectangle.RealParent);
			Assert.True(popupState.PopupStack.Children.Any(child => ReferenceEquals(child, rectangle)));

			Assert.NotNull(rectangle.Handler);
			var nativeRectangle = rectangle.Handler.PlatformView as WFrameworkElement;
			Assert.NotNull(nativeRectangle);
			Assert.NotNull(popupState.PopupStack.Handler);
			Assert.Same(popupState.PopupStack.Handler.PlatformView, nativeRectangle.Parent);

			await AssertEventually(() =>
				Math.Abs(nativeRectangle.ActualWidth - 220) <= 1 &&
				Math.Abs(nativeRectangle.ActualHeight - 120) <= 1);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void AssertParentOwnership(Rectangle rectangle, string lifecycleState)
		{
			var parent = rectangle.RealParent;
			Assert.True(
				parent is VerticalStackLayout parentLayout &&
					parentLayout.Children.Any(child => ReferenceEquals(child, rectangle)),
				$"Rectangle RealParent ownership was lost {lifecycleState}.");
		}

		static void InvokeButton(Button button)
		{
			Assert.NotNull(button.Handler);
			var nativeButton = button.Handler.PlatformView as WButton;
			Assert.NotNull(nativeButton);

			var automationPeer = new ButtonAutomationPeer(nativeButton);
			var invokeProvider = automationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			Assert.NotNull(invokeProvider);
			invokeProvider.Invoke();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void CollectClosedPopup()
		{
			for (var collection = 0; collection < 3; collection++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}
		}

		sealed class PopupState
		{
			Grid _popupRoot;
			VerticalStackLayout _popupStack;
			Rectangle _currentRectangle;
			Rectangle _retainedRectangle;
			Button _currentCloseButton;

			public Button OpenPopupButton { get; } = new Button
			{
				AutomationId = "OpenPopupButton",
				Text = "Open popup"
			};

			public Label CycleStatusLabel { get; } = new Label
			{
				AutomationId = "CycleStatus",
				Text = "Completed popup cycles: 0"
			};

			public ContentView PopupHost { get; } = new ContentView
			{
				AutomationId = "PopupHost",
				IsVisible = false
			};

			public Grid PopupRoot => _popupRoot;

			public VerticalStackLayout PopupStack => _popupStack;

			public Rectangle CurrentRectangle => _currentRectangle;

			public Button CurrentCloseButton => _currentCloseButton;

			public int OpenCallbackCount { get; private set; }

			public int CloseCallbackCycle { get; private set; } = -1;

			public ContentPage CreatePage()
			{
				OpenPopupButton.Clicked += OnOpenPopupClicked;

				var rootLayout = new Grid
				{
					Padding = 24,
					Children =
					{
						new VerticalStackLayout
						{
							Spacing = 16,
							Children =
							{
								new Label
								{
									FontAttributes = FontAttributes.Bold,
									FontSize = 22,
									Text = "Rectangle popup lifecycle"
								},
								new Label
								{
									Text = "Open and close the popup twice. The blue Rectangle remains visible until each close."
								},
								OpenPopupButton,
								CycleStatusLabel
							}
						},
						PopupHost
					}
				};

				return new ContentPage
				{
					Title = "Rectangle popup lifecycle",
					Content = rootLayout
				};
			}

			void OnOpenPopupClicked(object sender, EventArgs e)
			{
				if (_popupRoot is not null)
					return;

				_currentRectangle = new Rectangle
				{
					AutomationId = "PopupRectangle",
					Fill = Colors.CornflowerBlue,
					HeightRequest = 120,
					WidthRequest = 220
				};
				_currentCloseButton = new Button
				{
					AutomationId = "ClosePopupButton",
					Text = "Close popup"
				};
				_currentCloseButton.Clicked += OnClosePopupClicked;

				_popupStack = new VerticalStackLayout
				{
					Spacing = 16,
					Children =
					{
						new Label
						{
							HorizontalOptions = LayoutOptions.Center,
							Text = "Popup with Rectangle"
						},
						_currentRectangle,
						_currentCloseButton
					}
				};
				_popupRoot = new Grid
				{
					AutomationId = "PopupPanel",
					BackgroundColor = Color.FromArgb("#CCFFFFFF"),
					Padding = 32,
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.Center,
					Children = { _popupStack }
				};

				PopupHost.Content = _popupRoot;
				PopupHost.IsVisible = true;
				OpenPopupButton.IsEnabled = false;
				OpenCallbackCount++;
			}

			void OnClosePopupClicked(object sender, EventArgs e)
			{
				var closingRectangle = _currentRectangle;

				PopupHost.Content = null;
				PopupHost.IsVisible = false;
				OpenPopupButton.IsEnabled = true;
				_popupRoot = null;
				_popupStack = null;
				_currentRectangle = null;
				_currentCloseButton = null;
				CloseCallbackCycle = OpenCallbackCount;
				CycleStatusLabel.Text = $"Completed popup cycles: {CloseCallbackCycle}";

				if (_retainedRectangle is not null)
					CollectClosedPopup();

				_retainedRectangle = closingRectangle;
			}
		}
	}
}

