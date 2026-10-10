using UnityEngine;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.Image)]
	public class ImageSpriteBinder : Binder<Sprite, Image>
	{
		protected override string GetDisplayText()
		{
			return TryGetTarget(out var target) && target.sprite != null ? target.sprite.name : null;
		}

		protected override void UpdateValue(Sprite value)
		{
			Target.sprite = value;
		}
	}
}