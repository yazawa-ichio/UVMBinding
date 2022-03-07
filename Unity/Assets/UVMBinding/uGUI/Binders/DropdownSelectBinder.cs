using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class DropdownSelectBinder : Binder<int, Dropdown>
	{
		protected override void OnInit(Dropdown target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override void UpdateValue(int value)
		{
			Target.value = value;
		}
	}

}