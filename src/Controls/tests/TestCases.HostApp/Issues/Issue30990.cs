namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 30990, "Shell toolbar ignores shell properties", PlatformAffected.Android)]
public class Issue30990 : Shell
{
	const string NativeStatePending = "CALLBACK=PENDING";

#if ANDROID
	NativeStateObserver _nativeStateObserver = null!;
#endif

	public Issue30990()
	{
		Resources.Add(new Style(typeof(Shell))
		{
			ApplyToDerivedTypes = true,
			Setters =
			{
				new Setter { Property = Shell.ForegroundColorProperty, Value = Colors.Red },
				new Setter { Property = Shell.TitleColorProperty, Value = Colors.Red }
			}
		});

		var textToolbarItem = new ToolbarItem { Text = "Text 1" };
		var iconToolbarItem = new ToolbarItem
		{
			AutomationId = "IconToolbarItem",
			IconImageSource = "groceries.png"
		};
		var nativeStateLabel = new Label
		{
			AutomationId = "NativeStateLabel",
			Text = NativeStatePending
		};
		var page = new ContentPage
		{
			Content = new VerticalStackLayout
			{
				Children =
				{
					new Label { Text = "Hello", TextColor = Colors.White },
					nativeStateLabel
				}
			}
		};

		page.ToolbarItems.Add(textToolbarItem);
		page.ToolbarItems.Add(iconToolbarItem);
		Items.Add(new ShellContent { Content = page });

#if ANDROID
		page.Loaded += (_, _) =>
		{
			if (_nativeStateObserver is null && Handler?.PlatformView is Android.Views.View rootView)
			{
				_nativeStateObserver = new NativeStateObserver(rootView, this, page, nativeStateLabel);
				rootView.ViewTreeObserver?.AddOnPreDrawListener(_nativeStateObserver);
			}
		};
#endif
	}

#if ANDROID
	sealed class NativeStateObserver : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnPreDrawListener
	{
		readonly Android.Views.View _rootView;
		readonly Issue30990 _issueShell;
		readonly ContentPage _contentPage;
		readonly Label _nativeStateLabel;

		public NativeStateObserver(Android.Views.View rootView, Issue30990 issueShell, ContentPage contentPage, Label nativeStateLabel)
		{
			_rootView = rootView;
			_issueShell = issueShell;
			_contentPage = contentPage;
			_nativeStateLabel = nativeStateLabel;
		}

		public bool OnPreDraw()
		{
			if (!TryFindDescendant(_rootView, out AndroidX.AppCompat.Widget.Toolbar toolbar) ||
				toolbar.Menu is null ||
				toolbar.Menu.Size() < 2)
				return true;

			var textMenuItem = toolbar.Menu.GetItem(0);
			var iconMenuItem = toolbar.Menu.GetItem(1);
			if (textMenuItem is null || iconMenuItem is null || iconMenuItem.Icon is null)
				return true;

			var textView = toolbar.FindViewById(textMenuItem.ItemId);
			var iconView = toolbar.FindViewById(iconMenuItem.ItemId);
			if (textView is null ||
				iconView is null ||
				textView.Width <= 0 ||
				textView.Height <= 0 ||
				iconView.Width <= 0 ||
				iconView.Height <= 0)
				return true;

			var textArgb = GetDominantRenderedColor(textView);
			var iconArgb = GetDominantRenderedColor(iconView);
			if (textArgb is null || iconArgb is null)
				return true;

			var effectiveColor = Shell.GetForegroundColor(_contentPage) ?? Shell.GetForegroundColor(_issueShell);
			if (effectiveColor is null)
				return true;

			var iconIdentity = iconView.ContentDescription?.ToString() ?? string.Empty;
			_nativeStateLabel.Text =
				$"CALLBACK=COMPLETE;TextId={textMenuItem.TitleFormatted};IconId={iconIdentity};IconPresent=True;" +
				$"Effective={FormatColor(effectiveColor.ToUint())};" +
				$"Text={FormatColor(unchecked((uint)textArgb.Value))};Icon={FormatColor(unchecked((uint)iconArgb.Value))}";

			_rootView.ViewTreeObserver?.RemoveOnPreDrawListener(this);
			return true;
		}

		static bool TryFindDescendant<T>(Android.Views.View view, out T result) where T : Android.Views.View
		{
			if (view is T match)
			{
				result = match;
				return true;
			}

			if (view is Android.Views.ViewGroup viewGroup)
			{
				for (var i = 0; i < viewGroup.ChildCount; i++)
				{
					var child = viewGroup.GetChildAt(i);
					if (child is not null && TryFindDescendant(child, out result))
						return true;
				}
			}

			result = null!;
			return false;
		}

		static int? GetDominantRenderedColor(Android.Views.View view)
		{
			var config = Android.Graphics.Bitmap.Config.Argb8888;
			if (config is null)
				return null;

			using var bitmap = Android.Graphics.Bitmap.CreateBitmap(view.Width, view.Height, config);
			if (bitmap is null)
				return null;

			using var canvas = new Android.Graphics.Canvas(bitmap);
			view.Draw(canvas);

			var colorCounts = new Dictionary<int, int>();
			for (var x = 0; x < bitmap.Width; x++)
			{
				for (var y = 0; y < bitmap.Height; y++)
				{
					var argb = bitmap.GetPixel(x, y);
					if (((uint)argb >> 24) == 0)
						continue;

					var opaqueArgb = unchecked((int)((uint)argb | 0xFF000000));
					colorCounts[opaqueArgb] = colorCounts.TryGetValue(opaqueArgb, out var count) ? count + 1 : 1;
				}
			}

			if (colorCounts.Count == 0)
				return null;

			return colorCounts.MaxBy(pair => pair.Value).Key;
		}

		static string FormatColor(uint argb) => $"{argb:X8}";
	}
#endif
}

