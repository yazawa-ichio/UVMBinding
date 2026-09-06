using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	[SemanticRole(SemanticRole.None)]
	public class DropdownLocalizeStringDataBinder : Binder<IList<LocalizeTextKey>, Dropdown>
	{
		IList<LocalizeTextKey> m_Cache;

		void OnLocaleChanged(Locale locale)
		{
			if (m_Cache != null)
			{
				Apply();
			}
		}

		protected override void OnBind()
		{
			LocalizationSettings.Instance.OnSelectedLocaleChanged += OnLocaleChanged;
		}

		protected override void OnUnbind()
		{
			LocalizationSettings.Instance.OnSelectedLocaleChanged -= OnLocaleChanged;
		}

		protected override void UpdateValue(IList<LocalizeTextKey> value)
		{
			m_Cache = value;
			Apply();
		}

		void Apply()
		{
			var options = Target.options;
			options.Clear();
			if (m_Cache != null)
			{
				foreach (var item in m_Cache)
				{
					var option = new Dropdown.OptionData
					{
						text = item.ToString()
					};
					options.Add(option);
				}
			}
			Target.RefreshShownValue();
		}
	}


}