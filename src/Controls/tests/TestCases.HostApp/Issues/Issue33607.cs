#if WINDOWS
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Internals;
using ILayout = Microsoft.Maui.ILayout;
using IView = Microsoft.Maui.IView;

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 33607, "[Windows] ObjectDisposedException after closing window", PlatformAffected.UWP)]
public class Issue33607 : ContentPage
{
	const int CycleCount = 3;

	public Issue33607()
	{
		var cycleStateLabel = CreateStateLabel("Issue33607CompletedCycles", "-1");
		var destroyedWindowsLabel = CreateStateLabel("Issue33607DestroyedWindows", "0");
		var postCloseCallbacksLabel = CreateStateLabel("Issue33607PostCloseCallbacks", "0");
		var arrangedLayoutsLabel = CreateStateLabel("Issue33607ArrangedLayouts", "0");
		var mutatedCollectionsLabel = CreateStateLabel("Issue33607MutatedCollections", "0");
		var exceptionCountLabel = CreateStateLabel("Issue33607ExceptionCount", "0");
		var destroyedCompleteLabel = CreateStateLabel("Issue33607DestroyedComplete", "Destroyed");
		destroyedCompleteLabel.IsVisible = false;
		var callbacksCompleteLabel = CreateStateLabel("Issue33607CallbacksComplete", "Mutated");
		callbacksCompleteLabel.IsVisible = false;
		var completionLabel = CreateStateLabel("Issue33607Completed", "Completed");
		completionLabel.IsVisible = false;

		var runButton = new Button
		{
			AutomationId = "Issue33607Run",
			Text = "Run three window-close cycles"
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 16,
			Children =
			{
				runButton,
				cycleStateLabel,
				destroyedWindowsLabel,
				postCloseCallbacksLabel,
				arrangedLayoutsLabel,
				mutatedCollectionsLabel,
				exceptionCountLabel,
				destroyedCompleteLabel,
				callbacksCompleteLabel,
				completionLabel
			}
		};

		runButton.Clicked += (_, _) =>
		{
			runButton.IsEnabled = false;
			cycleStateLabel.Text = "0";

			var destroyedWindows = new HashSet<Window>();
			var arrangedLayouts = new HashSet<ILayout>();
			var mutatedCollections = new HashSet<ObservableCollection<string>>();
			var postCloseCallbacks = 0;
			var completedCycles = 0;
			var disposedServiceExceptions = 0;
			var app = Application.Current ?? throw new InvalidOperationException("Application.Current must be available.");

			StartCycle(1);

			void StartCycle(int cycleNumber)
			{
				var items = new ObservableCollection<string>
				{
					$"Initial item for cycle {cycleNumber}"
				};

				var concreteLayout = new VerticalStackLayout();
				ILayout itemsLayout = concreteLayout;
				concreteLayout.Add(CreateItemView(items[0]));
				items.CollectionChanged += (_, e) =>
					NotifyCollectionChangedEventArgsExtensions.Apply(
						e,
						(item, index, _) => concreteLayout.Insert(index, CreateItemView((string)item)),
						(_, index) => concreteLayout.RemoveAt(index),
						() =>
						{
							concreteLayout.Clear();
							foreach (var item in items)
								concreteLayout.Add(CreateItemView(item));
						});

				arrangedLayouts.Add(itemsLayout);
				arrangedLayoutsLabel.Text = arrangedLayouts.Count.ToString();

				var page = new ContentPage
				{
					Title = $"Secondary window {cycleNumber}",
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 12,
						Children =
						{
							new Label { Text = "Collection-backed layout window ready" },
							concreteLayout
						}
					}
				};

				var secondaryWindow = new Window
				{
					Page = page,
					Title = page.Title
				};

				page.Loaded += OnSecondaryPageLoaded;
				secondaryWindow.Destroying += OnSecondaryWindowDestroying;
				app.OpenWindow(secondaryWindow);

				void OnSecondaryPageLoaded(object sender, EventArgs args)
				{
					page.Loaded -= OnSecondaryPageLoaded;
					page.Dispatcher.Dispatch(() => app.CloseWindow(secondaryWindow));
				}

				void OnSecondaryWindowDestroying(object sender, EventArgs args)
				{
					secondaryWindow.Destroying -= OnSecondaryWindowDestroying;
					destroyedWindows.Add(secondaryWindow);
					destroyedWindowsLabel.Text = destroyedWindows.Count.ToString();
					if (destroyedWindows.Count == CycleCount)
						destroyedCompleteLabel.IsVisible = true;

					Dispatcher.Dispatch(CompleteCycle);
				}

				void CompleteCycle()
				{
					var postCloseItem = $"Post-close item {cycleNumber}";
					try
					{
						items.Add(postCloseItem);
					}
					catch (ObjectDisposedException exception) when (exception.ObjectName == "IServiceProvider")
					{
						disposedServiceExceptions++;
					}

					postCloseCallbacks++;
					if (items.Contains(postCloseItem))
						mutatedCollections.Add(items);

					completedCycles++;
					postCloseCallbacksLabel.Text = postCloseCallbacks.ToString();
					mutatedCollectionsLabel.Text = mutatedCollections.Count.ToString();
					exceptionCountLabel.Text = disposedServiceExceptions.ToString();
					cycleStateLabel.Text = completedCycles.ToString();
					if (postCloseCallbacks == CycleCount)
						callbacksCompleteLabel.IsVisible = true;

					if (completedCycles < CycleCount)
					{
						StartCycle(completedCycles + 1);
						return;
					}

					completionLabel.IsVisible = true;
				}
			}
		};
	}

	static IView CreateItemView(string item)
	{
		var itemLabel = new Label();
		itemLabel.SetBinding(Label.TextProperty, ".");

		return new ContentView
		{
			BindingContext = item,
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
	}

	static Label CreateStateLabel(string automationId, string text) =>
		new()
		{
			AutomationId = automationId,
			Text = text
		};
}
#endif

