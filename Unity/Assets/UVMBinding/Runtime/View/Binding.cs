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

		public void Bind(IViewModel model)
		{
			if (m_Disposed) return;
			Unbind();
			if (model == null)
			{
				return;
			}
			Log.Trace("bind {0}", model);
			using (ListPool<IBindingProperty>.Use(out var list))
			{
				foreach (var prop in model.Property.GetAllImpl(list))
				{
					Bind(prop);
				}
			}
			model.Property.OnNewProperty += Bind;
			m_ViewModel = model;
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