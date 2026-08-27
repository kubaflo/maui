#if WINDOWS
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue34754 : _IssuesUITest
{
	public Issue34754(TestDevice device) : base(device) { }

	public override string Issue => "WinUI drag and drop and CanMixGroups support was not available";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void GroupedItemCanBeDraggedBetweenGroups()
	{
		var rootElement = App.WaitForElement("Issue34754Root");
		var alphaOneElement = App.WaitForElement("AlphaOne");
		var groupBHeaderElement = App.WaitForElement("GroupBHeader");
		var betaOneElement = App.WaitForElement("BetaOne");
		var betaTwoElement = App.WaitForElement("BetaTwo");
		var propertyStateElement = App.WaitForElement("PropertyState");
		var transitionStateElement = App.WaitForElement("TransitionState");

		Assert.That(rootElement, Is.Not.Null);
		Assert.That(alphaOneElement, Is.Not.Null);
		Assert.That(groupBHeaderElement, Is.Not.Null);
		Assert.That(betaOneElement, Is.Not.Null);
		Assert.That(betaTwoElement, Is.Not.Null);
		Assert.That(propertyStateElement, Is.Not.Null);
		Assert.That(transitionStateElement, Is.Not.Null);

		Assert.That(propertyStateElement.GetText(),
			Is.EqualTo("CanReorderItems=True;CanMixGroups=True"));
		Assert.That(transitionStateElement.GetText(),
			Is.EqualTo("Callback=-1;Group=A;TransitionObserved=False"));

		App.Tap("AlphaOne");
		var selectionObserved = App.WaitForTextToBePresentInElement(
			"SelectionState",
			"Selected=Alpha one",
			TimeSpan.FromSeconds(5));
		Assert.That(selectionObserved, Is.True, "Pointer selection did not identify Alpha one");

		var rootRect = rootElement.GetRect();
		var alphaRect = alphaOneElement.GetRect();
		var groupBHeaderRect = groupBHeaderElement.GetRect();
		var betaOneRect = betaOneElement.GetRect();
		var betaTwoRect = betaTwoElement.GetRect();
		var startX = alphaRect.CenterX();
		var startY = alphaRect.CenterY();
		var endX = startX;
		var travel = rootRect.Height * 0.24f;
		var endY = startY + travel;

		Assert.Multiple(() =>
		{
			Assert.That(startX, Is.InRange(rootRect.X, rootRect.X + rootRect.Width),
				"Drag start must be inside the measured root");
			Assert.That(startY, Is.InRange(rootRect.Y, rootRect.Y + rootRect.Height),
				"Drag start must be inside the measured root");
			Assert.That(endX, Is.InRange(rootRect.X, rootRect.X + rootRect.Width),
				"Drag endpoint must be inside the measured root");
			Assert.That(endY, Is.InRange(rootRect.Y, rootRect.Y + rootRect.Height),
				"Drag endpoint must be inside the measured root");
			Assert.That(endY, Is.GreaterThanOrEqualTo(groupBHeaderRect.Y + groupBHeaderRect.Height),
				"Drag endpoint must cross the Group B header");
			Assert.That(endY, Is.GreaterThanOrEqualTo(betaOneRect.Y),
				"Drag endpoint must reach the Group B item surface");
			Assert.That(endY, Is.LessThanOrEqualTo(betaTwoRect.Y + betaTwoRect.Height),
				"Drag endpoint must remain on the Group B item surface");
			Assert.That(travel, Is.GreaterThan(10),
				"Drag travel must exceed Windows pointer movement slop");
		});

		App.DragCoordinates(startX, startY, endX, endY);

		var transitionObserved = App.WaitForTextToBePresentInElement(
			"TransitionState",
			"TransitionObserved=True",
			TimeSpan.FromSeconds(5));
		var concreteTransitionState = App.FindElement("TransitionState");
		Assert.That(concreteTransitionState, Is.Not.Null);
		var concreteTransitionText = concreteTransitionState.GetText();

		Assert.That(transitionObserved, Is.True,
			$"Cross-group drag produced no collection transition. Start=({startX},{startY}); End=({endX},{endY}); State={concreteTransitionText}");

		var movedToGroupB = App.WaitForTextToBePresentInElement(
			"TransitionState",
			"Group=B",
			TimeSpan.FromSeconds(5));
		Assert.That(movedToGroupB, Is.True,
			$"Alpha one did not move into Group B. State={concreteTransitionText}");

		var movedAlphaOneElement = App.FindElement("AlphaOne");
		var movedGroupBHeaderElement = App.FindElement("GroupBHeader");
		Assert.That(movedAlphaOneElement, Is.Not.Null);
		Assert.That(movedGroupBHeaderElement, Is.Not.Null);
		var movedAlphaRect = movedAlphaOneElement.GetRect();
		var movedGroupBHeaderRect = movedGroupBHeaderElement.GetRect();
		Assert.That(movedAlphaRect.Y,
			Is.GreaterThanOrEqualTo(movedGroupBHeaderRect.Y + movedGroupBHeaderRect.Height),
			"Alpha one's native element must be rendered below the Group B header");
	}
}
#endif
