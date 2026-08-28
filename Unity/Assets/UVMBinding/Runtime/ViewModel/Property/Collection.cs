using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UVMBinding.Core;

namespace UVMBinding
{
	public class Collection<T> : IList<T> where T : class, IViewModel
	{
		public readonly string Path;
		IViewModel m_VM;

		List<IViewModel> m_List;
		List<IViewModel> GetList()
		{
			if (m_List == null)
			{
				m_List = new List<IViewModel>();
				m_VM.Set<List<IViewModel>>(Path, m_List);
			}
			return m_List;
		}

		public int Count => m_List?.Count ?? 0;

		public bool IsReadOnly => false;

		//保持しているのは List<IViewModel> なので List<T> を渡すと変換が必要になる。自身を渡す
		public event System.Action<Collection<T>> OnChanged;

		public Collection(string path, IViewModel vm)
		{
			Path = path;
			m_VM = vm;
			m_VM.Property.Subscribe<List<IViewModel>>(Path, OnNotifyChanged);
		}

		public Collection(string path, IViewModel vm, List<T> val) : this(path, vm)
		{
			GetList().AddRange(val.Cast<IViewModel>());
		}

		void OnNotifyChanged(List<IViewModel> val)
		{
			OnChanged?.Invoke(this);
		}

		public void SetDirty()
		{
			m_VM.SetDirty(Path);
		}

		public T this[int index]
		{
			get
			{
				return GetList()[index] as T;
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
			if (m_List.RemoveAll(x => match(x as T)) > 0)
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
			var list = GetList();
			for (int i = 0; i < list.Count; i++)
			{
				array[arrayIndex + i] = list[i] as T;
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetList().Cast<T>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}

	}


}