using System;
using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Logger;

namespace UVMBinding.Core
{

	public class Binding : IDisposable, IViewEventDispatcher
	{
		List<IBinder> m_Binding = new();
		IViewModel m_ViewModel;
		bool m_Disposed;

		public void Init(IViewElement[] elements)
		{
			m_Binding = new List<IBinder>(elements.Length);
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
				Debug.AssertFormat(!string.IsNullOrEmpty(binder.Path), binder as UnityEngine.Object, "{0} Path Empty", binder);
			}
		}


		public void Add(IViewElement elm)
		{
			if (m_Disposed) return;
			AddImpl(elm);
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
				if (binder != null && binder.IsActive)
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
			}
		}

		void TryRebind()
		{
			List<IBinder> rebind = default;
			for (int i = m_Binding.Count - 1; i >= 0; i--)
			{
				var binder = m_Binding[i];
				if (!binder.IsActive)
				{
					continue;
				}
				if (binder.IsRebind)
				{
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
			m_ViewModel.OnBind();
			using (ListPool<IBindingProperty>.Use(out var list))
			{
				foreach (var prop in m_ViewModel.Property.GetAllImpl(list))
				{
					Bind(prop);
				}
			}
			m_ViewModel.Property.OnNewProperty += Bind;
			if (m_ViewModel.Property.ShouldCreateProperty)
			{
				foreach (var binder in m_Binding)
				{
					if (binder.IsActive)
					{
						m_ViewModel.Property.TryGet(binder.Path, out var prop);
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
			m_ViewModel.Property.OnNewProperty -= Bind;
			m_ViewModel = null;
		}

		void Bind(IBindingProperty prop)
		{
			if (m_Disposed) return;
			foreach (var binder in m_Binding)
			{
				if (binder.Path == prop.Path && binder.IsActive)
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