using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Internals;
using ILayout = Microsoft.Maui.ILayout;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	int _cycleCount;
	int _templateReadyCount;
	int _closeReturnCount;
	int _successfulUpdateCount;

	public Issue33607()
	{
		var templateReadyLabel = new Label
		{
			AutomationId = "Issue33607TemplateReadyCount",
			Text = "Template-ready count: 0"
		};
		var closeReturnLabel = new Label
		{
			AutomationId = "Issue33607CloseReturnCount",
			Text = "Close-return count: 0"
		};
		var successfulUpdateLabel = new Label
		{
			AutomationId = "Issue33607SuccessfulUpdateCount",
			Text = "Successful-update count: 0"
		};
		var cycleButton = new Button
		{
			AutomationId = "Issue33607CycleButton",
			Text = "Open, close, and update"
		};

		cycleButton.Clicked += (_, _) =>
		{
			cycleButton.IsEnabled = false;
			_cycleCount++;

			var initialItemText = $"Initial item for cycle {_cycleCount}";
			var items = new ObservableCollection<string>
			{
				initialItemText
			};
			var itemsLayout = new VerticalStackLayout();
			ILayout reportedLayout = itemsLayout;
			var postCloseItemText = $"Post-close item {_cycleCount}";
			var appliedAction = (NotifyCollectionChangedAction)(-1);
			var appliedPostCloseInsert = false;
			items.CollectionChanged += (_, e) =>
			{
				appliedAction = e.Apply(
					(item, index, create) =>
					{
						if (create && index == 1 && Equals(item, postCloseItemText))
							appliedPostCloseInsert = true;
					},
					(_, _) => { },
					() => { });
			};
			BindableLayout.SetItemsSource(itemsLayout, items);
			BindableLayout.SetItemTemplate(itemsLayout, new DataTemplate(() =>
			{
				var itemLabel = new Label();
				itemLabel.SetBinding(Label.TextProperty, ".");

				return new ContentView
				{
					Content = new Border
					{
						Content = new VerticalStackLayout
						{
							Children =
							{
								itemLabel
							}
						}
					}
				};
			}));

			var page = new ContentPage
			{
				Title = $"Issue 33607 cycle {_cycleCount}",
				Content = new VerticalStackLayout
				{
					Padding = 24,
					Children =
					{
						new Label { Text = "Secondary window ready" },
						itemsLayout
					}
				}
			};
			var secondaryWindow = new Window
			{
				Page = page,
				Title = page.Title
			};
			var application = Application.Current ?? throw new InvalidOperationException("Application.Current must be available.");

			page.Loaded += (_, _) =>
			{
				if (itemsLayout.Children.FirstOrDefault() is ContentView itemContentView &&
					itemContentView.Content is Border itemBorder &&
					itemBorder.Content is VerticalStackLayout itemStack &&
					itemStack.Children.FirstOrDefault() is Label itemLabel &&
					itemLabel.Text == initialItemText &&
					itemLabel.Handler is not null)
				{
					_templateReadyCount++;
					templateReadyLabel.Text = $"Template-ready count: {_templateReadyCount}";
				}

				page.Dispatcher.Dispatch(() =>
				{
					application.CloseWindow(secondaryWindow);
					_closeReturnCount++;

					try
					{
						items.Add(postCloseItemText);
						if (appliedAction == NotifyCollectionChangedAction.Add &&
							appliedPostCloseInsert &&
							reportedLayout.Count == 2)
						{
							_successfulUpdateCount++;
							successfulUpdateLabel.Text = $"Successful-update count: {_successfulUpdateCount}";
						}
					}
					catch (ObjectDisposedException)
					{
					}

					closeReturnLabel.Text = $"Close-return count: {_closeReturnCount}";
					cycleButton.IsEnabled = true;
				});
			};

			application.OpenWindow(secondaryWindow);
		};

		Content = new VerticalStackLayout
		{
			Children =
			{
				templateReadyLabel,
				closeReturnLabel,
				successfulUpdateLabel,
				cycleButton
			}
		};
	}
}

