#if IOS && !MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
[Category("Issue26526")]
[Category(TestCategory.CollectionView)]
public class Issue26526 : ControlsHandlerTestBase
{
	[Fact]
	public async Task DefaultLabelTextRemainsReadableAfterChangingToDarkTheme()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Window, WindowHandler>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Grid, LayoutHandler>();
				handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
				handlers.AddHandler<CollectionView, CollectionViewHandler2>();
				handlers.AddHandler<Border, BorderHandler>();
				handlers.AddHandler<Image, ImageHandler>();
				handlers.AddHandler<Label, LabelHandler>();
			});
		});

		var items = new List<string>
		{
			"If you're visiting this page, you're likely here because you're searching for a random sentence.",
			"Sometimes a random word just isn't enough, and that is where the random sentence generator comes into play. By inputting the desired number, you can make a list of as many random sentences as you want or need. Producing random sentences can be helpful in a number of different ways.",
			"For writers, a random sentence can help them get their creative juices flowing. Since the topic of the sentence is completely unknown, it forces the writer to be creative when the sentence appears.",
			"For those writers who have writers' block, this can be an excellent way to take a step to crumbling those walls."
		};

		var application = Application.Current;
		Assert.NotNull(application);
		var originalTheme = application.UserAppTheme;

		var (page, collectionView) = await InvokeOnMainThreadAsync(() =>
		{
			application.UserAppTheme = AppTheme.Light;
			Assert.Equal(AppTheme.Light, application.RequestedTheme);

			var itemCollection = new CollectionView
			{
				ItemsSource = items,
				ItemTemplate = new DataTemplate(() =>
				{
					var itemText = new Label { Margin = new Thickness(0, 0, 0, 10) };
					itemText.SetBinding(Label.TextProperty, ".");

					var headingGrid = new Grid
					{
						Margin = new Thickness(0, 0, 0, 10)
					};
					headingGrid.Add(new Label
					{
						Text = "Username",
						FontFamily = "Baskerville",
						HorizontalOptions = LayoutOptions.Start
					});
					headingGrid.Add(new Label
					{
						Text = "Today",
						FontFamily = "Baskerville",
						VerticalOptions = LayoutOptions.Center,
						HorizontalOptions = LayoutOptions.End
					});

					var textColumn = new VerticalStackLayout();
					textColumn.Add(headingGrid);
					textColumn.Add(itemText);

					var imageColumn = new VerticalStackLayout
					{
						VerticalOptions = LayoutOptions.Start,
						Spacing = 5
					};
					imageColumn.Add(new Image
					{
						Source = "dotnet_bot.png",
						WidthRequest = 40,
						HeightRequest = 40,
						VerticalOptions = LayoutOptions.Start,
						HorizontalOptions = LayoutOptions.Center
					});

					var itemGrid = new Grid
					{
						ColumnDefinitions =
						{
							new ColumnDefinition(new GridLength(40)),
							new ColumnDefinition(GridLength.Star)
						},
						ColumnSpacing = 10
					};
					itemGrid.Add(imageColumn);
					itemGrid.Add(textColumn, 1);

					return new VerticalStackLayout
					{
						Padding = 20,
						Children =
						{
							new Border
							{
								BackgroundColor = Colors.White,
								StrokeShape = new RoundRectangle { CornerRadius = 15 },
								Padding = 10,
								Content = itemGrid
							}
						}
					};
				})
			};

			var root = new Grid { Margin = 20 };
			root.Add(itemCollection);

			return (
				new ContentPage
				{
					Title = "Item Height",
					Content = root
				},
				itemCollection);
		});

		try
		{
			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await AssertEventually(
					() => collectionView.GetVisualTreeDescendants()
						.OfType<Label>()
						.Any(label => label.Text == items[0] && label.Handler is LabelHandler),
					timeout: 5000,
					message: "The first CollectionView item was not realized.");

				var firstItemLabel = collectionView.GetVisualTreeDescendants()
					.OfType<Label>()
					.First(label => label.Text == items[0]);
				var firstItemBorder = FindContainingBorder(firstItemLabel);
				var labelHandler = Assert.IsType<LabelHandler>(firstItemLabel.Handler);
				var nativeLabel = Assert.IsAssignableFrom<UILabel>(labelHandler.PlatformView);
				var nativeBorder = Assert.IsAssignableFrom<UIView>(firstItemBorder.Handler.PlatformView);

				Assert.Same(items[0], firstItemLabel.BindingContext);
				Assert.Null(firstItemLabel.GetValue(Label.TextColorProperty));
				Assert.Equal(Colors.White, firstItemBorder.BackgroundColor);
				Assert.NotNull(nativeLabel.Window);
				Assert.NotNull(nativeBorder.Window);
				Assert.True(nativeLabel.Frame.Width > 0 && nativeLabel.Frame.Height > 0);
				Assert.True(nativeBorder.Frame.Width > 0 && nativeBorder.Frame.Height > 0);

				Assert.Equal(UIUserInterfaceStyle.Light, nativeLabel.TraitCollection.UserInterfaceStyle);
				var initialForeground = GetResolvedRgba(nativeLabel, UIUserInterfaceStyle.Light);
				Assert.True(
					ContrastAgainstWhite(initialForeground) >= 4.5,
					$"The initial label foreground {Format(initialForeground)} was not readable against white.");

				var transitionObserved = false;
				void OnRequestedThemeChanged(object sender, AppThemeChangedEventArgs args)
				{
					if (args.RequestedTheme == AppTheme.Dark)
						transitionObserved = true;
				}

				application.RequestedThemeChanged += OnRequestedThemeChanged;
				try
				{
					application.UserAppTheme = AppTheme.Dark;
					await AssertEventually(
						() => transitionObserved && application.RequestedTheme == AppTheme.Dark,
						message: "The application did not complete the Light-to-Dark theme transition.");

					var realizedLabel = collectionView.GetVisualTreeDescendants()
						.OfType<Label>()
						.First(label => label.Text == items[0]);
					var realizedHandler = Assert.IsType<LabelHandler>(realizedLabel.Handler);
					var postTransitionLabel = Assert.IsAssignableFrom<UILabel>(realizedHandler.PlatformView);

					Assert.Same(firstItemLabel, realizedLabel);
					Assert.Same(nativeLabel, postTransitionLabel);
					Assert.Null(realizedLabel.GetValue(Label.TextColorProperty));
					Assert.Equal(Colors.White, firstItemBorder.BackgroundColor);
					Assert.NotNull(postTransitionLabel.Window);

					var postTransitionForeground = GetResolvedRgba(postTransitionLabel, UIUserInterfaceStyle.Dark);
					var contrast = ContrastAgainstWhite(postTransitionForeground);
					Assert.True(
						contrast >= 4.5,
						"Issue26526 default CollectionView label became unreadable after the Light-to-Dark theme transition. " +
						$"Expected contrast of at least 4.500 against white, observed {contrast:F3} with {Format(postTransitionForeground)}.");
				}
				finally
				{
					application.RequestedThemeChanged -= OnRequestedThemeChanged;
				}
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => application.UserAppTheme = originalTheme);
		}
	}

	static Border FindContainingBorder(Element element)
	{
		var parent = element.Parent;
		while (parent is not null)
		{
			if (parent is Border border)
				return border;

			parent = parent.Parent;
		}

		throw new Xunit.Sdk.XunitException("The realized item label was not inside the expected Border.");
	}

	static (double Red, double Green, double Blue, double Alpha) GetResolvedRgba(
		UILabel label,
		UIUserInterfaceStyle userInterfaceStyle)
	{
		Assert.NotNull(label.TextColor);
		var traits = UITraitCollection.FromUserInterfaceStyle(userInterfaceStyle);
		var color = label.TextColor.GetResolvedColor(traits);
		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return ((double)red, (double)green, (double)blue, (double)alpha);
	}

	static double ContrastAgainstWhite((double Red, double Green, double Blue, double Alpha) color)
	{
		var luminance =
			0.2126 * ToLinear(color.Red) +
			0.7152 * ToLinear(color.Green) +
			0.0722 * ToLinear(color.Blue);
		return 1.05 / (luminance + 0.05);
	}

	static double ToLinear(double component) =>
		component <= 0.04045
			? component / 12.92
			: Math.Pow((component + 0.055) / 1.055, 2.4);

	static string Format((double Red, double Green, double Blue, double Alpha) color) =>
		$"RGBA({color.Red:F3}, {color.Green:F3}, {color.Blue:F3}, {color.Alpha:F3})";
}
#endif

