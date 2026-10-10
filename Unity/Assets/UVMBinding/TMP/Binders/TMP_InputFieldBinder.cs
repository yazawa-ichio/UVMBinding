using TMPro;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.TextBox)]
	public class TMP_InputFieldBinder : Binder<string, TMP_InputField>
	{
		protected override void OnInit(TMP_InputField target)
		{
			target.onValueChanged.AddListener(Set);
		}

		protected override string GetDisplayText()
		{
			return TryGetTarget(out var target) ? target.text : null;
		}

		protected override void UpdateValue(string value)
		{
			Target.text = value;
		}
	}
}