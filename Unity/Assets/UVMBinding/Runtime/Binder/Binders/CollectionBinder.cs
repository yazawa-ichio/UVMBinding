using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Arguments;
using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding.Binders
{
	public interface ICollectionIndex
	{
		int Index { get; set; }
	}

	[SemanticRole(SemanticRole.List)]
	public class CollectionBinder : Binder<IList<IViewModel>>, IRowTemplateInfo
	{
		[SerializeField]
		GameObject m_Preafb = null;
		[SerializeField]
		bool m_DisableAutoActive = false;

		GameObject IRowTemplateInfo.RowTemplate => m_Preafb;

		List<GameObject> m_Instance = new();
		List<IViewModel> m_Bound = new();
		List<GameObject> m_Pool = new();
		bool m_Updating;

		readonly Dictionary<IViewModel, List<Entry>> m_Lookup = new();
		readonly List<List<Entry>> m_EntryPool = new();
		readonly List<GameObject> m_Detached = new();
		readonly Dictionary<GameObject, IView> m_Views = new();

		public int PooledCount => m_Pool.Count;

		readonly struct Entry
		{
			public readonly GameObject Instance;
			public readonly int Index;

			public Entry(GameObject instance, int index)
			{
				Instance = instance;
				Index = index;
			}
		}

		protected override void UpdateValue(IList<IViewModel> value)
		{
			if (m_Updating)
			{
				Log.Debug(this, "{0} Reentrant UpdateValue. skip", this);
				return;
			}
			m_Updating = true;
			try
			{
				Apply(value);
			}
			finally
			{
				m_Updating = false;
			}
		}

		void Apply(IList<IViewModel> value)
		{
			int count = value?.Count ?? 0;
			RemoveDestroyed();
			BuildLookup();
			try
			{
				for (int i = 0; i < count; i++)
				{
					var vm = value[i];
					var instance = Take(vm, out bool reused, out int previousIndex);
					if (i < m_Instance.Count)
					{
						m_Instance[i] = instance;
						m_Bound[i] = vm;
					}
					else
					{
						m_Instance.Add(instance);
						m_Bound.Add(vm);
					}
					if (reused && previousIndex == i)
					{
						Refresh(instance);
						continue;
					}
					Bind(i, instance, vm);
				}
				foreach (var pair in m_Lookup)
				{
					foreach (var entry in pair.Value)
					{
						Release(entry.Instance);
					}
				}
				foreach (var detached in m_Detached)
				{
					Release(detached);
				}
			}
			finally
			{
				ClearLookup();
			}
			if (m_Instance.Count > count)
			{
				m_Instance.RemoveRange(count, m_Instance.Count - count);
				m_Bound.RemoveRange(count, m_Bound.Count - count);
			}
			for (int i = 0; i < count; i++)
			{
				var child = m_Instance[i].transform;
				if (child.GetSiblingIndex() != i)
				{
					child.SetSiblingIndex(i);
				}
			}
		}

		void RemoveDestroyed()
		{
			for (int i = m_Instance.Count - 1; i >= 0; i--)
			{
				if (m_Instance[i] == null)
				{
					m_Views.Remove(m_Instance[i]);
					m_Instance.RemoveAt(i);
					m_Bound.RemoveAt(i);
				}
			}
			for (int i = m_Pool.Count - 1; i >= 0; i--)
			{
				if (m_Pool[i] == null)
				{
					m_Pool.RemoveAt(i);
				}
			}
		}

		void BuildLookup()
		{
			for (int i = 0; i < m_Instance.Count; i++)
			{
				var vm = m_Bound[i];
				if (vm == null)
				{
					m_Detached.Add(m_Instance[i]);
					continue;
				}
				if (!m_Lookup.TryGetValue(vm, out var list))
				{
					list = m_EntryPool.Count > 0 ? m_EntryPool[m_EntryPool.Count - 1] : new List<Entry>();
					if (m_EntryPool.Count > 0)
					{
						m_EntryPool.RemoveAt(m_EntryPool.Count - 1);
					}
					m_Lookup[vm] = list;
				}
				list.Add(new Entry(m_Instance[i], i));
			}
		}

		void ClearLookup()
		{
			foreach (var pair in m_Lookup)
			{
				pair.Value.Clear();
				m_EntryPool.Add(pair.Value);
			}
			m_Lookup.Clear();
			m_Detached.Clear();
		}

		GameObject Take(IViewModel vm, out bool reused, out int previousIndex)
		{
			if (vm != null && m_Lookup.TryGetValue(vm, out var list) && list.Count > 0)
			{
				var entry = list[0];
				list.RemoveAt(0);
				if (list.Count == 0)
				{
					list.Clear();
					m_EntryPool.Add(list);
					m_Lookup.Remove(vm);
				}
				reused = true;
				previousIndex = entry.Index;
				return entry.Instance;
			}
			reused = false;
			previousIndex = -1;
			if (m_Detached.Count > 0)
			{
				var detached = m_Detached[m_Detached.Count - 1];
				m_Detached.RemoveAt(m_Detached.Count - 1);
				return detached;
			}
			if (m_Pool.Count > 0)
			{
				var pooled = m_Pool[m_Pool.Count - 1];
				m_Pool.RemoveAt(m_Pool.Count - 1);
				return pooled;
			}
			return Instantiate(m_Preafb, transform);
		}

		void Release(GameObject instance)
		{
			if (instance == null)
			{
				return;
			}
			instance.SetActive(false);
			m_Pool.Add(instance);
		}

		void Refresh(GameObject instance)
		{
			FindView(instance)?.TryUpdate();
		}

		IView FindView(GameObject instance)
		{
			if (m_Views.TryGetValue(instance, out var cached))
			{
				return cached;
			}
			var view = instance.GetComponent<IView>();
			m_Views[instance] = view;
			return view;
		}

		protected virtual void Bind(int index, GameObject instance, IViewModel vm)
		{
			var view = FindView(instance);
			if (view == null)
			{
				Log.Warning(this, "{0} Not Found View in Collection Item {1}", this, instance);
				return;
			}
			view.Prepare();
			view.ViewModel = vm;
			if (!instance.activeSelf && !m_DisableAutoActive)
			{
				instance.SetActive(true);
			}
			SetIndex(index, instance, view);
			view.TryUpdate();
		}

		void SetIndex(int index, GameObject instance, IView view)
		{
			using (ListPool<ICollectionIndex>.Use(out var list))
			{
				instance.GetComponentsInChildren(true, list);
				foreach (var item in list)
				{
					item.Index = index;
				}
			}
			using (ListPool<IViewElement>.Use(out var list))
			{
				view.GetElements(list);
				foreach (var item in list)
				{
					if (item is IHasEventArgument e && e.GetArgument() is ICollectionIndex i)
					{
						i.Index = index;
					}
				}
			}
		}
	}
}
