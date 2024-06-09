using TMPro;

namespace UVMBinding.Binders
{
	public class TMP_DropdownSelectBinder : Binder<int, TMP_Dropdown>
	{
		protected override void OnInit(TMP_Dropdown target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override void UpdateValue(int value)
		{
			Target.value = value;
		}
	}
}