using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class SliderBinder : Binder<float, Slider>
	{
		protected override void OnInit(Slider target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override void UpdateValue(float value)
		{
			Target.value = value;
		}
	}
}