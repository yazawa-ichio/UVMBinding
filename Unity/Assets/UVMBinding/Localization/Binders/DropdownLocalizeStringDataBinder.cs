using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class DropdownLocalizeStringDataBinder : Binder<IList<LocalizeTextKey>, Dropdown>
	{
		protected override void OnInit(Dropdown target)
		{
			target.ClearOptions();
		}

		protected override void OnBind()
		{
			LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
		}

		protected override void OnUnbind()
		{
			LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
		}

		void OnLocaleChanged(Locale locale)
		{
			Apply(Get());
		}

		protected override void UpdateValue(IList<LocalizeTextKey> value)
		{
			Apply(value);
		}

		void Apply(IList<LocalizeTextKey> value)
		{
			Target.ClearOptions();
			if (value != null)
			{
				foreach (var item in value.Select(x => x.ToString()))
				{
					Target.options.Add(new Dropdown.OptionData(item));
				}
				Target.RefreshShownValue();
			}
		}
	}


}