#if WINDOWS
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue25407")]
	public class Issue25407 : ControlsHandlerTestBase
	{
		[Fact]
		[RequiresUnreferencedCode("XAML parsing may require unreferenced code")]
		public async Task BindableObjectMemberReceivesInheritedBindingContext()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<Issue25407StyledLabelView, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			var viewModel = new Issue25407ViewModel
			{
				LabelBackgroundColor = Colors.Violet
			};
			var affectedPage = new ContentPage();
			affectedPage.LoadFromXaml(
				"""
				<ContentPage
					xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
					xmlns:local="clr-namespace:Microsoft.Maui.DeviceTests;assembly=Microsoft.Maui.Controls.DeviceTests">
					<ScrollView>
						<VerticalStackLayout Padding="24" Spacing="16">
							<local:Issue25407StyledLabelView HeightRequest="100">
								<local:Issue25407StyledLabelView.StyledLabel>
									<Label
										HorizontalTextAlignment="Center"
										VerticalTextAlignment="Center"
										Text="Affected label" />
								</local:Issue25407StyledLabelView.StyledLabel>
								<local:Issue25407StyledLabelView.LabelStyle>
									<local:Issue25407LabelStyle BackgroundColor="{Binding LabelBackgroundColor}" />
								</local:Issue25407StyledLabelView.LabelStyle>
							</local:Issue25407StyledLabelView>
						</VerticalStackLayout>
					</ScrollView>
				</ContentPage>
				""");

			var affectedScrollView = Assert.IsType<ScrollView>(affectedPage.Content);
			var affectedLayout = Assert.IsType<VerticalStackLayout>(affectedScrollView.Content);
			var affectedControl = Assert.IsType<Issue25407StyledLabelView>(Assert.Single(affectedLayout.Children));
			var expected = viewModel.LabelBackgroundColor;

			var cleanControl = new Issue25407StyledLabelView
			{
				HeightRequest = 100,
				StyledLabel = new Label
				{
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					Text = "Affected label"
				},
				LabelStyle = new Issue25407LabelStyle
				{
					BackgroundColor = viewModel.LabelBackgroundColor
				}
			};
			var cleanPage = new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children = { cleanControl }
					}
				}
			};

			Color cleanManaged = Colors.Transparent;
			Color cleanNative = Colors.Transparent;
			await AttachAndRun(cleanPage, _ =>
			{
				cleanManaged = cleanControl.LabelStyle.BackgroundColor;
				cleanNative = GetNativeBackground(cleanControl.StyledLabel);
				Assert.Equal(expected, cleanManaged);
				Assert.Equal(expected, cleanNative);
			});

			var bindingContextChanged = false;
			var nativeLoaded = false;
			var bindingContextChangedSource = new TaskCompletionSource();
			var nativeLoadedSource = new TaskCompletionSource();

			affectedPage.BindingContextChanged += (_, _) =>
			{
				bindingContextChanged = true;
				bindingContextChangedSource.TrySetResult();
			};
			affectedControl.StyledLabel.HandlerChanged += (_, _) =>
			{
				var labelHandler = Assert.IsType<LabelHandler>(affectedControl.StyledLabel.Handler);
				labelHandler.PlatformView.Loaded += (_, _) =>
				{
					nativeLoaded = true;
					nativeLoadedSource.TrySetResult();
				};
			};

			affectedPage.BindingContext = viewModel;
			await bindingContextChangedSource.Task.WaitAsync(TimeSpan.FromSeconds(2));

			await AttachAndRun(affectedPage, async _ =>
			{
				await nativeLoadedSource.Task.WaitAsync(TimeSpan.FromSeconds(2));

				Assert.True(bindingContextChanged);
				Assert.True(nativeLoaded);
				Assert.True(affectedControl.LabelStyle.IsSet(Issue25407LabelStyle.BackgroundColorProperty));
				Assert.Same(viewModel, affectedPage.BindingContext);
				Assert.Same(viewModel, affectedControl.BindingContext);
				var affectedLabelHandler = Assert.IsType<LabelHandler>(affectedControl.StyledLabel.Handler);
				Assert.NotNull(affectedLabelHandler.PlatformView);

				Color managed = Colors.Transparent;
				Color native = Colors.Transparent;
				var resolved = await Wait(() =>
				{
					managed = affectedControl.LabelStyle.BackgroundColor;
					native = GetNativeBackground(affectedControl.StyledLabel);
					return managed.Equals(expected) && native.Equals(expected);
				});

				Assert.True(
					resolved,
					$"Issue 25407 nested LabelStyle background mismatch: managed={managed}, native={native}, cleanManaged={cleanManaged}, cleanNative={cleanNative}, expected={expected}");
			});
		}

		static Color GetNativeBackground(Label label)
		{
			var handler = Assert.IsType<LabelHandler>(label.Handler);
			var container = Assert.IsType<WrapperView>(handler.ContainerView);
			var brush = Assert.IsType<WSolidColorBrush>(container.Background);
			return brush.Color.ToColor();
		}
	}

	public sealed class Issue25407ViewModel
	{
		public Color LabelBackgroundColor { get; set; }
	}

	public sealed class Issue25407LabelStyle : BindableObject
	{
		public static readonly BindableProperty BackgroundColorProperty =
			BindableProperty.Create(
				nameof(BackgroundColor),
				typeof(Color),
				typeof(Issue25407LabelStyle),
				Colors.Red);

		public Color BackgroundColor
		{
			get => (Color)GetValue(BackgroundColorProperty);
			set => SetValue(BackgroundColorProperty, value);
		}
	}

	public sealed class Issue25407StyledLabelView : ContentView
	{
		public static readonly BindableProperty LabelStyleProperty =
			BindableProperty.Create(
				nameof(LabelStyle),
				typeof(Issue25407LabelStyle),
				typeof(Issue25407StyledLabelView),
				default(Issue25407LabelStyle),
				propertyChanged: OnLabelStyleChanged);

		public Issue25407LabelStyle LabelStyle
		{
			get => (Issue25407LabelStyle)GetValue(LabelStyleProperty);
			set => SetValue(LabelStyleProperty, value);
		}

		public Label StyledLabel { get; set; }

		static void OnLabelStyleChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var control = (Issue25407StyledLabelView)bindable;
			var labelStyle = (Issue25407LabelStyle)newValue;
			control.Content = control.StyledLabel;
			control.StyledLabel.SetBinding(
				Label.BackgroundColorProperty,
				new Binding(nameof(Issue25407LabelStyle.BackgroundColor), source: labelStyle));
		}
	}
}
#endif

