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
#if IOS && !MACCATALYST
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.ContentView)]
	[Category("Issue36822")]
	public class Issue36822 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ImplicitStyleIsAppliedAfterDerivedConstructorCompletes()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<BadgeView36822, ContentViewHandler>();
				});
			});

			await InvokeOnMainThreadAsync(async () =>
			{
				var healthyHost = new VerticalStackLayout();
				var healthyBadge = new BadgeView36822();
				healthyBadge.Initialize();
				healthyHost.Add(healthyBadge);
				Assert.NotNull(healthyBadge.BadgeLabel);

				var affectedHost = new VerticalStackLayout
				{
					MinimumHeightRequest = 48
				};
				var resultLabel = new Label { Text = "Ready" };
				var constructButton = new Button { Text = "Construct styled BadgeView" };
				var layout = new VerticalStackLayout
				{
					Padding = 24,
					Spacing = 16,
					Children =
					{
						new Label { Text = "Reference control constructed before style registration:" },
						healthyHost,
						new Label { Text = "Affected control constructed after style registration:" },
						affectedHost,
						constructButton,
						resultLabel
					}
				};
				var page = new ContentPage { Content = layout };

				var implicitStyle = new Style(typeof(BadgeView36822));
				implicitStyle.Setters.Add(new Setter
				{
					Property = BadgeView36822.TextColorProperty,
					Value = Colors.Red
				});

				var resources = Application.Current.Resources;
				resources.Add(implicitStyle);

				try
				{
					BadgeView36822 affectedBadge = null;
					Type constructionExceptionType = null;
					var callbackCountBeforeAttachment = int.MinValue;
					var clicked = false;
					var constructionCompleted = false;

					constructButton.Clicked += delegate
					{
						clicked = true;
						try
						{
							affectedBadge = new BadgeView36822();
							constructionCompleted = true;
							affectedBadge.Initialize();
							callbackCountBeforeAttachment = affectedBadge.TextColorCallbackCount;
							affectedHost.Add(affectedBadge);
							resultLabel.Text = "Constructed";
						}
						catch (NullReferenceException exception)
						{
							constructionExceptionType = exception.GetType();
						}
					};

					await CreateHandlerAndAddToWindow(page, async () =>
					{
						var buttonHandler = constructButton.Handler as ButtonHandler;
						Assert.NotNull(buttonHandler);
						buttonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

						Assert.True(clicked, "The native button click did not invoke the construction callback.");
						Assert.True(
							constructionCompleted,
							$"BadgeView construction invoked its TextColor callback before the derived constructor initialized the Label: Expected no exception; Actual: {constructionExceptionType?.FullName ?? "none"}");
						Assert.NotNull(affectedBadge);
						Assert.Equal(0, callbackCountBeforeAttachment);
						Assert.Equal("Constructed", resultLabel.Text);

						await AssertEventually(() => affectedBadge.TextColorCallbackCount > 0);

						Assert.True(affectedBadge.TextColorCallbackCount > 0, "The implicit style callback did not run after attachment.");
						Assert.Equal(Colors.Red, affectedBadge.BadgeLabel.TextColor);
					});
				}
				finally
				{
					resources.Remove(typeof(BadgeView36822).FullName);
				}
			});
		}
	}

	public sealed class BadgeView36822 : ContentView
	{
		Label _badgeLabel;
		int _textColorCallbackCount;

		public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
			nameof(TextColor),
			typeof(Color),
			typeof(BadgeView36822),
			Colors.Black,
			propertyChanged: static (bindable, oldValue, newValue) =>
			{
				var badge = (BadgeView36822)bindable;
				badge._textColorCallbackCount++;
				badge._badgeLabel.TextColor = (Color)newValue;
			});

		public Color TextColor
		{
			get => (Color)GetValue(TextColorProperty);
			set => SetValue(TextColorProperty, value);
		}

		public Label BadgeLabel => _badgeLabel;

		public int TextColorCallbackCount => _textColorCallbackCount;

		public void Initialize()
		{
			_badgeLabel = new Label
			{
				Text = "Badge content",
				TextColor = TextColor
			};
			Content = _badgeLabel;
		}
	}
#endif
}

