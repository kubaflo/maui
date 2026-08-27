namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33530, "Border with Rotation and Start alignment is positioned incorrectly on initial load", PlatformAffected.Android)]
public partial class Issue33530 : ContentPage
{
	Border _referenceBorder = null!;

	public Issue33530()
	{
		InitializeComponent();
	}

	async void OnOpenReferenceClicked(object sender, EventArgs e)
	{
		var content = CreateTemplateContent("ReferenceBorderTemplate");
		_referenceBorder = FindRequiredName<Border>(content, "ReferenceBorder");

		MoveToModal(content, ReferenceLifecycleToken);
		MoveToModal(content, RotateReferenceButton);
		MoveToModal(content, ReferenceArrangementToken);
		RotateReferenceButton.IsVisible = true;

		_referenceBorder.Loaded += OnReferenceBorderLoaded;
		await Navigation.PushModalAsync(CreateTransparentModal(content), false);
	}

	void OnReferenceBorderLoaded(object sender, EventArgs e)
	{
		_referenceBorder.Loaded -= OnReferenceBorderLoaded;
		ReferenceLifecycleToken.Text = "Loaded";
	}

	void OnRotateReferenceClicked(object sender, EventArgs e)
	{
		_referenceBorder.Rotation = -90;
		_referenceBorder.HorizontalOptions = LayoutOptions.Start;
		_referenceBorder.Dispatcher.Dispatch(() =>
		{
			ReferenceArrangementToken.Text = DescribeArrangement(_referenceBorder);
		});
	}

	async void OnOpenAffectedClicked(object sender, EventArgs e)
	{
		var content = CreateTemplateContent("AffectedBorderTemplate");
		var affectedBorder = FindRequiredName<Border>(content, "AffectedBorder");
		FindRequiredName<Label>(content, "AffectedContentLabel");

		MoveToModal(content, AffectedLifecycleToken);
		MoveToModal(content, AffectedArrangementToken);
		affectedBorder.Loaded += OnAffectedBorderLoaded;

		await Navigation.PushModalAsync(CreateTransparentModal(content), false);

		void OnAffectedBorderLoaded(object loadedSender, EventArgs loadedArgs)
		{
			affectedBorder.Loaded -= OnAffectedBorderLoaded;
			AffectedLifecycleToken.Text = "Loaded";
			AffectedArrangementToken.Text = DescribeArrangement(affectedBorder);
		}
	}

	Grid CreateTemplateContent(string resourceKey)
	{
		if (!Resources.TryGetValue(resourceKey, out var resource) || resource is not DataTemplate template)
			throw new InvalidOperationException($"Resource '{resourceKey}' is not a DataTemplate.");

		if (template.CreateContent() is not Grid content)
			throw new InvalidOperationException($"Resource '{resourceKey}' did not create a Grid.");

		return content;
	}

	static T FindRequiredName<T>(Element content, string name) where T : Element
	{
		return content.FindByName<T>(name)
			?? throw new InvalidOperationException($"Template element '{name}' was not found.");
	}

	static ContentPage CreateTransparentModal(Grid content)
	{
		return new ContentPage
		{
			BackgroundColor = Colors.Transparent,
			Content = content
		};
	}

	static void MoveToModal(Grid content, View view)
	{
		if (view.Parent is Layout parent)
			parent.Children.Remove(view);

		content.Children.Add(view);
	}

	static string DescribeArrangement(Border border)
	{
		var hasExpectedContent = border.Content is VerticalStackLayout stack &&
			stack.Children.Count == 2 &&
			stack.Children[0] is BoxView &&
			stack.Children[1] is Label;

		return $"Rotation={border.Rotation:0};HorizontalOptions={border.HorizontalOptions.Alignment};Shadow={border.Shadow is not null};Content={hasExpectedContent}";
	}
}
