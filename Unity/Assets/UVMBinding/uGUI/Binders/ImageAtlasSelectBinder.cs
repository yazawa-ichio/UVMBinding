using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.Image)]
	public class ImageAtlasSelectBinder : Binder<string, Image>
	{
		[SerializeField]
		SpriteAtlas m_Atlas;

		protected override string GetDisplayText()
		{
			return TryGetTarget(out var target) && target.sprite != null ? target.sprite.name : null;
		}

		protected override void UpdateValue(string value)
		{
			Target.sprite = m_Atlas.GetSprite(value);
		}
	}
}