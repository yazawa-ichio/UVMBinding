using UnityEngine;
namespace UVMBinding.Binders
{
	public class CanvasGroupAlphaBinder : Binder<float>
	{

		CanvasGroup m_Target;

		protected override void OnBind()
		{
			TryGetComponent(out m_Target);
		}

		protected override void UpdateValue(float value)
		{
			if (m_Target != null)
			{
				m_Target.alpha = value;
			}
		}
	}
}