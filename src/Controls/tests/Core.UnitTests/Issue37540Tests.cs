using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class Issue37540 : BaseTestFixture
	{
		[Fact]
		public void DynamicResourceReplacesExplicitBackgroundWhenLabelLoads()
		{
			bool loadedObserved = false;
			var targetLabel = new Label
			{
				Background = new SolidColorBrush(Colors.Transparent),
				FontSize = 20,
				Padding = 16,
				Text = "Affected label"
			};
			targetLabel.Loaded += (_, _) =>
			{
				loadedObserved = true;
				targetLabel.SetDynamicResource(Label.BackgroundProperty, "backgroundColor");
			};

			var referenceBorder = new Border
			{
				Background = Colors.Red,
				HeightRequest = 48,
				Stroke = Colors.Black,
				StrokeThickness = 1,
				Content = new Label
				{
					HorizontalOptions = LayoutOptions.Center,
					Text = "Red reference",
					VerticalOptions = LayoutOptions.Center
				}
			};
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 18,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 24,
						Text = "Dynamic resource background"
					},
					new Label { Text = "Expected: Red" },
					referenceBorder,
					targetLabel,
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						Text = "NO BUG:"
					}
				}
			};
			var page = new ContentPage
			{
				Content = layout,
				Resources = new ResourceDictionary
				{
					{ "backgroundColor", Colors.Red }
				}
			};

			Assert.Same(targetLabel, layout.Children[3]);
			Assert.Same(referenceBorder, layout.Children[2]);
			Assert.Equal(48, referenceBorder.HeightRequest);
			Assert.Equal(Colors.Red, Assert.IsType<Color>(page.Resources["backgroundColor"]));
			Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(targetLabel.Background).Color);

			_ = new Window(page);

			Assert.True(loadedObserved);
			Assert.Same(targetLabel, layout.Children[3]);
			Assert.Equal("Affected label", targetLabel.Text);
			Assert.True(
				targetLabel.Background is SolidColorBrush brush && brush.Color == Colors.Red,
				"Label background should resolve to Red after Loaded.");
		}
	}
}
