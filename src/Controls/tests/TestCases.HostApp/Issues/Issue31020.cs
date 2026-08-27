#if ANDROID
using System.Globalization;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 31020, "Shell OnSizeAllocated is not called after device rotation", PlatformAffected.Android)]
public class Issue31020 : Shell
{
	int _sizeAllocatedCount;

	public Issue31020()
	{
		Items.Add(new ShellContent
		{
			Title = "Home",
			Route = "Issue31020MainPage",
			ContentTemplate = new DataTemplate(() => new Issue31020MainPage(this))
		});
	}

	internal int SizeAllocatedCount => _sizeAllocatedCount;

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		_sizeAllocatedCount++;
	}
}

public class Issue31020MainPage : ContentPage
{
	readonly Issue31020 _issueShell;
	readonly Label _shellCountLabel;
	readonly Label _pageCountLabel;
	readonly Label _geometryLabel;
	readonly Label _resultLabel;
	int _allocationCount;
	int _armedPageAllocationCount;
	int _armedShellAllocationCount;
	double _armedWidth;
	double _armedHeight;
	double _lastWidth;
	double _lastHeight;
	double _reportedWidth;
	double _reportedHeight;
	bool _isArmed;

	public Issue31020MainPage(Issue31020 issueShell)
	{
		_issueShell = issueShell;
		Title = "OnSizeAllocated";

		_shellCountLabel = new Label
		{
			AutomationId = "ShellCountLabel",
			Text = "Shell callbacks after rotation: pending"
		};
		_pageCountLabel = new Label
		{
			AutomationId = "PageCountLabel",
			Text = "MainPage callbacks after rotation: pending"
		};
		_geometryLabel = new Label
		{
			AutomationId = "GeometryLabel",
			Text = "Page size: pending"
		};
		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			FontAttributes = FontAttributes.Bold,
			Text = "Not checked"
		};

		var armButton = new Button
		{
			AutomationId = "ArmRotationButton",
			Text = "Arm rotation check"
		};
		armButton.Clicked += OnArmClicked;

		var checkButton = new Button
		{
			AutomationId = "CheckRotationButton",
			Text = "Check rotation callbacks"
		};
		checkButton.Clicked += OnCheckClicked;

		Content = new ScrollView
		{
			AutomationId = "Issue31020PageRoot",
			Content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 18,
						Text = "Shell rotation allocation check"
					},
					_shellCountLabel,
					_pageCountLabel,
					_geometryLabel,
					armButton,
					checkButton,
					_resultLabel
				}
			}
		};
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		_allocationCount++;
		_lastWidth = width;
		_lastHeight = height;

		if (_isArmed && (width != _reportedWidth || height != _reportedHeight))
		{
			_reportedWidth = width;
			_reportedHeight = height;
			_pageCountLabel.Text = $"MainPage callbacks after rotation: {_allocationCount - _armedPageAllocationCount}";
			_geometryLabel.Text = string.Create(
				CultureInfo.InvariantCulture,
				$"Last page allocation: {width:F0}x{height:F0}");
		}
	}

	void OnArmClicked(object sender, EventArgs e)
	{
		_armedPageAllocationCount = _allocationCount;
		_armedShellAllocationCount = _issueShell.SizeAllocatedCount;
		_armedWidth = _lastWidth;
		_armedHeight = _lastHeight;
		_reportedWidth = _lastWidth;
		_reportedHeight = _lastHeight;
		_isArmed = true;
		_shellCountLabel.Text = "Shell callbacks after rotation: 0";
		_pageCountLabel.Text = "MainPage callbacks after rotation: 0";
		_geometryLabel.Text = $"Armed page size: {_armedWidth:F0}x{_armedHeight:F0}";
		_resultLabel.Text = "Armed";
	}

	void OnCheckClicked(object sender, EventArgs e)
	{
		if (!_isArmed)
			return;

		int pageCallbacks = _allocationCount - _armedPageAllocationCount;
		int shellCallbacks = _issueShell.SizeAllocatedCount - _armedShellAllocationCount;
		bool changedToLandscape = _armedWidth < _armedHeight && _lastWidth > _lastHeight;

		_shellCountLabel.Text = $"Shell callbacks after rotation: {shellCallbacks}";
		_pageCountLabel.Text = $"MainPage callbacks after rotation: {pageCallbacks}";
		_geometryLabel.Text = $"Page changed to landscape: {changedToLandscape}; {_armedWidth:F0}x{_armedHeight:F0} -> {_lastWidth:F0}x{_lastHeight:F0}";
		_resultLabel.Text = changedToLandscape && pageCallbacks > 0 && shellCallbacks > 0
			? "Both controls received callbacks"
			: "Shell callback missing";
	}
}
#endif

