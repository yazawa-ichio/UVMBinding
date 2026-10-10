using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding
{
	public abstract class ViewModelSourceProvider : BindSourceProvider, IAutoViewUpdate
	{
		bool m_IsActive;
		IViewElement[] m_Elements;

		Binding m_Binding;

		public bool IsActive => m_IsActive && this != null && gameObject != null;

		public abstract IViewModel GetViewModel();

		protected virtual void Awake()
		{
			m_IsActive = true;
			Bind();
			ViewUpdater.Register(this);
		}

		protected virtual void OnDestroy()
		{
			m_IsActive = false;
			Unbind();
		}

		public void TryUpdate()
		{
			m_Binding?.TryUpdate();
		}

		protected void Bind()
		{
			var viewModel = GetViewModel();
			if (viewModel == null)
			{
				return;
			}
			m_Binding = new Binding();
			m_Elements = GetTargetElements();
			m_Binding.Init(m_Elements);
			m_Binding.Bind(viewModel);
		}

		IViewElement[] GetTargetElements()
		{
			using (ListPool<IViewElement>.Use(out var elements))
			using (ListPool<IViewElement>.Use(out var targets))
			{
				GetComponents(elements);
				foreach (var element in elements)
				{
					if (element is Component component && IsTarget(component))
					{
						targets.Add(element);
					}
				}
				return targets.ToArray();
			}
		}

		protected void Unbind()
		{
			m_Binding?.Dispose();
			m_Binding = null;
		}
	}
}