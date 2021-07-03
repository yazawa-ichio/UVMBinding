using UVMBinding.Core;

namespace UVMBinding.Binders
{
	public class ViewBinder : Binder<IViewModel>
	{
		IView m_View;

		protected override void OnBind()
		{
			TryGetComponent(out m_View);
		}

		protected override void UpdateValue(IViewModel value)
		{
			if (m_View != null)
			{
				m_View.ViewModel = value;
			}
		}

		protected override bool CanUse(IView view)
		{
			TryGetComponent(out m_View);
			return m_View == null || !m_View.Equals(view);
		}

	}
}