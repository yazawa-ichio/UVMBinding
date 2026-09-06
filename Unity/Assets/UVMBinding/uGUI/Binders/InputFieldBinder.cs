using UnityEngine.UI;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.TextBox)]
	public class InputFieldBinder : Binder<string, InputField>
	{
		protected override void OnInit(InputField target)
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