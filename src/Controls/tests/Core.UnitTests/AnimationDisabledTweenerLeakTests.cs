using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Animations;
using Xunit;

namespace Microsoft.Maui.Controls.Core.UnitTests
{
	// Reproduces: when animations are disabled (Ticker.SystemEnabled == false), the
	// low-level IAnimationManager.Add/Insert tweener APIs strand their tweener (and its
	// captured closure) in the static AnimationExtensions.s_tweeners dictionary forever.
	//
	// AnimationExtensions.Add does:  s_tweeners[id] = animation; animation.Commit(manager);
	// and only removes the entry from a one-shot `animation.Finished` handler.
	// But Animation.Commit(IAnimationManager) just calls manager.Add(animation), and
	// AnimationManager.Add early-returns when !Ticker.SystemEnabled WITHOUT ever firing
	// Finished -> the s_tweeners entry (and everything its `step` closure captures) leaks.
	public class AnimationDisabledTweenerLeakTests
	{
		sealed class DisabledTicker : Ticker
		{
			public override bool SystemEnabled => false;
			public override bool IsRunning => false;
			public override void Start() { }
			public override void Stop() { }
		}

		static List<WeakReference> AddTweenersWhileDisabled(int count, bool removeAfterwards)
		{
			var refs = new List<WeakReference>(count);
			var manager = new AnimationManager(new DisabledTicker());

			for (int i = 0; i < count; i++)
			{
				// A 1 MB payload captured by the animation step closure, standing in for
				// the view / view-model graph a real `step` would capture.
				var payload = new byte[1024 * 1024];
				refs.Add(new WeakReference(payload));

				int id = manager.Add(_ => { GC.KeepAlive(payload); });

				if (removeAfterwards)
				{
					// What `animation.Finished` *should* have done, but never does when disabled.
					manager.Remove(id);
				}
			}

			return refs;
		}

		static int AliveAfterFullGC(List<WeakReference> refs)
		{
			for (int i = 0; i < 6; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}

			return refs.Count(r => r.IsAlive);
		}

		[Fact]
		public void DisabledAnimationStrandsTweenerInStaticDictionary()
		{
			// Leaky: Add(step) while animations are disabled and never removed.
			var leaky = AddTweenersWhileDisabled(30, removeAfterwards: false);
			int leakyAlive = AliveAfterFullGC(leaky);

			// Mitigation: same, but Remove(id) afterwards (what Finished would have done).
			var mitigated = AddTweenersWhileDisabled(30, removeAfterwards: true);
			int mitigatedAlive = AliveAfterFullGC(mitigated);

			// The leak: all 30 payloads survive a full GC, stranded in static s_tweeners.
			Assert.Equal(30, leakyAlive);
			// Removing the tweener (the missing cleanup) releases everything.
			Assert.Equal(0, mitigatedAlive);
		}
	}
}
