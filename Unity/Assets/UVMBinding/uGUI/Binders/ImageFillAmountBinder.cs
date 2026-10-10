using UnityEngine.UI;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	public class ImageFillAmountBinder : Binder<float, Image>
	{
		protected override void UpdateValue(float value)
		{
			Target.fillAmount = value;
		}
	}
}