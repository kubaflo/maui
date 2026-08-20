using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests;

#if MACCATALYST
[Category("Issue30532")]
[Category("TimePicker")]
[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
public class Issue30532 : ControlsHandlerTestBase
{
	const double RequestedCharacterSpacing = 10;

	[Fact]
	public async Task CharacterSpacingAppliesToRenderedTimeText()
	{
		EnsureHandlerCreated(builder =>
		{
			builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
				handlers.AddHandler<Page, PageHandler>();
				handlers.AddHandler<Layout, LayoutHandler>();
				handlers.AddHandler<Label, LabelHandler>();
				handlers.AddHandler<Button, ButtonHandler>();
				handlers.AddHandler<TimePicker, TimePickerHandler>();
			});
		});

		var headingLabel = new Label
		{
			Text = "TimePicker with CharacterSpacing 10:"
		};
		var affectedTimePicker = new TimePicker
		{
			CharacterSpacing = RequestedCharacterSpacing,
			HorizontalOptions = LayoutOptions.Start,
			Time = new TimeSpan(11, 0, 0)
		};
		var checkSpacingButton = new Button
		{
			HorizontalOptions = LayoutOptions.Start,
			Text = "Check character spacing"
		};
		var diagnosticsLabel = new Label
		{
			Text = "Waiting for the settled TimePicker measurement."
		};
		var statusLabel = new Label
		{
			FontAttributes = FontAttributes.Bold,
			Text = "NO BUG:"
		};
		var stackLayout = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 20
		};
		stackLayout.Add(headingLabel);
		stackLayout.Add(affectedTimePicker);
		stackLayout.Add(checkSpacingButton);
		stackLayout.Add(diagnosticsLabel);
		stackLayout.Add(statusLabel);

		var page = new ContentPage
		{
			Content = stackLayout
		};

		await CreateHandlerAndAddToWindow<IWindowHandler>(new Microsoft.Maui.Controls.Window(page), async _ =>
		{
			await OnLoadedAsync(affectedTimePicker);

			var platformView = Assert.IsAssignableFrom<UIView>(affectedTimePicker.Handler.PlatformView);
			Assert.NotNull(platformView.Window);
			Assert.True(platformView.Frame.Width > 0, "The attached TimePicker must have a nonzero native frame.");
			Assert.Equal(new TimeSpan(11, 0, 0), affectedTimePicker.Time);

			affectedTimePicker.CharacterSpacing = 0;
			var renderedText = Array.Empty<(string Text, double CharacterSpacing)>();
			await AssertEventually(
				() =>
				{
					renderedText = GetRenderedTimeText(affectedTimePicker);
					return renderedText.Length > 0;
				},
				message: "The native TimePicker did not expose its rendered time text.");
			Assert.All(renderedText, text => Assert.Equal(0, text.CharacterSpacing));

			var propertyChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == TimePicker.CharacterSpacingProperty.PropertyName)
					propertyChanged.TrySetResult();
			}

			affectedTimePicker.PropertyChanged += OnPropertyChanged;
			double observedCharacterSpacing = double.NaN;
			try
			{
				affectedTimePicker.CharacterSpacing = RequestedCharacterSpacing;
				await propertyChanged.Task;
				await AssertEventually(
					() =>
					{
						renderedText = GetRenderedTimeText(affectedTimePicker);
						if (renderedText.Length == 0)
							return false;

						observedCharacterSpacing = renderedText[0].CharacterSpacing;
						return renderedText.All(text =>
							Math.Abs(text.CharacterSpacing - RequestedCharacterSpacing) < 0.01);
					},
					message: "TimePicker rendered text did not apply CharacterSpacing 10.");
			}
			finally
			{
				affectedTimePicker.PropertyChanged -= OnPropertyChanged;
			}

			Assert.True(propertyChanged.Task.IsCompletedSuccessfully, "The CharacterSpacing property-change callback did not occur.");
			Assert.False(double.IsNaN(observedCharacterSpacing), "The post-trigger character-spacing sentinel was not replaced.");
			Assert.NotNull(Assert.IsAssignableFrom<UIView>(affectedTimePicker.Handler.PlatformView).Window);
			Assert.Equal(RequestedCharacterSpacing, observedCharacterSpacing, 2);
		});
	}

	static (string Text, double CharacterSpacing)[] GetRenderedTimeText(TimePicker timePicker)
	{
		if (timePicker.Handler?.PlatformView is not UIView platformView)
			return Array.Empty<(string, double)>();

		return GetRenderedText(platformView)
			.Where(text => text.Text.Length > 1)
			.ToArray();
	}

	static IEnumerable<(string Text, double CharacterSpacing)> GetRenderedText(UIView view)
	{
		var text = view switch
		{
			UILabel label => label.AttributedText?.Value ?? label.Text ?? string.Empty,
			UITextField textField => textField.AttributedText?.Value ?? textField.Text ?? string.Empty,
			_ => string.Empty
		};
		var characterSpacing = view switch
		{
			UILabel label => label.AttributedText.GetCharacterSpacing(),
			UITextField textField => textField.AttributedText.GetCharacterSpacing(),
			_ => 0
		};

		if (view.Window is not null && !view.Hidden && view.Alpha > 0 && text.Length > 0)
			yield return (text, characterSpacing);

		foreach (var subview in view.Subviews)
		{
			foreach (var childText in GetRenderedText(subview))
				yield return childText;
		}
	}
}
#endif
