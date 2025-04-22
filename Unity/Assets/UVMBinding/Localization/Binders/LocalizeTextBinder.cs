using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class LocalizeTextBinder : Binder<LocalizeTextKey, Text>
	{
		[SerializeField]
		private LocalizedString m_StringReference = new LocalizedString();

		bool m_RawText;

		private void OnEnable()
		{
			m_StringReference.StringChanged += UpdateString;
		}

		private void OnDisable()
		{
			m_StringReference.StringChanged -= UpdateString;
		}

		void UpdateString(string text)
		{
			if (!m_RawText)
			{
				Target.text = text;
			}
		}

		protected override void UpdateValue(LocalizeTextKey value)
		{
			if (value.RawText)
			{
				Target.text = value.Entry;
				m_RawText = true;
			}
			else
			{
				m_RawText = false;
				if (value.Dic != null)
				{
					m_StringReference.Arguments = new object[] { value.Dic };
				}
				if (!string.IsNullOrEmpty(value.Table))
				{
					m_StringReference.TableReference = value.Table;
				}
				if (!string.IsNullOrEmpty(value.Entry))
				{
					m_StringReference.TableEntryReference = value.Entry;
				}
				m_StringReference.RefreshString();
			}
		}
	}


}