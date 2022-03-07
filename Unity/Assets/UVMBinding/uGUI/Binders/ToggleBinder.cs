using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class ToggleBinder : Binder<bool, Toggle>
	{
		protected override void OnInit(Toggle target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override void UpdateValue(bool value)
		{
			Target.isOn = value;
		}
	}

}