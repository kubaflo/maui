#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.Maui.Controls.Platform
{
	internal static class TemplatedItemSourceFactory
	{
		internal static object Create(IEnumerable itemsSource, DataTemplate itemTemplate, BindableObject container,
			double? itemHeight = null, double? itemWidth = null, Thickness? itemSpacing = null, IMauiContext mauiContext = null)
		{
			if (itemsSource is null)
			{
				return Array.Empty<object>();
			}

			switch (itemsSource)
			{
				case IList observable when itemsSource is INotifyCollectionChanged:
					return new ObservableItemTemplateCollection(observable, itemTemplate, container, itemHeight, itemWidth, itemSpacing, mauiContext);
				case IList list:
					return new ItemTemplateContextList(list, itemTemplate, container, itemHeight, itemWidth, itemSpacing, mauiContext);
				case INotifyCollectionChanged:
					// The source raises collection change notifications but does not implement the non-generic IList
					// (e.g. IReadOnlyList<T> or a plain IEnumerable). Adapt it so the observable pipeline can be used;
					// otherwise the notifications would be silently ignored.
					return new ObservableItemTemplateCollection(new ReadOnlyObservableListAdapter(itemsSource),
						itemTemplate, container, itemHeight, itemWidth, itemSpacing, mauiContext);
			}

			return new ItemTemplateContextEnumerable(itemsSource, itemTemplate, container, itemHeight, itemWidth, itemSpacing, mauiContext);
		}

		internal static object CreateGrouped(IEnumerable itemsSource, DataTemplate itemTemplate,
			DataTemplate groupHeaderTemplate, DataTemplate groupFooterTemplate, BindableObject container, IMauiContext mauiContext = null)
		{
			return new GroupedItemTemplateCollection(itemsSource, itemTemplate, groupHeaderTemplate, groupFooterTemplate, container, mauiContext);
		}

		// Presents an observable, non-IList source (e.g. IReadOnlyList<T>) as the read-only IList that
		// ObservableItemTemplateCollection requires, forwarding the source's change notifications.
		sealed class ReadOnlyObservableListAdapter : IList, INotifyCollectionChanged
		{
			readonly IEnumerable _source;
			readonly IReadOnlyList<object> _readOnlyList;
			readonly NotifyCollectionChangedEventHandler _sourceCollectionChanged;
			readonly WeakNotifyCollectionChangedProxy _proxy = new();

			~ReadOnlyObservableListAdapter() => _proxy.Unsubscribe();

			public ReadOnlyObservableListAdapter(IEnumerable source)
			{
				_source = source;
				_readOnlyList = source as IReadOnlyList<object>;
				_sourceCollectionChanged = OnSourceCollectionChanged;
				_proxy.Subscribe((INotifyCollectionChanged)source, _sourceCollectionChanged);
			}

			public event NotifyCollectionChangedEventHandler CollectionChanged;

			void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
				=> CollectionChanged?.Invoke(this, args);

			public int Count
			{
				get
				{
					if (_readOnlyList is not null)
					{
						return _readOnlyList.Count;
					}

					var count = 0;
					var enumerator = _source.GetEnumerator();
					while (enumerator.MoveNext())
					{
						count++;
					}

					(enumerator as IDisposable)?.Dispose();
					return count;
				}
			}

			public object this[int index]
			{
				get
				{
					if (_readOnlyList is not null)
					{
						return _readOnlyList[index];
					}

					if (index < 0)
					{
						throw new ArgumentOutOfRangeException(nameof(index));
					}

					var enumerator = _source.GetEnumerator();
					try
					{
						for (int n = 0; n <= index; n++)
						{
							if (!enumerator.MoveNext())
							{
								throw new ArgumentOutOfRangeException(nameof(index));
							}
						}

						return enumerator.Current;
					}
					finally
					{
						(enumerator as IDisposable)?.Dispose();
					}
				}
				set => throw new NotSupportedException();
			}

			public bool IsFixedSize => true;
			public bool IsReadOnly => true;
			public bool IsSynchronized => false;
			public object SyncRoot => this;

			public IEnumerator GetEnumerator() => _source.GetEnumerator();

			public int IndexOf(object value)
			{
				var index = 0;
				foreach (var item in _source)
				{
					if (Equals(item, value))
					{
						return index;
					}

					index++;
				}

				return -1;
			}

			public bool Contains(object value) => IndexOf(value) > -1;

			public void CopyTo(Array array, int index)
			{
				foreach (var item in _source)
				{
					array.SetValue(item, index);
					index++;
				}
			}

			public int Add(object value) => throw new NotSupportedException();
			public void Clear() => throw new NotSupportedException();
			public void Insert(int index, object value) => throw new NotSupportedException();
			public void Remove(object value) => throw new NotSupportedException();
			public void RemoveAt(int index) => throw new NotSupportedException();
		}
	}
}


