using System;
using System.Linq;
using System.Threading.Tasks;
using AndroidX.Core.View.Accessibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Accessibility)]
	[Category("Issue33612")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue33612 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task SemanticHintLabelsGestureClickAction()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
				});
			});

			const string expectedDescription = "First accessible item";
			const string expectedHint = "Activates the first item";
			var firstItem = new
			{
				Name = "Item 1",
				Description = expectedDescription,
				Hint = expectedHint
			};
			var items = new[]
			{
				firstItem,
				new
				{
					Name = "Item 2",
					Description = "Second accessible item",
					Hint = "Activates the second item"
				}
			};

			var itemsLayout = new StackLayout();
			SemanticProperties.SetDescription(itemsLayout, "Accessible collection");
			BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
			{
				var label = new Label
				{
					FontSize = 18
				};
				label.SetBinding(Label.TextProperty, "Name");

				var border = new Border
				{
					Padding = 16,
					Margin = new Thickness(0, 4),
					Content = label
				};
				border.SetBinding(SemanticProperties.DescriptionProperty, "Description");
				border.SetBinding(SemanticProperties.HintProperty, "Hint");
				border.GestureRecognizers.Add(new TapGestureRecognizer());
				return border;
			}));
			BindableLayout.SetItemsSource(itemsLayout, items);

			var contentLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children = { itemsLayout }
			};
			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = contentLayout
				}
			};

			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async _ =>
			{
				Assert.Equal(2, itemsLayout.Children.Count);
				var firstBorder = Assert.IsType<Border>(itemsLayout.Children[0]);
				Assert.Same(firstItem, firstBorder.BindingContext);
				Assert.Equal("Item 1", Assert.IsType<Label>(firstBorder.Content).Text);
				Assert.Null(firstBorder.Style);
				Assert.Equal(new Thickness(16), firstBorder.Padding);
				Assert.Equal(new Thickness(0, 4), firstBorder.Margin);
				Assert.Equal(expectedDescription, SemanticProperties.GetDescription(firstBorder));
				Assert.Equal(expectedHint, SemanticProperties.GetHint(firstBorder));

				await OnFrameSetToNotEmpty(firstBorder);
				Assert.NotNull(firstBorder.Handler);
				Assert.NotNull(firstBorder.Handler.PlatformView);
				var platformView = Assert.IsAssignableFrom<AView>(firstBorder.Handler.PlatformView);
				Assert.True(platformView.IsAttachedToWindow, "The first generated Border must be attached to the Android window.");
				Assert.NotNull(platformView.WindowToken);

				using var node = platformView.CreateAccessibilityNodeInfo();
				Assert.NotNull(node);
				Assert.Equal(expectedDescription, node.ContentDescription?.ToString());
				Assert.Equal(expectedHint, node.HintText?.ToString());

				var actions = node.ActionList;
				Assert.NotNull(actions);
				var clickActions = actions
					.Where(action => action.Id == AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionClick.Id)
					.ToList();
				Assert.Single(clickActions);

				const string unobserved = "<unobserved>";
				string observedActionLabel = unobserved;
				observedActionLabel = clickActions[0].Label?.ToString();
				Assert.NotEqual(unobserved, observedActionLabel);
				Assert.True(
					string.Equals(expectedHint, observedActionLabel, StringComparison.Ordinal),
					$"Issue33612 accessibility action label mismatch: expected '{expectedHint}', observed '{observedActionLabel ?? "<null>"}', ACTION_CLICK count {clickActions.Count}.");
			});
		}

	}
}

