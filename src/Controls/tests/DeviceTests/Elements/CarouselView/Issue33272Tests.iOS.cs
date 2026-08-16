using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.CarouselView)]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue33272 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ItemContentRemainsWithinCarouselViewBounds()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<CarouselView, CarouselViewHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			Grid itemRoot = null;
			Label itemLabel = null;

			var carouselView = new CarouselView
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				ItemsSource = new[] { "Item1" },
				ItemTemplate = new DataTemplate(() =>
				{
					itemLabel = new Label
					{
						FontSize = 24,
						HorizontalOptions = LayoutOptions.Center,
						VerticalOptions = LayoutOptions.Start
					};
					itemLabel.SetBinding(Label.TextProperty, ".");

					itemRoot = new Grid
					{
						HorizontalOptions = LayoutOptions.Fill,
						VerticalOptions = LayoutOptions.Fill
					};
					itemRoot.Add(itemLabel);
					return itemRoot;
				})
			};

			var page = new ContentPage
			{
				Content = new Grid
				{
					carouselView
				}
			};

			await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
			{
				await AssertEventually(() =>
					itemRoot?.Handler?.PlatformView is UIView { Window: not null } itemView &&
					itemLabel?.Handler?.PlatformView is UIView { Window: not null } labelView &&
					carouselView.Handler?.PlatformView is UIView { Window: not null } carouselPlatformView &&
					itemView.Bounds.Width > 0 &&
					itemView.Bounds.Height > 0 &&
					labelView.Bounds.Width > 0 &&
					labelView.Bounds.Height > 0 &&
					carouselPlatformView.Bounds.Width > 0 &&
					carouselPlatformView.Bounds.Height > 0);

				var itemTop = carouselView.Y + itemRoot.Y + itemLabel.Y;

				Assert.True(itemTop > 0.5, "CarouselView item content should remain below the usable top edge.");
			});
		}
	}
}
