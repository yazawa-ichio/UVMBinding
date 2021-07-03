using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class TextBinder : Binder<string>
	{
		[SerializeField]
		string m_Format;

		Text m_Text;

		protected override void OnBind()
		{
			TryGetComponent(out m_Text);
		}

		protected override void UpdateValue(string value)
		{
			if (m_Text != null)
			{
				m_Text.text = string.IsNullOrEmpty(m_Format) ? value : string.Format(m_Format, value);
			}
		}
	}
}