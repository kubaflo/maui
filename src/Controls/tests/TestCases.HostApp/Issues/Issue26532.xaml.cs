namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 26532, "Changing BindingContext clears the previous Picker selection", PlatformAffected.Android)]
public partial class Issue26532 : TestContentPage
{
	readonly QuestionViewModel _firstQuestion;
	readonly QuestionViewModel _secondQuestion;

	public Issue26532()
	{
		_firstQuestion = new QuestionViewModel(
			["Answer A", "Answer B"],
			OnFirstQuestionSelectionChanged);
		_secondQuestion = new QuestionViewModel([], static (_, _) => { });
		BindingContext = _firstQuestion;
		InitializeComponent();
	}

	protected override void Init()
	{
	}

	void OnFirstQuestionSelectionChanged(string previous, string current)
	{
		OriginalSelectionStatus.Text = $"Original selected: {current ?? "null"}";

		if (current is not null)
			SelectionStatus.Text = $"Selection received: {current}";
	}

	void OnNextClicked(object sender, EventArgs e)
	{
		BindingContext = _secondQuestion;
		TransitionStatus.Text = $"Question index: 1; answer count: {_secondQuestion.Answers.Length}";
		OriginalSelectionStatus.Text = $"Original selected: {_firstQuestion.SelectedAnswer ?? "null"}";
	}

	sealed class QuestionViewModel
	{
		readonly Action<string, string> _selectionChanged;
		string _selectedAnswer = null!;

		public QuestionViewModel(string[] answers, Action<string, string> selectionChanged)
		{
			Answers = answers;
			_selectionChanged = selectionChanged;
		}

		public string[] Answers { get; }

		public string SelectedAnswer
		{
			get => _selectedAnswer;
			set
			{
				if (_selectedAnswer == value)
					return;

				var previous = _selectedAnswer;
				_selectedAnswer = value;
				_selectionChanged(previous, value);
			}
		}
	}
}
