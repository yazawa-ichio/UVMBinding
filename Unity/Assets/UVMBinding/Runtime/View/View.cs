using System.Collections.Generic;
using UnityEngine;
using UVMBinding.Core;
using UVMBinding.Logger;

namespace UVMBinding
{

	public class View : MonoBehaviour, IView
	{
		[SerializeField]
#pragma warning disable CS0414
		string m_ReservationViewModel = default;
#pragma warning restore CS0414
		[SerializeField]
		bool m_UpdateOnAttach = false;

		[System.NonSerialized]
		bool m_Prepare;
		Binding m_Binding;
		IViewElement[] m_Elements;
		bool m_IsActive = true;
		IViewModel m_ViewModel;

		public bool IsActive => m_IsActive && this != null && gameObject != null;

		public IViewModel ViewModel
		{
			get => m_ViewModel;
			set => Attach(value);
		}

		public bool UpdateOnAttach
		{
			get => m_UpdateOnAttach;
			set => m_UpdateOnAttach = value;
		}

		private void Awake()
		{
			ViewUpdater.Register(this);
		}

		private void OnDestroy()
		{
			m_IsActive = false;
			m_Binding?.Dispose();
			m_Binding = null;
			OnDestroyImpl();
		}

		public void Prepare(bool force = false)
		{
			Log.Trace(this, "{0} Prepare force:{1}", this, force);
			if (m_Prepare && !force)
			{
				Log.Trace(this, "{0} Prepared", this);
				return;
			}
			m_Prepare = true;


			using (ListPool<IViewElement>.Use(out var elements))
			using (ListPool<IView>.Use(out var viewList))
			{
				GetComponentsInChildren(true, elements);
				GetComponentsInChildren(true, viewList);
				for (int i = elements.Count - 1; i >= 0; i--)
				{
					if (!elements[i].CanUse(this))
					{
						elements.RemoveAt(i);
					}
				}
				foreach (var view in viewList)
				{
					if (ReferenceEquals(this, view))
					{
						continue;
					}
					view.Prepare(force);
					using (ListPool<IViewElement>.Use(out var childElements))
					{
						view.GetElements(childElements);
						foreach (var remove in childElements)
						{
							elements.Remove(remove);
						}
					}
				}
				m_Elements = elements.ToArray();
			}
		}

		protected virtual void OnDestroyImpl() { }

		public virtual void TryUpdate()
		{
			m_Binding?.TryUpdate();
		}

		private void TryInit()
		{
			if (m_Binding != null)
			{
				return;
			}
			Log.Trace(this, "{0} Binding Init", this);
			if (m_Binding == null)
			{
				m_Binding = new Binding();
			}
			m_Binding.Init(m_Elements);
		}

		public T Attach<T>(System.Action<T> action = null) where T : IViewModel, new()
		{
			T vm = new T();
			action?.Invoke(vm);
			Attach(vm);
			return vm;
		}

		public void Attach(IViewModel vm)
		{
			Prepare();
			TryInit();
			Log.Trace(this, "{0} Attach {1}", this, vm);
			m_ViewModel = vm;
			m_Binding.Bind(vm);
			if (m_UpdateOnAttach || ViewUpdater.IsUpdating)
			{
				Log.Trace(this, "{0} Attach And Update", this);
				TryUpdate();
			}
		}

		public void GetElements(List<IViewElement> elements)
		{
			Prepare();
			if (m_Elements == null) return;
			foreach (var element in m_Elements)
			{
				elements.Add(element);
			}
		}

	}


}