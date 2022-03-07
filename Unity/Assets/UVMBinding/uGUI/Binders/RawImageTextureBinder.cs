using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class RawImageTextureBinder : Binder<Texture, RawImage>
	{
		protected override void UpdateValue(Texture value)
		{
			Target.texture = value;
		}
	}
}