using TMPro;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.Text)]
	public class TextMeshProBinder : Binder<string, TMP_Text>
	{
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