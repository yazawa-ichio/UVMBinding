using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace UVMBinding.Binders
{
	public class DropdownStringDataBinder : Binder<IList<string>, Dropdown>
	{
		protected override void OnInit(Dropdown target)
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