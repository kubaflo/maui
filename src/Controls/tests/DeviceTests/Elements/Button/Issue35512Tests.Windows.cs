using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue35512")]
	public class Issue35512 : ControlsHandlerTestBase
	{
#if WINDOWS
		[Fact]
		public async Task NullBackgroundColorRestoresImplicitStyleBrush()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var styleColor = Color.FromArgb("#512BD4");
			var implicitButtonStyle = new Style(typeof(Button))
			{
				Setters =
				{
					new Setter { Property = Button.BackgroundColorProperty, Value = styleColor },
					new Setter { Property = Button.TextColorProperty, Value = Colors.White },
				}
			};

			var affectedButton = new Button { Text = "Affected default button" };
			var referenceButton = new Button { Text = "Reference default button" };
			var affectedColumn = new VerticalStackLayout
			{
				Spacing = 6,
				Children =
				{
					new Label { Text = "Affected" },
					affectedButton,
				}
			};
			var referenceColumn = new VerticalStackLayout
			{
				Spacing = 6,
				Children =
				{
					new Label { Text = "Unchanged reference" },
					referenceButton,
				}
			};
			var comparisonGrid = new Grid
			{
				ColumnSpacing = 12,
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star),
				},
				Children =
				{
					affectedColumn,
					referenceColumn,
				}
			};
			Grid.SetColumn(referenceColumn, 1);

			var page = new ContentPage
			{
				Resources = new ResourceDictionary { implicitButtonStyle },
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 30,
						Spacing = 14,
						Children =
						{
							new Label
							{
								Text = "Button BackgroundColor null reset",
								FontSize = 24,
								FontAttributes = FontAttributes.Bold,
							},
							new Label
							{
								Text = "Both sample buttons begin with the same implicit violet Button style. The affected button must return to violet after the reset.",
							},
							comparisonGrid,
						}
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var affectedHandler = Assert.IsType<ButtonHandler>(affectedButton.Handler);
				var referenceHandler = Assert.IsType<ButtonHandler>(referenceButton.Handler);
				var affectedPlatformButton = Assert.IsAssignableFrom<WButton>(affectedHandler.PlatformView);
				var referencePlatformButton = Assert.IsAssignableFrom<WButton>(referenceHandler.PlatformView);
				var initialAffectedBrush = Assert.IsType<WSolidColorBrush>(affectedPlatformButton.Background);
				var initialReferenceBrush = Assert.IsType<WSolidColorBrush>(referencePlatformButton.Background);
				var expectedStyleColor = styleColor.ToWindowsColor();

				Assert.Equal(expectedStyleColor, initialReferenceBrush.Color);
				Assert.Equal(initialReferenceBrush.Color, initialAffectedBrush.Color);
				var initialAffectedColor = initialAffectedBrush.Color;

				var expectingReset = false;
				var managedTransitionObserved = false;
				var managedResetObserved = false;
				PropertyChangedEventHandler managedPropertyChanged = (_, args) =>
				{
					if (args.PropertyName != nameof(Button.BackgroundColor))
						return;

					if (expectingReset)
						managedResetObserved = affectedButton.BackgroundColor is null;
					else if (affectedButton.BackgroundColor is Color backgroundColor)
						managedTransitionObserved = backgroundColor.Equals(Colors.Red);
				};
				affectedButton.PropertyChanged += managedPropertyChanged;

				var nativeTransition = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				var nativeCallbackCount = 0;
				var nativeCallbackToken = affectedPlatformButton.RegisterPropertyChangedCallback(
					WButton.BackgroundProperty,
					(_, _) =>
					{
						nativeCallbackCount++;
						nativeTransition.TrySetResult(true);
					});

				affectedButton.BackgroundColor = Colors.Red;
				await nativeTransition.Task.WaitAsync(TimeSpan.FromSeconds(5));
				Assert.True(managedTransitionObserved);
				Assert.True(nativeCallbackCount > 0);
				var redBrush = Assert.IsType<WSolidColorBrush>(affectedPlatformButton.Background);
				Assert.NotEqual(initialAffectedColor, redBrush.Color);
				Assert.Equal(Colors.Red.ToWindowsColor(), redBrush.Color);

				var callbacksBeforeReset = nativeCallbackCount;
				nativeTransition = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				expectingReset = true;
				managedResetObserved = false;

				affectedButton.BackgroundColor = null;
				await nativeTransition.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.True(managedResetObserved);
				Assert.True(nativeCallbackCount > callbacksBeforeReset);
				Assert.Null(affectedButton.BackgroundColor);
				var resetBrush = Assert.IsType<WSolidColorBrush>(affectedPlatformButton.Background);
				var referenceBrushAfterReset = Assert.IsType<WSolidColorBrush>(referencePlatformButton.Background);

				affectedButton.PropertyChanged -= managedPropertyChanged;
				affectedPlatformButton.UnregisterPropertyChangedCallback(WButton.BackgroundProperty, nativeCallbackToken);

				Assert.Equal(expectedStyleColor, referenceBrushAfterReset.Color);
				Assert.True(
					resetBrush.Color.Equals(initialAffectedColor),
					$"Button BackgroundColor null reset should restore the initial implicit-style brush. Expected {initialAffectedColor} but observed {resetBrush.Color}.");
			});
		}
#endif
	}
}

