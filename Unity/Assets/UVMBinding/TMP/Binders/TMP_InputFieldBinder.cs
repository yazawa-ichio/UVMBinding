using TMPro;

namespace UVMBinding.Binders
{
	public class TMP_InputFieldBinder : Binder<string, TMP_InputField>
	{
		protected override void OnInit(TMP_InputField target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override void UpdateValue(string value)
		{
			Target.text = value;
		}
	}
}