using UnityEngine;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class TextBinder : Binder<string, Text>
	{
		[SerializeField]
		string m_Format;

		protected override void UpdateValue(string value)
		{
			Target.text = string.IsNullOrEmpty(m_Format) ? value : string.Format(m_Format, value);
		}
	}
}