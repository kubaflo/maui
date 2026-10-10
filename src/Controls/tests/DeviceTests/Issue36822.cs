#if WINDOWS
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue36822")]
	public class Issue36822 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task DetachedControlDoesNotApplyApplicationImplicitStyleDuringConstruction()
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var resources = Application.Current.Resources;
				var styleKey = typeof(ConstructorBadge).FullName;

				Assert.False(resources.ContainsKey(styleKey));

				var cleanBadge = new ConstructorBadge();
				cleanBadge.BadgeLabel = new Label
				{
					Text = "Constructed badge"
				};
				cleanBadge.BadgeLabel.TextColor = cleanBadge.TextColor;
				cleanBadge.Content = cleanBadge.BadgeLabel;
				Assert.Same(cleanBadge.BadgeLabel, cleanBadge.Content);

				var implicitStyle = new Style(typeof(ConstructorBadge));
				implicitStyle.Setters.Add(new Setter
				{
					Property = ConstructorBadge.TextColorProperty,
					Value = Colors.Red
				});
				resources.Add(implicitStyle);

				try
				{
					Assert.True(resources.TryGetValue(styleKey, out var registeredResource));
					var registeredStyle = Assert.IsType<Style>(registeredResource);
					var registeredSetter = Assert.Single(registeredStyle.Setters);
					Assert.Same(ConstructorBadge.TextColorProperty, registeredSetter.Property);
					Assert.Equal(Colors.Red, registeredSetter.Value);

					var constructionAttempted = false;
					var constructionCompleted = false;
					Exception constructionException = new InvalidOperationException("Construction was not attempted.");

					try
					{
						constructionAttempted = true;
						var styledBadge = new ConstructorBadge();
						styledBadge.BadgeLabel = new Label
						{
							Text = "Constructed badge"
						};
						styledBadge.BadgeLabel.TextColor = styledBadge.TextColor;
						styledBadge.Content = styledBadge.BadgeLabel;
						constructionCompleted = true;
						constructionException = null;
					}
					catch (NullReferenceException exception)
					{
						constructionException = exception;
					}

					Assert.True(constructionAttempted);
					var exceptionName = constructionException is null ? "none" : constructionException.GetType().Name;
					Assert.True(
						constructionCompleted && constructionException is null,
						$"Detached ConstructorBadge construction completed={constructionCompleted}; exception={exceptionName}; expected completed=True with no exception");
				}
				finally
				{
					resources.Remove(styleKey);
				}
			});
		}

		sealed class ConstructorBadge : ContentView
		{
			public Label BadgeLabel;

			public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
				nameof(TextColor),
				typeof(Color),
				typeof(ConstructorBadge),
				Colors.Black,
				propertyChanged: (bindable, oldValue, newValue) =>
					((ConstructorBadge)bindable).BadgeLabel.TextColor = (Color)newValue);

			public Color TextColor
			{
				get => (Color)GetValue(TextColorProperty);
				set => SetValue(TextColorProperty, value);
			}
		}
	}
}
#endif

