namespace Maui.Controls.Sample.Issues;

[XamlCompilation(XamlCompilationOptions.Compile)]
[Issue(IssueTracker.Github, 32213, "Windows CollectionView header and footer templates are ignored", PlatformAffected.WinPhone)]
public partial class Issue32213 : ContentPage
{
	bool _issueHeaderTemplateLoaded;
	bool _issueFooterTemplateLoaded;

	public Issue32213()
	{
		InitializeComponent();

		var items = new[] { "1", "2", "3", "4" };
		IssueCollectionView.ItemsSource = items;
		ArrangementLabel.Text =
			$"Header={IssueCollectionView.Header}; Footer={IssueCollectionView.Footer}; " +
			$"HeaderTemplate={IssueCollectionView.HeaderTemplate is not null}; " +
			$"FooterTemplate={IssueCollectionView.FooterTemplate is not null}; Items={items.Length}";
	}

	void OnShowCollectionClicked(object sender, EventArgs e)
	{
		_issueHeaderTemplateLoaded = false;
		_issueFooterTemplateLoaded = false;
		IssueCollectionView.IsVisible = true;
		ShowCollectionButton.IsEnabled = false;
		Dispatcher.Dispatch(() => CheckTemplatesButton.IsVisible = true);
	}

	void OnHeaderTemplateLoaded(object sender, EventArgs e)
	{
		_issueHeaderTemplateLoaded = true;
	}

	void OnFooterTemplateLoaded(object sender, EventArgs e)
	{
		_issueFooterTemplateLoaded = true;
	}

	void OnCheckTemplatesClicked(object sender, EventArgs e)
	{
		ArrangementLabel.Text = _issueHeaderTemplateLoaded && _issueFooterTemplateLoaded
			? "Both templates loaded"
			: "One or more templates did not load";
	}
}
