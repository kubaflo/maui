using System;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 35760, "[Android] Shell toolbar title does not update after switching tabs while action mode is open", PlatformAffected.Android)]
public partial class Issue35760 : Shell
{
	bool _pageTwoObserved;

	public Issue35760()
	{
		InitializeComponent();
		Navigated += OnShellNavigated;
	}

	void OnNavigateClicked(object sender, EventArgs e)
	{
		_ = GoToAsync("//Page2");
	}

	void OnShellNavigated(object sender, ShellNavigatedEventArgs e)
	{
		if (_pageTwoObserved || !e.Current.Location.OriginalString.Contains("Page2", StringComparison.Ordinal))
			return;

		_pageTwoObserved = true;
		FirstShellContent.Title = "First tab";
		FirstPageLayout.Children.Remove(NavigationStateLabel);
		SecondPageLayout.Children.Insert(0, NavigationStateLabel);
		NavigationStateLabel.Text = "2";
	}
}
