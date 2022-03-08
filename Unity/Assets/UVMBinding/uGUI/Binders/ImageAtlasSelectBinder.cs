using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	public class ImageAtlasSelectBinder : Binder<string, Image>
	{
		[SerializeField]
		SpriteAtlas m_Atlas;

		protected override void UpdateValue(string value)
		{
			Target.sprite = m_Atlas.GetSprite(value);
		}
	}
}