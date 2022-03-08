using System.Collections;
using System.Collections.Generic;
using UVMBinding.Core;

namespace UVMBinding
{
	public class ValueCollection<T> : IList<T>
	{
		public readonly string Path;
		IViewModel m_VM;

		List<T> m_List;
		List<T> GetList()
		{
			if (m_List == null)
			{
				m_List = new List<T>();
				m_VM.Set<List<T>>(Path, m_List);
			}
			return m_List;
		}

		public List<T> Value
		{
			set { m_List = value; m_VM.Set<List<T>>(Path, value); }
		}

		public int Count => m_List?.Count ?? 0;

		bool ICollection<T>.IsReadOnly => false;

		public event System.Action<List<T>> OnChanged;

		public ValueCollection(string path, IViewModel vm)
		{
			Path = path;
			m_VM = vm;
			m_VM.Property.Subscribe<List<T>>(Path, OnNotifyChanged);
		}

		public ValueCollection(string path, IViewModel vm, List<T> val) : this(path, vm)
		{
			Value = val;
		}

		void OnNotifyChanged(List<T> val)
		{
			OnChanged?.Invoke(val);
		}

		public void SetDirty()
		{
			m_VM.SetDirty(Path);
		}

		public T this[int index]
		{
			get
			{
				return GetList()[index];
			}
			set
			{
				GetList()[index] = value;
				SetDirty();
			}
		}

		public void Add(T val)
		{
			var list = GetList();
			list.Add(val);
			SetDirty();
		}

		public bool Remove(T val)
		{
			if (m_List != null && m_List.Remove(val))
			{
				SetDirty();
				return true;
			}
			return false;
		}

		public void Clear()
		{
			if (m_List == null) return;
			if (m_List.Count > 0)
			{
				m_List.Clear();
				SetDirty();
			}
		}

		public void AddRange(IEnumerable<T> collection)
		{
			var list = GetList();
			list.AddRange(collection);
			SetDirty();
		}

		public void RemoveAll(System.Predicate<T> match)
		{
			if (m_List == null) return;
			if (m_List.RemoveAll(match) > 0)
			{
				SetDirty();
			}
		}

		public int IndexOf(T item)
		{
			return m_List?.IndexOf(item) ?? -1;
		}

		public void Insert(int index, T item)
		{
			m_List?.Insert(index, item);
			SetDirty();
		}

		public void RemoveAt(int index)
		{
			m_List?.RemoveAt(index);
			SetDirty();
		}

		public bool Contains(T item)
		{
			return m_List?.Contains(item) ?? false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			GetList().CopyTo(array, arrayIndex);
			SetDirty();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetList() as IEnumerator<T>;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetList() as IEnumerator;
		}

	}


}