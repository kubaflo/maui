#if ANDROID
namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33270, "PointerGestureRecognizer is never fired when the view has a PanGestureRecognizer attached", PlatformAffected.Android)]
public class Issue33270 : ContentPage
{
	public Issue33270()
	{
		var pointerEnteredCount = 0;

		var panStatusLabel = new Label
		{
			AutomationId = "PanStatus",
			Text = "Pan received: NO"
		};

		var pointerStatusLabel = new Label
		{
			AutomationId = "PointerStatus",
			Text = "Pointer entered: 0"
		};

		var pointerGestureRecognizer = new PointerGestureRecognizer();
		pointerGestureRecognizer.PointerEntered += (_, _) =>
		{
			pointerEnteredCount++;
			pointerStatusLabel.Text = $"Pointer entered: {pointerEnteredCount}";
		};

		var panGestureRecognizer = new PanGestureRecognizer();
		panGestureRecognizer.PanUpdated += (_, e) =>
		{
			if (e.StatusType is GestureStatus.Started or GestureStatus.Running or GestureStatus.Completed)
			{
				panStatusLabel.Text = "Pan received: YES";
			}
		};

		var dragTarget = new Grid
		{
			AutomationId = "DragTarget",
			HeightRequest = 220,
			BackgroundColor = Colors.LightGray,
			Children =
			{
				new Label
				{
					AutomationId = "DragInstruction",
					Text = "DRAG HERE",
					FontSize = 28,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					InputTransparent = true
				}
			}
		};

		dragTarget.GestureRecognizers.Add(pointerGestureRecognizer);
		dragTarget.GestureRecognizers.Add(panGestureRecognizer);

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Drag the target. Its pan and pointer recognizers are attached to the same view.",
					FontSize = 18
				},
				dragTarget,
				panStatusLabel,
				pointerStatusLabel
			}
		};
	}
}
#endif

