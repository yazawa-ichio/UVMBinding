using UnityEngine;
using UVMBinding.Core;

namespace UVMBinding
{
	public abstract class ViewModelSourceProvider : MonoBehaviour, IBindSourceProvider, IAutoViewUpdate
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
			m_Elements = GetComponents<IViewElement>();
			m_Binding.Init(m_Elements);
			m_Binding.Bind(viewModel);
		}

		protected void Unbind()
		{
			m_Binding?.Dispose();
			m_Binding = null;
		}
	}
}