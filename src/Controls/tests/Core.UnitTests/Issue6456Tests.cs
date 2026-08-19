using Microsoft.Maui.Controls.Internals;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	public class Issue6456 : BaseTestFixture
	{
		[Fact]
		public void InitializingBindingsWithRefreshingTrueDoesNotExecuteRefreshCommand()
		{
			int commandExecutions = 0;
			var checkConfigurationCommand = new Command(() => commandExecutions++);
			var viewModel = new Issue6456ViewModel
			{
				IsChecking = true,
				CheckConfigurationCommand = checkConfigurationCommand
			};
			var statusLabel = new Label
			{
				FontSize = 18,
				Text = "NO BUG:"
			};
			var triggerButton = new Button
			{
				Text = "Initialize page bindings"
			};
			var contentLabel = new Label
			{
				FontSize = 20,
				Text = "RefreshView is mounted before page binding initialization"
			};
			var scrollView = new ScrollView
			{
				Content = contentLabel
			};
			var refreshView = new RefreshView
			{
				Content = scrollView
			};
			var commandBinding = new Binding(nameof(Issue6456ViewModel.CheckConfigurationCommand));
			var isRefreshingBinding = new Binding(nameof(Issue6456ViewModel.IsChecking));
			refreshView.SetBinding(RefreshView.CommandProperty, commandBinding);
			refreshView.SetBinding(RefreshView.IsRefreshingProperty, isRefreshingBinding);

			var grid = new Grid
			{
				Padding = 24,
				RowSpacing = 18,
				RowDefinitions = new RowDefinitionCollection
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star }
				}
			};
			grid.Add(statusLabel);
			grid.Add(triggerButton);
			Grid.SetRow(triggerButton, 1);
			grid.Add(refreshView);
			Grid.SetRow(refreshView, 2);
			var page = new ContentPage
			{
				Content = grid
			};

			int clickCallbacks = 0;
			object observedRefreshViewBindingContext = -1;
			refreshView.BindingContextChanged += (_, _) => observedRefreshViewBindingContext = refreshView.BindingContext;
			triggerButton.Clicked += (_, _) =>
			{
				clickCallbacks++;
				page.BindingContext = viewModel;
			};

			Assert.Same(grid, page.Content);
			Assert.Same(page, grid.Parent);
			Assert.Same(grid, statusLabel.Parent);
			Assert.Same(grid, triggerButton.Parent);
			Assert.Same(grid, refreshView.Parent);
			Assert.Same(refreshView, scrollView.Parent);
			Assert.Same(scrollView, contentLabel.Parent);
			Assert.Same(commandBinding, refreshView.GetContext(RefreshView.CommandProperty).Bindings.GetValue());
			Assert.Same(isRefreshingBinding, refreshView.GetContext(RefreshView.IsRefreshingProperty).Bindings.GetValue());
			Assert.Same(checkConfigurationCommand, viewModel.CheckConfigurationCommand);
			Assert.Null(page.BindingContext);
			Assert.Null(refreshView.BindingContext);
			Assert.Null(refreshView.Command);
			Assert.False(refreshView.IsRefreshing);
			Assert.Equal(0, commandExecutions);

			((IButtonController)triggerButton).SendClicked();

			Assert.Equal(1, clickCallbacks);
			Assert.Same(viewModel, page.BindingContext);
			Assert.Same(viewModel, refreshView.BindingContext);
			Assert.Same(viewModel, observedRefreshViewBindingContext);
			Assert.Same(checkConfigurationCommand, refreshView.Command);
			Assert.True(refreshView.IsRefreshing);
			Assert.True(commandExecutions == 0, $"Refresh command executions after binding initialization: observed {commandExecutions}, expected 0.");
		}

		public sealed class Issue6456ViewModel
		{
			public bool IsChecking { get; set; }

			public Command CheckConfigurationCommand { get; set; }
		}
	}
}
