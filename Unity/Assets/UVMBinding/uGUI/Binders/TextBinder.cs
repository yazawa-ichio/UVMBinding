using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.Text)]
	public class TextBinder : Binder<string, Text>
	{
		[SerializeField]
		string m_Format;

		protected override string GetDisplayText()
		{
			return TryGetTarget(out var target) ? target.text : null;
		}

		protected override void UpdateValue(string value)
		{
			Target.text = string.IsNullOrEmpty(m_Format) ? value : string.Format(m_Format, value);
		}
	}
}