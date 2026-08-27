#if WINDOWS
using Microsoft.Maui.Controls.Shapes;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26872, "Rectangle RealParent is garbage collected after closing modal pages", PlatformAffected.WinRT)]
public class Issue26872 : ContentPage
{
	const int RequiredCycles = 2;

	readonly List<Rectangle> _retainedRectangles = [];
	readonly List<WeakReference> _popupLayoutReferences = [];
	readonly Button _openPopupButton;
	readonly Button _inspectParentsButton;
	readonly Label _cycleStatusLabel;
	readonly Label _firstAttachmentLabel;
	readonly Label _secondAttachmentLabel;
	readonly Label _firstParentLabel;
	readonly Label _secondParentLabel;
	readonly Label _inspectionStatusLabel;
	int _closedPopupCount;
	bool _transitionInProgress;

	public Issue26872()
	{
		_openPopupButton = new Button
		{
			AutomationId = "Issue26872OpenPopup",
			Text = "Open Popup"
		};
		_openPopupButton.Clicked += OnOpenPopupClicked;

		_inspectParentsButton = new Button
		{
			AutomationId = "Issue26872InspectParents",
			Text = "Inspect Rectangle Parents",
			IsEnabled = false
		};
		_inspectParentsButton.Clicked += OnInspectParentsClicked;

		_cycleStatusLabel = CreateStatusLabel("Issue26872CycleStatus", "Ready for popup cycle 1 of 2.");
		_firstAttachmentLabel = CreateStatusLabel("Issue26872Attachment1", "Not attached");
		_secondAttachmentLabel = CreateStatusLabel("Issue26872Attachment2", "Not attached");
		_firstParentLabel = CreateStatusLabel("Issue26872Parent1", "Not inspected");
		_secondParentLabel = CreateStatusLabel("Issue26872Parent2", "Not inspected");
		_inspectionStatusLabel = CreateStatusLabel("Issue26872InspectionStatus", "Not inspected");

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "Rectangle popup parent lifecycle" },
					new Label { Text = "Open and close two popup pages, then inspect the retained Rectangle controls." },
					_cycleStatusLabel,
					_openPopupButton,
					_inspectParentsButton,
					_firstAttachmentLabel,
					_secondAttachmentLabel,
					_firstParentLabel,
					_secondParentLabel,
					_inspectionStatusLabel
				}
			}
		};
	}

	static Label CreateStatusLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};

	async void OnOpenPopupClicked(object sender, EventArgs e)
	{
		if (_transitionInProgress || _closedPopupCount >= RequiredCycles)
			return;

		_transitionInProgress = true;
		int cycle = _closedPopupCount + 1;
		var rectangle = new Rectangle
		{
			AutomationId = $"Issue26872Rectangle{cycle}",
			Fill = Colors.CornflowerBlue,
			Stroke = Colors.Navy,
			StrokeThickness = 4,
			WidthRequest = 220,
			HeightRequest = 120,
			HorizontalOptions = LayoutOptions.Center
		};
		var closeButton = new Button
		{
			AutomationId = "Issue26872ClosePopup",
			Text = "Close Popup"
		};
		closeButton.Clicked += OnClosePopupClicked;

		var popupLayout = new VerticalStackLayout
		{
			Padding = 30,
			Spacing = 24,
			Children =
			{
				new Label { Text = $"Popup cycle {cycle} of {RequiredCycles}" },
				rectangle,
				closeButton
			}
		};

		_retainedRectangles.Add(rectangle);
		_popupLayoutReferences.Add(new WeakReference(popupLayout));
		GetAttachmentLabel(cycle).Text = ReferenceEquals(rectangle.Parent, popupLayout) ? "Attached" : "Not attached";

		await Navigation.PushModalAsync(new ContentPage
		{
			Title = "Rectangle Popup",
			Content = popupLayout
		}, false);
		_transitionInProgress = false;
	}

	async void OnClosePopupClicked(object sender, EventArgs e)
	{
		if (_transitionInProgress)
			return;

		_transitionInProgress = true;
		int completedCycle = _closedPopupCount + 1;
		await Navigation.PopModalAsync(false);

		_closedPopupCount = completedCycle;
		_cycleStatusLabel.Text = $"Popup cycle {_closedPopupCount} of {RequiredCycles} closed.";
		_openPopupButton.IsEnabled = _closedPopupCount < RequiredCycles;
		_inspectParentsButton.IsEnabled = _closedPopupCount == RequiredCycles;
		_transitionInProgress = false;
	}

	void OnInspectParentsClicked(object sender, EventArgs e)
	{
		if (_closedPopupCount != RequiredCycles)
			return;

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		for (int index = 0; index < _retainedRectangles.Count; index++)
		{
			var popupLayout = _popupLayoutReferences[index].Target;
			var parent = _retainedRectangles[index].Parent;
			GetParentLabel(index + 1).Text =
				parent is not null && ReferenceEquals(parent, popupLayout) ? "Available" : "Collected";
		}

		_inspectionStatusLabel.Text = "Inspection complete";
	}

	Label GetAttachmentLabel(int cycle) => cycle == 1 ? _firstAttachmentLabel : _secondAttachmentLabel;

	Label GetParentLabel(int cycle) => cycle == 1 ? _firstParentLabel : _secondParentLabel;
}
#endif

