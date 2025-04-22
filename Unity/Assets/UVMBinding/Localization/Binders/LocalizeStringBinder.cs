using UnityEngine;
using UnityEngine.Localization.Components;

namespace UVMBinding.Binders
{
	[RequireComponent(typeof(LocalizeStringEvent))]
	public class LocalizeStringBinder : Binder<LocalizeTextKey, LocalizeStringEvent>
	{
		protected override void UpdateValue(LocalizeTextKey value)
		{
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
		}
	}


}