using System;
using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Logger;

namespace UVMBinding.Core
{

	public class Binding : IDisposable, IViewEventDispatcher
	{
		List<IBinder> m_Binding = new();
		List<IBinder> m_Animated = new();
		Dictionary<string, List<IBinder>> m_ByPath = new();
		IViewModel m_ViewModel;
		bool m_Disposed;

		readonly List<PropertyContainer> m_Sources = new List<PropertyContainer>();
		readonly List<int> m_Versions = new List<int>();
		int m_PathVersion = -1;

		public void Init(IViewElement[] elements)
		{
			m_Binding = new List<IBinder>(elements.Length);
			m_Animated.Clear();
			m_ByPath.Clear();
			foreach (var elm in elements)
			{
				AddImpl(elm);
			}
		}

		void AddImpl(IViewElement elm)
		{
			//イベントであれば登録
			(elm as IViewEvent)?.Bind(this);
			if (elm is IBinder binder)
			{
				m_Binding.Add(binder);
				if (elm is IBinderInfo info && info.HasConverter)
				{
					m_Animated.Add(binder);
				}
				if (!string.IsNullOrEmpty(binder.Path))
				{
					if (!m_ByPath.TryGetValue(binder.Path, out var list))
					{
						m_ByPath[binder.Path] = list = new List<IBinder>();
					}
					list.Add(binder);
				}
				Debug.AssertFormat(!string.IsNullOrEmpty(binder.Path), binder as UnityEngine.Object, "{0} Path Empty", binder);
			}
		}

		internal void AddPropertySource(PropertyContainer container)
		{
			if (container == null || m_Sources.Contains(container))
			{
				return;
			}
			m_Sources.Add(container);
			m_Versions.Add(int.MinValue);
		}

		bool HasChange()
		{
			bool changed = false;
			for (int i = 0; i < m_Sources.Count; i++)
			{
				var version = m_Sources[i].Version;
				if (m_Versions[i] != version)
				{
					m_Versions[i] = version;
					changed = true;
				}
			}
			return changed;
		}

		void InvalidateVersions()
		{
			for (int i = 0; i < m_Versions.Count; i++)
			{
				m_Versions[i] = int.MinValue;
			}
		}

		public void Add(IViewElement elm)
		{
			if (m_Disposed) return;
			AddImpl(elm);
			InvalidateVersions();
			if (m_ViewModel != null && elm is IBinder binder && binder.IsActive)
			{
				if (m_ViewModel.Property.TryGet(binder.Path, out var prop))
				{
					binder.Bind(prop);
				}
			}
		}

		public void Remove(IViewElement elm)
		{
			if (m_Disposed) return;
			if (elm is IBinder binder)
			{
				m_Binding.Remove(binder);
				m_Animated.Remove(binder);
				if (!string.IsNullOrEmpty(binder.Path) && m_ByPath.TryGetValue(binder.Path, out var list))
				{
					list.Remove(binder);
				}
				if (binder.IsActive)
				{
					binder.Unbind();
				}
			}
		}

		static Predicate<IBinder> s_RemoveAll = (x) => !x.IsActive;

		public void TryUpdate()
		{
			if (m_Disposed) return;

			TryRebind();
			TryBinderUpdate();

		}

		void TryBinderUpdate()
		{
			if (!HasChange())
			{
				foreach (var binder in m_Animated)
				{
					if (binder.IsActive && binder.NeedsUpdate)
					{
						binder.TryUpdate();
					}
				}
				return;
			}
			bool remove = false;
			foreach (var binder in m_Binding)
			{
				if (binder.IsActive)
				{
					binder.TryUpdate();
				}
				else
				{
					remove = true;
				}
			}
			if (remove)
			{
				m_Binding.RemoveAll(s_RemoveAll);
				m_Animated.RemoveAll(s_RemoveAll);
				foreach (var list in m_ByPath.Values)
				{
					list.RemoveAll(s_RemoveAll);
				}
			}
		}

		void TryRebind()
		{
			if (m_PathVersion == BinderPathVersion.Value)
			{
				return;
			}
			m_PathVersion = BinderPathVersion.Value;
			List<IBinder> rebind = default;
			for (int i = m_Binding.Count - 1; i >= 0; i--)
			{
				var binder = m_Binding[i];
				if (binder.IsRebind) // 軽いので先にチェックする
				{
					if (!binder.IsActive)
					{
						continue;
					}
					if (rebind == null)
					{
						rebind = ListPool<IBinder>.Pop();
					}
					rebind.Add(binder);
				}
			}
			if (rebind == null)
			{
				return;
			}
			foreach (var binder in rebind)
			{
				Remove(binder);
				Add(binder);
			}
			ListPool<IBinder>.Push(rebind);
		}

		public void Bind(IViewModel model)
		{
			if (m_Disposed) return;
			Unbind();
			if (model == null)
			{
				return;
			}
			Log.Trace("bind {0}", model);
			m_ViewModel = model;
			m_Sources.Clear();
			m_Versions.Clear();
			AddPropertySource(m_ViewModel.Property);
			m_PathVersion = -1;
			m_ViewModel.OnBind();
			using (ListPool<IBindingProperty>.Use(out var list))
			{
				foreach (var prop in m_ViewModel.Property.GetAllImpl(list))
				{
					BindToBinder(prop);
				}
			}
			m_ViewModel.Property.OnNewProperty += BindToBinder;
			if (m_ViewModel.Property.ShouldCreateProperty)
			{
				foreach (var binder in m_Binding)
				{
					if (binder.IsActive)
					{
						m_ViewModel.Property.TryGet(binder.Path, out var _);
					}
				}
			}
		}

		public void Unbind()
		{
			if (m_ViewModel == null) return;
			Log.Trace("Unbind {0}", m_ViewModel);
			foreach (var binder in m_Binding)
			{
				if (binder != null && binder.IsActive)
				{
					binder.Unbind();
				}
			}
			m_ViewModel.OnUnbind();
			m_ViewModel.Property.OnNewProperty -= BindToBinder;
			m_ViewModel = null;
		}

		internal void BindToBinder(IBindingProperty prop)
		{
			if (m_Disposed) return;
			if (!m_ByPath.TryGetValue(prop.Path, out var binders))
			{
				return;
			}
			InvalidateVersions();
			foreach (var binder in binders)
			{
				if (binder.IsActive)
				{
					Log.Trace("Bind {0} {1}", prop.Path, binder);
					binder.Bind(prop);
				}
			}
		}

		void IViewEventDispatcher.Dispatch(string name)
		{
			if (m_Disposed) return;
			m_ViewModel?.Event?.Publish(name);
		}

		void IViewEventDispatcher.Dispatch<T>(string name, T args)
		{
			if (m_Disposed) return;
			m_ViewModel?.Event?.Publish(name, args);
		}

		public void Dispose()
		{
			Unbind();
			m_Disposed = true;
		}

	}

}