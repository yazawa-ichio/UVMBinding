using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.Image)]
	public class RawImageTextureBinder : Binder<Texture, RawImage>
	{
		protected override string GetDisplayText()
		{
			return TryGetTarget(out var target) && target.texture != null ? target.texture.name : null;
		}

		protected override void UpdateValue(Texture value)
		{
			Target.texture = value;
		}
	}
}