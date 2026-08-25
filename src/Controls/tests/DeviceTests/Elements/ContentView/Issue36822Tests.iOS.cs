using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if MACCATALYST
	[Category(TestCategory.ContentView)]
	[Category("Issue36822")]
	public class Issue36822 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ImplicitStyleDoesNotApplyDuringDetachedConstruction()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler(typeof(ScrollView), typeof(ScrollViewHandler));
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
				});
			});

			var application = Application.Current;
			Assert.NotNull(application);

			var resources = application.Resources;
			var resourceKey = typeof(Issue36822BadgeView).FullName;
			var hadExistingResource = resources.TryGetValue(resourceKey, out var existingResource);
			if (hadExistingResource)
				resources.Remove(resourceKey);

			try
			{
				var healthyBadge = new Issue36822BadgeView();
				healthyBadge.Initialize();
				Assert.NotNull(healthyBadge.BadgeLabel);

				var healthyHost = new ContentView
				{
					Padding = 12,
					BackgroundColor = Colors.LightGray,
					Content = healthyBadge
				};
				var affectedHost = new ContentView
				{
					MinimumHeightRequest = 48,
					Padding = 12,
					BackgroundColor = Colors.LightGray
				};
				var triggerButton = new Button
				{
					Text = "Construct styled BadgeView"
				};
				var layout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						healthyHost,
						affectedHost,
						triggerButton
					}
				};
				var page = new ContentPage
				{
					Content = new ScrollView
					{
						Content = layout
					}
				};

				var implicitStyle = new Style(typeof(Issue36822BadgeView))
				{
					Setters =
					{
						new Setter
						{
							Property = Issue36822BadgeView.TextColorProperty,
							Value = Colors.Red
						}
					}
				};
				resources.Add(implicitStyle);

				Assert.True(resources.TryGetValue(resourceKey, out var registeredStyle));
				Assert.Same(implicitStyle, registeredStyle);

				bool clickCallbackRan = false;
				Issue36822BadgeView constructedBadge = null;
				Exception constructionException = new InvalidOperationException("The construction callback did not run.");

				triggerButton.Clicked += (_, _) =>
				{
					clickCallbackRan = true;
					try
					{
						constructedBadge = new Issue36822BadgeView();
						constructedBadge.Initialize();
						constructionException = null;
					}
					catch (NullReferenceException exception)
					{
						constructionException = exception;
					}
				};

				await CreateHandlerAndAddToWindow(page, async () =>
				{
					Assert.NotNull(triggerButton.Handler);
					var buttonHandler = Assert.IsType<ButtonHandler>(triggerButton.Handler);
					Assert.NotNull(buttonHandler.PlatformView);
					UIButton nativeButton = buttonHandler.PlatformView;

					nativeButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);

					Assert.True(clickCallbackRan, "The native button activation did not invoke the MAUI click callback.");
					Assert.True(
						constructionException is null,
						"Issue 36822: detached BadgeView construction must not invoke its property callback before initialization.");
					Assert.NotNull(constructedBadge);

					affectedHost.Content = constructedBadge;

					await AssertEventually(
						() => constructedBadge.StyleCallbackRan &&
							!constructedBadge.CallbackRanBeforeInitialization &&
							constructedBadge.BadgeLabel.TextColor == Colors.Red,
						message: "The implicit style was not applied to the initialized BadgeView after attachment.");

					Assert.Same(constructedBadge, affectedHost.Content);
				});
			}
			finally
			{
				resources.Remove(resourceKey);
				if (hadExistingResource)
					resources.Add(resourceKey, existingResource);
			}
		}

		sealed class Issue36822BadgeView : ContentView
		{
			Label _badgeLabel;
			bool _initializationCompleted;

			public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
				nameof(TextColor),
				typeof(Color),
				typeof(Issue36822BadgeView),
				Colors.Black,
				propertyChanged: OnTextColorChanged);

			public void Initialize()
			{
				_badgeLabel = new Label
				{
					Text = "BadgeView content",
					TextColor = TextColor
				};
				Content = _badgeLabel;
				_initializationCompleted = true;
			}

			public Color TextColor
			{
				get => (Color)GetValue(TextColorProperty);
				set => SetValue(TextColorProperty, value);
			}

			public Label BadgeLabel => _badgeLabel;

			public bool StyleCallbackRan { get; private set; }

			public bool CallbackRanBeforeInitialization { get; private set; }

			static void OnTextColorChanged(BindableObject bindable, object oldValue, object newValue)
			{
				var badge = (Issue36822BadgeView)bindable;
				badge.StyleCallbackRan = true;
				badge.CallbackRanBeforeInitialization = !badge._initializationCompleted;
				badge._badgeLabel.TextColor = (Color)newValue;
			}
		}
	}
#endif
}

