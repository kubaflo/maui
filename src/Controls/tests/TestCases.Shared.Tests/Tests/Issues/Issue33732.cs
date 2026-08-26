#if WINDOWS
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues;

public class Issue33732 : _IssuesUITest
{
	public Issue33732(TestDevice device) : base(device)
	{
	}

	public override string Issue => "Repeated MakeVisible moves an already-visible CollectionView item";

	[Test]
	[Category(UITestCategories.CollectionView)]
	public void RepeatedMakeVisibleKeepsItemAtViewportBottom()
	{
		App.WaitForElement("ScrollButton");
		App.WaitForElement("MonkeyCollection");

		var configuration = App.WaitForElement("ConfigurationLabel").GetText();
		if (configuration is null)
		{
			throw new InvalidOperationException("ConfigurationLabel text was null.");
		}

		Assert.That(configuration, Does.Contain("Position=MakeVisible; Animate=True; Density="));

		var densityText = configuration.Split("Density=", StringSplitOptions.None)[1];
		Assert.That(double.TryParse(densityText, NumberStyles.Float, CultureInfo.InvariantCulture, out var density), Is.True);
		Assert.That(density, Is.GreaterThan(0));

		var requestCount = -1;
		Assert.That(TryReadRequestCount(out requestCount), Is.True);
		Assert.That(requestCount, Is.Zero);

		App.Tap("ScrollButton");
		Assert.That(App.WaitForTextToBePresentInElement("RequestCountLabel", "Requests=1"), Is.True);
		Assert.That(TryReadRequestCount(out requestCount), Is.True);
		Assert.That(requestCount, Is.EqualTo(1));

		App.WaitForElement("Proboscis Monkey");
		var firstGeometry = WaitForStableGeometry();
		var expectedRowHeight = 80 * density;
		var tolerance = Math.Max(4 * density, 2);
		var firstTargetBottom = firstGeometry.Target.Y + firstGeometry.Target.Height;
		var collectionBottom = firstGeometry.Collection.Y + firstGeometry.Collection.Height;

		Assert.That(firstGeometry.Collection.Width, Is.GreaterThan(0));
		Assert.That(firstGeometry.Collection.Height, Is.GreaterThan(0));
		Assert.That(firstGeometry.Target.Width, Is.GreaterThan(0));
		Assert.That(firstGeometry.Target.Height, Is.EqualTo(expectedRowHeight).Within(tolerance));
		Assert.That(firstGeometry.Target.Y, Is.GreaterThanOrEqualTo(firstGeometry.Collection.Y - tolerance));
		Assert.That(firstTargetBottom, Is.LessThanOrEqualTo(collectionBottom + tolerance));
		Assert.That(firstTargetBottom, Is.EqualTo(collectionBottom).Within(tolerance),
			"Clean first MakeVisible request should bottom-align the fully visible 80-DIP target row.");

		App.Tap("ScrollButton");
		Assert.That(App.WaitForTextToBePresentInElement("RequestCountLabel", "Requests=2"), Is.True);
		Assert.That(TryReadRequestCount(out requestCount), Is.True);
		Assert.That(requestCount, Is.EqualTo(2));

		App.WaitForElement("Proboscis Monkey");
		var secondGeometry = WaitForStableGeometry();
		var secondTargetBottom = secondGeometry.Target.Y + secondGeometry.Target.Height;
		var secondCollectionBottom = secondGeometry.Collection.Y + secondGeometry.Collection.Height;

		Assert.That(secondGeometry.Target.Width, Is.GreaterThan(0));
		Assert.That(secondGeometry.Target.Height, Is.EqualTo(expectedRowHeight).Within(tolerance));
		Assert.That(secondTargetBottom, Is.EqualTo(secondCollectionBottom).Within(tolerance),
			$"Issue33732: after second MakeVisible tap, Proboscis Monkey must remain bottom-aligned. " +
			$"Target top/bottom={secondGeometry.Target.Y}/{secondTargetBottom}; " +
			$"collection top/bottom={secondGeometry.Collection.Y}/{secondCollectionBottom}; " +
			$"expected bottom={secondCollectionBottom}; tolerance={tolerance}.");
	}

	bool TryReadRequestCount(out int requestCount)
	{
		requestCount = -1;
		var text = App.WaitForElement("RequestCountLabel").GetText();
		if (text is null || !text.StartsWith("Requests=", StringComparison.Ordinal))
		{
			return false;
		}

		return int.TryParse(text["Requests=".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out requestCount);
	}

	(Rectangle Target, Rectangle Collection) WaitForStableGeometry()
	{
		var timeout = TimeSpan.FromSeconds(10);
		var requiredStableDuration = TimeSpan.FromMilliseconds(500);
		var stopwatch = Stopwatch.StartNew();
		var stableSince = stopwatch.Elapsed;
		var previousTarget = Rectangle.Empty;
		var previousCollection = Rectangle.Empty;

		while (stopwatch.Elapsed < timeout)
		{
			var target = App.WaitForElement("TargetRow").GetRect();
			var collection = App.WaitForElement("MonkeyCollection").GetRect();

			if (AreClose(target, previousTarget) && AreClose(collection, previousCollection))
			{
				if (stopwatch.Elapsed - stableSince >= requiredStableDuration)
				{
					return (target, collection);
				}
			}
			else
			{
				stableSince = stopwatch.Elapsed;
			}

			previousTarget = target;
			previousCollection = collection;
		}

		Assert.Fail("CollectionView and target-row geometry did not settle within the bounded probe.");
		return (previousTarget, previousCollection);
	}

	static bool AreClose(Rectangle first, Rectangle second) =>
		Math.Abs(first.X - second.X) <= 1 &&
		Math.Abs(first.Y - second.Y) <= 1 &&
		Math.Abs(first.Width - second.Width) <= 1 &&
		Math.Abs(first.Height - second.Height) <= 1;
}
#endif
