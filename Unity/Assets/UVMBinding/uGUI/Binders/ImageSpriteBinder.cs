using UnityEngine;
using UnityEngine.UI;
namespace UVMBinding.Binders
{
	public class ImageSpriteBinder : Binder<Sprite, Image>
	{
		protected override void UpdateValue(Sprite value)
		{
			Target.sprite = value;
		}
	}
}