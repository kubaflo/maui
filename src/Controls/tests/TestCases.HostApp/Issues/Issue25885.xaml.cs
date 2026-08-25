using System.Windows.Input;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 25885, "Command event spills to parent if child command is busy", PlatformAffected.iOS)]
public partial class Issue25885 : ContentPage
{
	bool _childCanExecute = true;
	int _childExecutionCount;
	int _parentExecutionCount;

	public Issue25885()
	{
		InitializeComponent();

		ChildCommand = new Command(ExecuteChildCommand, () => _childCanExecute);
		ParentCommand = new Command(ExecuteParentCommand);
		BindingContext = this;
	}

	public ICommand ChildCommand { get; }

	public ICommand ParentCommand { get; }

	void ExecuteChildCommand()
	{
		_childExecutionCount++;
		ChildCountLabel.Text = $"Child executions: {_childExecutionCount}";
		_childCanExecute = false;
		((Command)ChildCommand).ChangeCanExecute();
		ChildStateLabel.Text = "Child command: unavailable";
	}

	void ExecuteParentCommand()
	{
		_parentExecutionCount++;
		ParentCountLabel.Text = $"Parent executions: {_parentExecutionCount}";
	}
}
