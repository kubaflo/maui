#if WINDOWS
using Microsoft.Maui.Controls.Shapes;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26872, "Rectangle RealParent is garbage collected after closing popups", PlatformAffected.WinRT)]
public class Issue26872 : ContentPage
{
	const int RequiredCycles = 2;
	readonly Grid _rootGrid;
	readonly Label _cycleStatusLabel;
	readonly Label _initialParentStatusLabel;
	readonly Label _popupStatusLabel;
	readonly Label _resultStatusLabel;
	readonly Label _checkStatusLabel;
	readonly Button _checkParentButton;
	readonly List<Rectangle> _retainedRectangles = [];
	readonly List<string> _initialParentIds = [];
	int _completedCycles = -1;

	public Issue26872()
	{
		Title = "Rectangle popup parent";

		_resultStatusLabel = new Label
		{
			AutomationId = "ResultStatus",
			FontSize = 18,
			Text = "Unchecked"
		};

		_cycleStatusLabel = new Label
		{
			AutomationId = "CycleStatus",
			Text = "Completed 0 of 2"
		};

		_initialParentStatusLabel = new Label
		{
			AutomationId = "InitialParentStatus",
			Text = "No popup closed"
		};

		_popupStatusLabel = new Label
		{
			AutomationId = "PopupStatus",
			Text = "No popup open"
		};

		_checkStatusLabel = new Label
		{
			AutomationId = "CheckStatus",
			Text = "Unchecked"
		};

		var openPopupButton = new Button
		{
			AutomationId = "OpenPopupButton",
			Text = "Open popup"
		};
		openPopupButton.Clicked += OnOpenPopupClicked;

		_checkParentButton = new Button
		{
			AutomationId = "CheckParentButton",
			IsEnabled = false,
			Text = "Check Rectangle parent"
		};
		_checkParentButton.Clicked += OnCheckParentClicked;

		var controls = new VerticalStackLayout
		{
			Spacing = 16,
			Children =
			{
				new Label
				{
					FontSize = 20,
					Text = "Open and close the Rectangle popup twice."
				},
				_resultStatusLabel,
				_cycleStatusLabel,
				_initialParentStatusLabel,
				_popupStatusLabel,
				_checkStatusLabel,
				openPopupButton,
				_checkParentButton
			}
		};

		_rootGrid = new Grid
		{
			Padding = 24,
			Children = { controls }
		};
		Content = _rootGrid;
	}

	void OnOpenPopupClicked(object sender, EventArgs e)
	{
		int cycle = _completedCycles + 2;
		string popupId = $"PopupSurface{cycle}";

		var rectangle = new Rectangle
		{
			AutomationId = $"PopupRectangle{cycle}",
			Fill = Colors.Blue,
			HeightRequest = 140,
			WidthRequest = 220,
			HorizontalOptions = LayoutOptions.Center
		};

		var closeButton = new Button
		{
			AutomationId = "ClosePopupButton",
			Text = "Close popup",
			HorizontalOptions = LayoutOptions.Center
		};

		var popup = new Grid
		{
			AutomationId = popupId,
			BackgroundColor = Colors.White,
			Padding = 24,
			RowSpacing = 16,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			}
		};
		popup.Add(rectangle);
		popup.Add(closeButton, 0, 1);

		_popupStatusLabel.Text = $"Popup {cycle} pending";
		rectangle.SizeChanged += OnRectangleSizeChanged;
		closeButton.Clicked += OnClosePopupClicked;
		_rootGrid.Add(popup);

		void OnRectangleSizeChanged(object sizeSender, EventArgs sizeEventArgs)
		{
			if (Math.Abs(rectangle.Width - rectangle.WidthRequest) >= 0.5 ||
				Math.Abs(rectangle.Height - rectangle.HeightRequest) >= 0.5 ||
				rectangle.RealParent?.AutomationId != popupId)
			{
				return;
			}

			rectangle.SizeChanged -= OnRectangleSizeChanged;
			_popupStatusLabel.Text = $"Popup {cycle} ready";
		}

		void OnClosePopupClicked(object closeSender, EventArgs closeEventArgs)
		{
			closeButton.Clicked -= OnClosePopupClicked;

			string initialParentId = rectangle.RealParent?.AutomationId ?? "<null>";
			_initialParentIds.Add(initialParentId);
			_retainedRectangles.Add(rectangle);
			_rootGrid.Remove(popup);

			_completedCycles++;
			_cycleStatusLabel.Text = $"Completed {_completedCycles + 1} of {RequiredCycles}";
			_initialParentStatusLabel.Text = string.Join(",", _initialParentIds);
			_checkParentButton.IsEnabled = _completedCycles + 1 >= RequiredCycles;
		}
	}

	void OnCheckParentClicked(object sender, EventArgs e)
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		_resultStatusLabel.Text = string.Join(",", _retainedRectangles.Select(
			rectangle => rectangle.RealParent?.AutomationId ?? "<null>"));
		_checkStatusLabel.Text = "Checked";
	}
}
#endif

