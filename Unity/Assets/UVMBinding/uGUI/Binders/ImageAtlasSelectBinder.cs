using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	public class ImageAtlasSelectBinder : Binder<string>
	{
		[SerializeField]
		SpriteAtlas m_Atlas;

		Image m_Target;

		protected override void OnBind()
		{
			TryGetComponent(out m_Target);
		}

		protected override void UpdateValue(string value)
		{
			if (m_Target != null)
			{
				m_Target.sprite = m_Atlas.GetSprite(value);
			}
		}
	}
}