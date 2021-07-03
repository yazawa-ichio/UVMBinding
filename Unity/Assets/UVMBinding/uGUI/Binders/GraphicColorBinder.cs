using UnityEngine;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	public class GraphicColorBinder : Binder<Color>
	{

		Graphic m_Target;

		protected override void OnBind()
		{
			TryGetComponent(out m_Target);
		}

		protected override void UpdateValue(Color value)
		{
			if (m_Target != null)
			{
				m_Target.color = value;
			}
		}
	}
}