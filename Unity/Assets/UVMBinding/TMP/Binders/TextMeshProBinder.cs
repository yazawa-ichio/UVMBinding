using TMPro;

namespace UVMBinding.Binders
{
	public class TextMeshProBinder : Binder<string, TMP_Text>
	{
		protected override void UpdateValue(string value)
		{
			Target.text = value;
		}
	}
}