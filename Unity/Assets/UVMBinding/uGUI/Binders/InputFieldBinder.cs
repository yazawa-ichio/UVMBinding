using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class InputFieldBinder : Binder<string, InputField>
	{
		protected override void OnInit(InputField target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override void UpdateValue(string value)
		{
			Target.text = value;
		}
	}
}