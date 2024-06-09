using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace UVMBinding.Binders
{
	public class TMP_DropdownStringDataBinder : Binder<IList<string>, TMP_Dropdown>
	{
		protected override void OnInit(TMP_Dropdown target)
		{
			target.ClearOptions();
		}

		protected override void UpdateValue(IList<string> value)
		{
			Target.ClearOptions();
			Target.AddOptions(value.ToList());
		}
	}

}