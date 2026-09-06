using UnityEngine;
using UnityEngine.Localization.Components;

namespace UVMBinding.Binders
{
	[RequireComponent(typeof(LocalizeStringEvent))]
	[SemanticRole(SemanticRole.Text)]
	public class LocalizeStringBinder : Binder<LocalizeTextKey, LocalizeStringEvent>
	{
		LocalizeTextKey m_Value;
		bool m_DoFallback;

		protected override void OnInit(LocalizeStringEvent target)
		{
			target.OnUpdateString.AddListener((x) =>
			{
				if (m_DoFallback) return;
				if (string.IsNullOrEmpty(x))
				{
					SetFallback();
				}
			});
		}

		void SetFallback()
		{
			try
			{
				m_DoFallback = true;
				if (m_Value.Fallback != null)
				{
					Target.OnUpdateString?.Invoke(m_Value.Fallback.ToString());
				}
			}
			finally
			{

				m_DoFallback = false;
			}
		}

		protected override void UpdateValue(LocalizeTextKey value)
		{
			m_Value = value;
			if (value.RawText)
			{
				Target.OnUpdateString?.Invoke(value.Entry);
			}
			else
			{
				if (value.Dic != null)
				{
					Target.StringReference.Arguments = new object[] { value.Dic };
				}
				if (!string.IsNullOrEmpty(value.Table))
				{
					Target.SetTable(value.Table);
				}
				if (!string.IsNullOrEmpty(value.Entry))
				{
					Target.SetEntry(value.Entry);
				}
			}
			Target.RefreshString();
		}
	}


}