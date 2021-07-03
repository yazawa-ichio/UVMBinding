using UnityEngine;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	public class ImageSpriteBinder : Binder<Sprite>
	{

		Image m_Target;

		protected override void OnBind()
		{
			TryGetComponent(out m_Target);
		}

		protected override void UpdateValue(Sprite value)
		{
			if (m_Target != null)
			{
				m_Target.sprite = value;
			}
		}
	}
}