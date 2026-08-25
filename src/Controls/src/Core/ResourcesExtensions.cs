#nullable disable
using System;
using System.Collections.Generic;

namespace Microsoft.Maui.Controls
{
	static class ResourcesExtensions
	{
		public static IEnumerable<KeyValuePair<string, object>> GetMergedResources(this IElementDefinition element)
		{
			return GetMergedResources(element, null);
		}

		static IEnumerable<KeyValuePair<string, object>> GetMergedResources(this IElementDefinition element, HashSet<string> requestedKeys)
		{
			Dictionary<string, object> resources = null;
			var current = Application.Current;
			bool visitedCurrentApplication = false;
			while (element != null)
			{
				visitedCurrentApplication |= ReferenceEquals(element, current);
				var ve = element as IResourcesProvider;
				if (ve != null && ve.IsResourcesCreated)
				{
					if (requestedKeys == null)
						resources = resources ?? new(StringComparer.Ordinal);

					foreach (KeyValuePair<string, object> res in ve.Resources.MergedResources)
					{
						if (requestedKeys != null && !requestedKeys.Contains(res.Key))
							continue;

						resources = resources ?? new(StringComparer.Ordinal);
						// If a MergedDictionary value is overridden for a DynamicResource, 
						// it comes out later in the enumeration of MergedResources
						// TryGetValue ensures we pull the up-to-date value for the key
						if (!resources.ContainsKey(res.Key) && ve.Resources.TryGetValue(res.Key, out object value))
							resources.Add(res.Key, value);
						else if (res.Key.StartsWith(Style.StyleClassPrefix, StringComparison.Ordinal))
						{
							var mergedClassStyles = new List<Style>(resources[res.Key] as List<Style>);
							mergedClassStyles.AddRange(res.Value as List<Style>);
							resources[res.Key] = mergedClassStyles;
						}
					}
				}
				var app = element as Application;
				if (app != null && app.SystemResources != null)
				{
					if (requestedKeys == null)
						resources = resources ?? new Dictionary<string, object>(8, StringComparer.Ordinal);

					foreach (KeyValuePair<string, object> res in app.SystemResources)
					{
						if (requestedKeys != null && !requestedKeys.Contains(res.Key))
							continue;

						resources = resources ?? new Dictionary<string, object>(8, StringComparer.Ordinal);
						if (!resources.ContainsKey(res.Key))
							resources.Add(res.Key, res.Value);
						else if (res.Key.StartsWith(Style.StyleClassPrefix, StringComparison.Ordinal))
						{
							var mergedClassStyles = new List<Style>(resources[res.Key] as List<Style>);
							mergedClassStyles.AddRange(res.Value as List<Style>);
							resources[res.Key] = mergedClassStyles;
						}
					}
				}
				if (app != null && (requestedKeys == null || requestedKeys.Contains(AppThemeBinding.AppThemeResource)))
				{
					resources = resources ?? new(StringComparer.Ordinal);
					resources[AppThemeBinding.AppThemeResource] = app.RequestedTheme;
				}

				element = element.Parent;
			}

			if (!visitedCurrentApplication &&
				current is IResourcesProvider application &&
				application.IsResourcesCreated)
			{
				if (requestedKeys != null)
				{
					foreach (var key in requestedKeys)
					{
						if ((resources == null || !resources.ContainsKey(key)) &&
							current.TryGetResource(key, out var value))
						{
							resources ??= new(StringComparer.Ordinal);
							resources.Add(key, value);
						}
					}
				}
				else
				{
					foreach (var resource in application.Resources.MergedResources)
					{
						resources ??= new(StringComparer.Ordinal);
						if (!resources.ContainsKey(resource.Key) &&
							application.Resources.TryGetValue(resource.Key, out var value))
						{
							resources.Add(resource.Key, value);
						}
						else if (resource.Key.StartsWith(Style.StyleClassPrefix, StringComparison.Ordinal))
						{
							var mergedClassStyles = new List<Style>(resources[resource.Key] as List<Style>);
							mergedClassStyles.AddRange(resource.Value as List<Style>);
							resources[resource.Key] = mergedClassStyles;
						}
					}
				}
			}

			return resources;
		}

		internal static IEnumerable<KeyValuePair<string, object>> GetMergedResourcesForKeys(this IElementDefinition element, IEnumerable<string> keys)
		{
			if (element == null || keys == null)
				return null;

			HashSet<string> requestedKeys = null;
			foreach (var key in keys)
			{
				if (string.IsNullOrEmpty(key))
					continue;

				requestedKeys ??= new HashSet<string>(StringComparer.Ordinal);
				requestedKeys.Add(key);
			}

			if (requestedKeys == null || requestedKeys.Count == 0)
				return null;

			return GetMergedResourcesForKeys(element, requestedKeys);
		}

		internal static IEnumerable<KeyValuePair<string, object>> GetMergedResourcesForKeys(this IElementDefinition element, HashSet<string> requestedKeys)
		{
			if (element == null || requestedKeys == null || requestedKeys.Count == 0)
				return null;

			return GetMergedResources(element, requestedKeys);
		}

		public static bool TryGetResource(this IElementDefinition element, string key, out object value)
		{
			var resourceTarget = element;
			while (element != null)
			{
				if (element is IResourcesProvider ve && ve.IsResourcesCreated && ve.Resources.TryGetValue(key, out value))
					return true;
				if (element is Application app && app.SystemResources != null && app.SystemResources.TryGetValue(key, out value))
					return true;
				element = element.Parent;
			}

			//Fallback for the XF previewer
			if (!IsImplicitStyleKey(resourceTarget, key) &&
				Application.Current != null &&
				((IResourcesProvider)Application.Current).IsResourcesCreated &&
				Application.Current.Resources.TryGetValue(key, out value))
				return true;

			value = null;
			return false;
		}

		static bool IsImplicitStyleKey(IElementDefinition element, string key)
		{
			for (var type = element?.GetType(); type != null && typeof(Element).IsAssignableFrom(type); type = type.BaseType)
			{
				if (string.Equals(type.FullName, key, StringComparison.Ordinal))
					return true;
			}

			return false;
		}
	}
}
